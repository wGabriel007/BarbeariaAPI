-- =====================================================================
-- Migração pontual: MULTI-BARBEARIA (multi-tenant) — o sistema deixa de
-- servir UMA barbearia só e passa a hospedar VÁRIAS, cada uma com seus
-- próprios donos/barbeiros/clientes/serviços/planos/agendamentos, todas
-- compartilhando o mesmo banco de dados.
--
-- Só é necessária se você já criou seu banco a partir do 01_schema.sql
-- ANTES desta mudança. Se você está criando o banco pela primeira vez,
-- ignore este arquivo — o 01_schema.sql já vem com tudo isso incluído.
--
-- Você não precisa rodar isto manualmente: a própria Api aplica
-- automaticamente qualquer migração pendente assim que sobe — ver
-- Barbearia.Infrastructure/Persistencia/MigrationRunner.cs.
--
-- O que muda:
--   1. Nova tabela `empresas` — cada linha é UMA barbearia cadastrada na
--      plataforma (chamada de "Empresa" no código pra não colidir com o
--      nome do projeto — ver Barbearia.Domain.Entidades.Empresa), com um
--      `slug` único que vira parte do link que os clientes dela acessam
--      (ex.: seusite.vercel.app/barbearia-do-joao).
--   2. Uma barbearia "Minha Barbearia" (slug 'minha-barbearia') nasce
--      automaticamente aqui — é pra onde TODOS os dados que já existiam
--      antes desta migração são migrados, então nada se perde: seu banco
--      atual simplesmente vira a primeira barbearia da plataforma.
--   3. `empresa_id` é adicionado em usuarios, clientes, barbeiros,
--      servicos, planos_assinatura, assinaturas, agendamentos,
--      pagamentos, solicitacoes_plano, configuracoes_site e
--      premios_ranking, sempre apontando pra uma linha de `empresas` —
--      é isso que separa os dados de cada barbearia (ver os
--      HasQueryFilter em BarbeariaDbContext.cs). Em usuarios fica
--      OPCIONAL (nullable) — só o SuperAdmin (dono da plataforma, ver
--      TipoUsuario) não pertence a nenhuma barbearia; em todo o resto é
--      obrigatório.
--   4. configuracoes_site deixa de ser uma linha global única (Id fixo
--      em 1) e passa a ter uma linha POR BARBEARIA.

-- ---------------------------------------------------------------------
-- 1. Tabela `empresas` (a barbearia em si, o "tenant").
-- ---------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS empresas (
  id            BIGINT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
  nome          VARCHAR(150) NOT NULL,
  slug          VARCHAR(80)  NOT NULL,
  status        INT NOT NULL DEFAULT 1 CHECK (status IN (0, 1, 2)),
                  -- 0=Inativo, 1=Ativo, 2=Bloqueado (mesmo enum StatusRegistro de sempre)
  criado_em     TIMESTAMPTZ NOT NULL DEFAULT now(),
  atualizado_em TIMESTAMPTZ NOT NULL DEFAULT now()
);

DO $$
BEGIN
  IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'uq_empresas_slug') THEN
    ALTER TABLE empresas ADD CONSTRAINT uq_empresas_slug UNIQUE (slug);
  END IF;
END $$;

DO $$
BEGIN
  IF NOT EXISTS (SELECT 1 FROM pg_trigger WHERE tgname = 'trg_empresas_atualizado') THEN
    CREATE TRIGGER trg_empresas_atualizado
      BEFORE UPDATE ON empresas
      FOR EACH ROW EXECUTE FUNCTION atualizar_timestamp();
  END IF;
END $$;

-- ---------------------------------------------------------------------
-- 2. Barbearia padrão — pra onde todo dado que já existia neste banco
--    (antes de existir o conceito de multi-barbearia) é migrado. Rodar
--    de novo não duplica (ON CONFLICT no slug único acima).
-- ---------------------------------------------------------------------
INSERT INTO empresas (nome, slug, status)
VALUES ('Minha Barbearia', 'minha-barbearia', 1)
ON CONFLICT (slug) DO NOTHING;

-- ---------------------------------------------------------------------
-- 3. empresa_id em cada tabela — ADD COLUMN (nullable primeiro, pra
--    poder fazer o backfill), UPDATE pra apontar pra barbearia padrão em
--    toda linha já existente, FK, índice, e só então (quando aplicável)
--    NOT NULL.
-- ---------------------------------------------------------------------

-- usuarios: fica NULLABLE de propósito (só o SuperAdmin não pertence a
-- nenhuma barbearia) — por isso aqui NÃO entra um "SET NOT NULL" no
-- final, diferente de todas as outras tabelas abaixo.
ALTER TABLE usuarios ADD COLUMN IF NOT EXISTS empresa_id BIGINT;
UPDATE usuarios SET empresa_id = (SELECT id FROM empresas WHERE slug = 'minha-barbearia')
  WHERE empresa_id IS NULL;
DO $$
BEGIN
  IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'fk_usuarios_empresa') THEN
    ALTER TABLE usuarios ADD CONSTRAINT fk_usuarios_empresa FOREIGN KEY (empresa_id) REFERENCES empresas(id) ON DELETE RESTRICT;
  END IF;
END $$;
CREATE INDEX IF NOT EXISTS ix_usuarios_empresa ON usuarios (empresa_id);

-- uq_usuarios_email (UNIQUE global em email, do 01_schema.sql) não faz
-- mais sentido num mundo multi-barbearia: duas barbearias DIFERENTES
-- precisam poder ter, cada uma, um usuário com o mesmo email (ex.: o
-- mesmo barbeiro trabalha em duas barbearias, ou só coincidência).
-- Troca por DOIS índices únicos PARCIAIS:
--   - email único DENTRO de cada barbearia (empresa_id, email);
--   - email único entre os SuperAdmins entre si (empresa_id IS NULL) —
--     sem isso, dois SuperAdmins nunca cairiam na mesma checagem acima
--     (empresa_id NULL não bate com NULL num índice único do Postgres).
ALTER TABLE usuarios DROP CONSTRAINT IF EXISTS uq_usuarios_email;
CREATE UNIQUE INDEX IF NOT EXISTS ux_usuarios_empresa_email ON usuarios (empresa_id, email) WHERE empresa_id IS NOT NULL;
CREATE UNIQUE INDEX IF NOT EXISTS ux_usuarios_email_superadmin ON usuarios (email) WHERE empresa_id IS NULL;

-- Macro repetida pra cada tabela restante (todas seguem o mesmo
-- molde: ADD COLUMN -> backfill -> FK -> índice -> NOT NULL). Escrita
-- por extenso (sem função/DO reaproveitado) de propósito — mais fácil
-- de ler e de rodar um pedaço na mão se algo der errado no meio.

-- clientes
ALTER TABLE clientes ADD COLUMN IF NOT EXISTS empresa_id BIGINT;
UPDATE clientes SET empresa_id = (SELECT id FROM empresas WHERE slug = 'minha-barbearia')
  WHERE empresa_id IS NULL;
DO $$
BEGIN
  IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'fk_clientes_empresa') THEN
    ALTER TABLE clientes ADD CONSTRAINT fk_clientes_empresa FOREIGN KEY (empresa_id) REFERENCES empresas(id) ON DELETE RESTRICT;
  END IF;
END $$;
CREATE INDEX IF NOT EXISTS ix_clientes_empresa ON clientes (empresa_id);
ALTER TABLE clientes ALTER COLUMN empresa_id SET NOT NULL;

-- uq_clientes_cpf era global — o mesmo CPF (pessoa física) pode muito
-- bem ser cliente de duas barbearias diferentes na plataforma; o que
-- não pode é se repetir DENTRO da mesma barbearia.
ALTER TABLE clientes DROP CONSTRAINT IF EXISTS uq_clientes_cpf;
CREATE UNIQUE INDEX IF NOT EXISTS ux_clientes_empresa_cpf ON clientes (empresa_id, cpf);

-- barbeiros
ALTER TABLE barbeiros ADD COLUMN IF NOT EXISTS empresa_id BIGINT;
UPDATE barbeiros SET empresa_id = (SELECT id FROM empresas WHERE slug = 'minha-barbearia')
  WHERE empresa_id IS NULL;
DO $$
BEGIN
  IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'fk_barbeiros_empresa') THEN
    ALTER TABLE barbeiros ADD CONSTRAINT fk_barbeiros_empresa FOREIGN KEY (empresa_id) REFERENCES empresas(id) ON DELETE RESTRICT;
  END IF;
END $$;
CREATE INDEX IF NOT EXISTS ix_barbeiros_empresa ON barbeiros (empresa_id);
ALTER TABLE barbeiros ALTER COLUMN empresa_id SET NOT NULL;

-- servicos
ALTER TABLE servicos ADD COLUMN IF NOT EXISTS empresa_id BIGINT;
UPDATE servicos SET empresa_id = (SELECT id FROM empresas WHERE slug = 'minha-barbearia')
  WHERE empresa_id IS NULL;
DO $$
BEGIN
  IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'fk_servicos_empresa') THEN
    ALTER TABLE servicos ADD CONSTRAINT fk_servicos_empresa FOREIGN KEY (empresa_id) REFERENCES empresas(id) ON DELETE RESTRICT;
  END IF;
END $$;
CREATE INDEX IF NOT EXISTS ix_servicos_empresa ON servicos (empresa_id);
ALTER TABLE servicos ALTER COLUMN empresa_id SET NOT NULL;

-- uq_servicos_nome era global — SEM esta troca, a segunda barbearia da
-- plataforma nem conseguiria cadastrar um serviço chamado "Corte" se a
-- primeira já tiver um com esse nome. Único passa a ser por barbearia.
ALTER TABLE servicos DROP CONSTRAINT IF EXISTS uq_servicos_nome;
CREATE UNIQUE INDEX IF NOT EXISTS ux_servicos_empresa_nome ON servicos (empresa_id, nome);

-- planos_assinatura
ALTER TABLE planos_assinatura ADD COLUMN IF NOT EXISTS empresa_id BIGINT;
UPDATE planos_assinatura SET empresa_id = (SELECT id FROM empresas WHERE slug = 'minha-barbearia')
  WHERE empresa_id IS NULL;
DO $$
BEGIN
  IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'fk_planos_assinatura_empresa') THEN
    ALTER TABLE planos_assinatura ADD CONSTRAINT fk_planos_assinatura_empresa FOREIGN KEY (empresa_id) REFERENCES empresas(id) ON DELETE RESTRICT;
  END IF;
END $$;
CREATE INDEX IF NOT EXISTS ix_planos_assinatura_empresa ON planos_assinatura (empresa_id);
ALTER TABLE planos_assinatura ALTER COLUMN empresa_id SET NOT NULL;

-- uq_planos_nome era global — mesmo raciocínio de uq_servicos_nome
-- acima ("Plano Mensal" vai se repetir entre barbearias diferentes).
ALTER TABLE planos_assinatura DROP CONSTRAINT IF EXISTS uq_planos_nome;
CREATE UNIQUE INDEX IF NOT EXISTS ux_planos_assinatura_empresa_nome ON planos_assinatura (empresa_id, nome);

-- assinaturas
ALTER TABLE assinaturas ADD COLUMN IF NOT EXISTS empresa_id BIGINT;
UPDATE assinaturas SET empresa_id = (SELECT id FROM empresas WHERE slug = 'minha-barbearia')
  WHERE empresa_id IS NULL;
DO $$
BEGIN
  IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'fk_assinaturas_empresa') THEN
    ALTER TABLE assinaturas ADD CONSTRAINT fk_assinaturas_empresa FOREIGN KEY (empresa_id) REFERENCES empresas(id) ON DELETE RESTRICT;
  END IF;
END $$;
CREATE INDEX IF NOT EXISTS ix_assinaturas_empresa ON assinaturas (empresa_id);
ALTER TABLE assinaturas ALTER COLUMN empresa_id SET NOT NULL;

-- agendamentos
ALTER TABLE agendamentos ADD COLUMN IF NOT EXISTS empresa_id BIGINT;
UPDATE agendamentos SET empresa_id = (SELECT id FROM empresas WHERE slug = 'minha-barbearia')
  WHERE empresa_id IS NULL;
DO $$
BEGIN
  IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'fk_agendamentos_empresa') THEN
    ALTER TABLE agendamentos ADD CONSTRAINT fk_agendamentos_empresa FOREIGN KEY (empresa_id) REFERENCES empresas(id) ON DELETE RESTRICT;
  END IF;
END $$;
CREATE INDEX IF NOT EXISTS ix_agendamentos_empresa ON agendamentos (empresa_id);
ALTER TABLE agendamentos ALTER COLUMN empresa_id SET NOT NULL;

-- pagamentos
ALTER TABLE pagamentos ADD COLUMN IF NOT EXISTS empresa_id BIGINT;
UPDATE pagamentos SET empresa_id = (SELECT id FROM empresas WHERE slug = 'minha-barbearia')
  WHERE empresa_id IS NULL;
DO $$
BEGIN
  IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'fk_pagamentos_empresa') THEN
    ALTER TABLE pagamentos ADD CONSTRAINT fk_pagamentos_empresa FOREIGN KEY (empresa_id) REFERENCES empresas(id) ON DELETE RESTRICT;
  END IF;
END $$;
CREATE INDEX IF NOT EXISTS ix_pagamentos_empresa ON pagamentos (empresa_id);
ALTER TABLE pagamentos ALTER COLUMN empresa_id SET NOT NULL;

-- solicitacoes_plano
ALTER TABLE solicitacoes_plano ADD COLUMN IF NOT EXISTS empresa_id BIGINT;
UPDATE solicitacoes_plano SET empresa_id = (SELECT id FROM empresas WHERE slug = 'minha-barbearia')
  WHERE empresa_id IS NULL;
DO $$
BEGIN
  IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'fk_solicitacoes_plano_empresa') THEN
    ALTER TABLE solicitacoes_plano ADD CONSTRAINT fk_solicitacoes_plano_empresa FOREIGN KEY (empresa_id) REFERENCES empresas(id) ON DELETE RESTRICT;
  END IF;
END $$;
CREATE INDEX IF NOT EXISTS ix_solicitacoes_plano_empresa ON solicitacoes_plano (empresa_id);
ALTER TABLE solicitacoes_plano ALTER COLUMN empresa_id SET NOT NULL;

-- premios_ranking
ALTER TABLE premios_ranking ADD COLUMN IF NOT EXISTS empresa_id BIGINT;
UPDATE premios_ranking SET empresa_id = (SELECT id FROM empresas WHERE slug = 'minha-barbearia')
  WHERE empresa_id IS NULL;
DO $$
BEGIN
  IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'fk_premios_ranking_empresa') THEN
    ALTER TABLE premios_ranking ADD CONSTRAINT fk_premios_ranking_empresa FOREIGN KEY (empresa_id) REFERENCES empresas(id) ON DELETE RESTRICT;
  END IF;
END $$;
CREATE INDEX IF NOT EXISTS ix_premios_ranking_empresa ON premios_ranking (empresa_id);
ALTER TABLE premios_ranking ALTER COLUMN empresa_id SET NOT NULL;

-- uq_premios_ranking_mes_ano_posicao era global (mes, ano, posicao) —
-- sem empresa_id, só UMA barbearia no sistema inteiro conseguiria
-- configurar o prêmio de "1º lugar de março/2026".
ALTER TABLE premios_ranking DROP CONSTRAINT IF EXISTS uq_premios_ranking_mes_ano_posicao;
CREATE UNIQUE INDEX IF NOT EXISTS ux_premios_ranking_empresa_mes_ano_posicao ON premios_ranking (empresa_id, mes, ano, posicao);

-- configuracoes_site: deixa de ser uma linha global única (id sempre 1,
-- coluna sem geração automática) e passa a ter uma linha por barbearia.
--
-- Antes disso, precisa resolver um problema à parte: a coluna `id` foi
-- criada em 01_schema.sql como "BIGINT PRIMARY KEY" comum (SEM
-- GENERATED ... AS IDENTITY), porque só existia a linha fixa id=1,
-- inserida "na mão" pelo próprio 01_schema.sql — nunca precisou que o
-- banco gerasse um id sozinho. Agora que cada barbearia ganha sua
-- própria linha (criada por EmpresaService.FnCriarAsync, sem informar
-- id nenhum — igual toda outra tabela), o banco PRECISA saber gerar
-- esse id sozinho, senão o INSERT da 2ª barbearia em diante falha
-- (id não teria de onde vir). Por isso a conversão abaixo, ANTES do
-- resto: torna a coluna identity (GENERATED ALWAYS, pra bater com
-- ConfiguracaoSiteConfiguration.cs) e avança a sequência interna pra
-- depois do maior id já existente (evita colisão com a linha id=1 que
-- já está lá).
DO $$
BEGIN
  IF NOT EXISTS (
    SELECT 1 FROM pg_attribute a
    JOIN pg_class c ON a.attrelid = c.oid
    WHERE c.relname = 'configuracoes_site' AND a.attname = 'id' AND a.attidentity <> ''
  ) THEN
    ALTER TABLE configuracoes_site ALTER COLUMN id ADD GENERATED ALWAYS AS IDENTITY;
    PERFORM setval(
      pg_get_serial_sequence('configuracoes_site', 'id'),
      COALESCE((SELECT MAX(id) FROM configuracoes_site), 0) + 1,
      false
    );
  END IF;
END $$;

ALTER TABLE configuracoes_site ADD COLUMN IF NOT EXISTS empresa_id BIGINT;
UPDATE configuracoes_site SET empresa_id = (SELECT id FROM empresas WHERE slug = 'minha-barbearia')
  WHERE empresa_id IS NULL;
DO $$
BEGIN
  IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'fk_configuracoes_site_empresa') THEN
    ALTER TABLE configuracoes_site ADD CONSTRAINT fk_configuracoes_site_empresa FOREIGN KEY (empresa_id) REFERENCES empresas(id) ON DELETE RESTRICT;
  END IF;
END $$;
ALTER TABLE configuracoes_site ALTER COLUMN empresa_id SET NOT NULL;
CREATE UNIQUE INDEX IF NOT EXISTS ux_configuracoes_site_empresa ON configuracoes_site (empresa_id);
