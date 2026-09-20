-- =====================================================================
-- Sistema de Gestão de Barbearia — Schema do banco de dados
-- PostgreSQL 16+
--
-- Convenção de nomenclatura: snake_case (idiomático em Postgres).
-- Quando a API em .NET (EF Core / Npgsql) mapear essas tabelas, cada
-- nome_assim vira NomeAssim em C# automaticamente, usando o pacote
-- Npgsql.EntityFrameworkCore.PostgreSQL com a convenção snake_case.
-- =====================================================================

-- Extensão necessária para o EXCLUDE constraint que impede horários
-- conflitantes na tabela agendamentos (ver comentário mais abaixo).
CREATE EXTENSION IF NOT EXISTS btree_gist;

-- =====================================================================
-- ENUMS: definidos em C# (Domain), persistidos como INT
--
-- Decisão de arquitetura: em vez de CREATE TYPE ... AS ENUM, os
-- "status"/"tipo" abaixo são colunas INT com um CHECK constraint
-- restringindo o intervalo válido. Por quê:
--
--   1. Clean Architecture: a regra "quais status existem" é regra de
--      negócio e deve viver no Domain (C#), não presa a uma feature
--      específica do Postgres. O banco é só detalhe de infraestrutura.
--   2. INT é o mapeamento padrão do EF Core para enum (sem precisar de
--      .HasConversion<string>()) — mais compacto e mais rápido de
--      comparar/indexar do que texto.
--   3. Ainda ganhamos a proteção do banco: um INSERT com valor fora do
--      intervalo é rejeitado pelo CHECK, mesmo vindo de fora da API.
--
-- O PREÇO desse formato — e por isso ele exige mais disciplina do que
-- VARCHAR: com INT, o significado de cada número só existe "de cabeça".
-- Se o enum em C# for reordenado (ex.: alguém troca a ordem dos
-- membros), os números já gravados no banco silenciosamente passam a
-- significar outra coisa — sem erro de compilação, sem exceção, só
-- dado errado. A defesa é DISCIPLINA, não código:
--
--   - No C#, SEMPRE atribua o valor numérico explicitamente:
--       public enum TipoUsuario { Admin = 0, Barbeiro = 1 }
--     nunca escreva "public enum TipoUsuario { Admin, Barbeiro }" e
--     deixe o compilador escolher os números.
--   - Só ADICIONE valores no final. Nunca reordene, nunca reaproveite
--     um número de um valor removido.
--
-- MAPA CANÔNICO (fonte da verdade — replicar exatamente esses números
-- nos enums em C# na Fase 2, em Barbearia.Domain.Enums):
--
--   TipoUsuario        0=Admin           1=Barbeiro        2=Comum
--   StatusRegistro     0=Inativo         1=Ativo           2=Bloqueado
--     (reaproveitado como está, sem criar um enum por tabela, em:
--      usuarios.status, barbeiros.status, clientes.status, servicos.status,
--      planos_assinatura.status — todos têm exatamente o mesmo
--      significado; ficam campos separados porque cada linha pode
--      estar Bloqueada/Inativa independentemente das outras. Em C#
--      use o MESMO enum StatusRegistro em todas essas propriedades,
--      não um enum por tabela redefinindo os mesmos 3 valores.
--      Repare que o DEFAULT dessas colunas é 1 (Ativo), não 0 — 0 aqui
--      é Inativo, então um registro novo precisa nascer com 1
--      explicitamente, nunca contar com o zero "de fábrica".)
--   StatusAssinatura   0=Ativa           1=Suspensa        2=Cancelada       3=Expirada
--   StatusAgendamento  0=Agendado        1=Confirmado      2=EmAtendimento
--                      3=Concluido       4=Cancelado       5=NaoCompareceu
--   FormaPagamento     0=Dinheiro        1=Pix             2=CartaoCredito
--                      3=CartaoDebito    4=Assinatura
--   StatusPagamento    0=Pendente        1=Pago            2=Cancelado       3=Reembolsado
--   StatusSolicitacaoPlano  0=Pendente    1=Aceita          2=Rejeitada
-- =====================================================================


-- =====================================================================
-- FUNÇÃO E TRIGGER DE AUDITORIA
--
-- Toda tabela "de negócio" tem criado_em e atualizado_em. Em vez de
-- confiar que a aplicação sempre vai lembrar de atualizar
-- atualizado_em em todo UPDATE, colocamos essa responsabilidade no
-- banco: é impossível esquecer, e funciona mesmo se alguém rodar um
-- UPDATE manual via psql.
-- =====================================================================

CREATE OR REPLACE FUNCTION atualizar_timestamp()
RETURNS TRIGGER AS $$
BEGIN
  NEW.atualizado_em = now();
  RETURN NEW;
END;
$$ LANGUAGE plpgsql;


-- =====================================================================
-- USUARIOS — contas de acesso ao painel (dono/admin e barbeiros)
--
-- Decisão de design: clientes NÃO têm login nesta v1. O sistema é um
-- painel operado pela barbearia (dono e barbeiros), não um app para
-- o cliente final. Isso simplifica bastante o MVP; dá pra evoluir
-- depois para um "portal do cliente" sem quebrar o que já existe.
--
-- "status" substitui o antigo "ativo BOOLEAN": um booleano só
-- distingue 2 estados, e "conta bloqueada" (ex.: por segurança, ou
-- suspensa pelo admin) é semanticamente diferente de "conta
-- desativada porque a pessoa saiu da barbearia" — por isso 3 estados.
-- =====================================================================

CREATE TABLE usuarios (
  id              BIGINT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
  nome_completo   VARCHAR(150) NOT NULL,
  email           VARCHAR(150) NOT NULL,
  senha_hash      VARCHAR(255) NOT NULL, -- NUNCA guardar senha em texto puro; hash com BCrypt/Argon2 feito na API
  tipo            INT NOT NULL DEFAULT 2 CHECK (tipo IN (0, 1, 2)),
                    -- 0=Admin, 1=Barbeiro, 2=Comum (quem se autocadastra pelo /cadastro
                    -- vira Comum por padrão — só a PRIMEIRA conta do sistema vira Admin
                    -- automaticamente; ver AutenticacaoService.RegistrarAsync)
  status          INT NOT NULL DEFAULT 1 CHECK (status IN (0, 1, 2)),
                    -- 0=Inativo, 1=Ativo, 2=Bloqueado
  telefone        VARCHAR(20), -- contato pessoal da aba "Meu perfil" (ver 08_migracao_perfil_usuario.sql) — opcional, separado do telefone de Cliente/Barbeiro
  foto_url        VARCHAR(500), -- URL relativa do upload (ver Barbearia.Infrastructure/Storage/ArmazenamentoArquivosLocal); NULL usa o avatar padrão (iniciais do nome)
  criado_em       TIMESTAMPTZ NOT NULL DEFAULT now(),
  atualizado_em   TIMESTAMPTZ NOT NULL DEFAULT now(),

  CONSTRAINT uq_usuarios_email UNIQUE (email)
);

CREATE TRIGGER trg_usuarios_atualizado
  BEFORE UPDATE ON usuarios
  FOR EACH ROW EXECUTE FUNCTION atualizar_timestamp();


-- =====================================================================
-- BARBEIROS — dados profissionais de quem atende
--
-- Relação 1:1 com usuarios (todo barbeiro tem uma conta de login;
-- nem toda conta de login é necessariamente um barbeiro — o "admin"
-- pode ser só o dono, sem atender clientes). Por isso barbeiros é
-- uma tabela separada de usuarios, e não um campo dentro dela.
-- =====================================================================

CREATE TABLE barbeiros (
  id                    BIGINT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
  usuario_id            BIGINT NOT NULL REFERENCES usuarios(id) ON DELETE CASCADE,
  telefone              VARCHAR(20),
  status                INT NOT NULL DEFAULT 1 CHECK (status IN (0, 1, 2)),
                          -- 0=Inativo, 1=Ativo, 2=Bloqueado (mesmo sentido de usuarios.status;
                          -- a aplicação é responsável por manter os dois sincronizados)
  ausente               BOOLEAN NOT NULL DEFAULT false,
                          -- "de folga hoje" — À PARTE do status acima (ver 05_migracao_ausencia_barbeiro.sql).
                          -- O próprio barbeiro liga/desliga; enquanto ligado, ele não aparece
                          -- pra Comum/Cliente escolherem na hora de pedir um agendamento.
  bio                   TEXT,
                          -- Apresentação livre do próprio barbeiro (ver 12_migracao_perfil_barbearia.sql)
                          -- pra aba "Sobre a barbearia" — self-service, ele mesmo escreve sobre si.
  especialidade         VARCHAR(150),
                          -- Rótulo curto (ex.: "Corte degradê, barba clássica") — o "subtítulo" do
                          -- card do barbeiro, ao lado do nome.
  criado_em             TIMESTAMPTZ NOT NULL DEFAULT now(),
  atualizado_em         TIMESTAMPTZ NOT NULL DEFAULT now(),

  CONSTRAINT uq_barbeiros_usuario UNIQUE (usuario_id)
);

CREATE TRIGGER trg_barbeiros_atualizado
  BEFORE UPDATE ON barbeiros
  FOR EACH ROW EXECUTE FUNCTION atualizar_timestamp();


-- =====================================================================
-- CLIENTES
-- =====================================================================

CREATE TABLE clientes (
  id                BIGINT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
  nome_completo     VARCHAR(150) NOT NULL,
  telefone          VARCHAR(20), -- opcional: um cliente auto-provisionado a partir de um usuário Comum (ver usuario_id) nasce sem telefone
  email             VARCHAR(150),
  cpf               VARCHAR(14), -- formato 000.000.000-00; opcional (nem toda barbearia exige)
  data_nascimento   DATE,
  observacoes       TEXT,
  status            INT NOT NULL DEFAULT 1 CHECK (status IN (0, 1, 2)),
                      -- 0=Inativo, 1=Ativo, 2=Bloqueado
  usuario_id        BIGINT REFERENCES usuarios(id) ON DELETE SET NULL,
                      -- NULL na maioria (cliente cadastrado manualmente pelo staff). Só é
                      -- preenchido quando este Cliente nasceu automaticamente pra um usuário
                      -- Comum que solicitou seu primeiro agendamento (ver 04_...sql / AgendamentoService.SolicitarAsync)
  criado_em         TIMESTAMPTZ NOT NULL DEFAULT now(),
  atualizado_em     TIMESTAMPTZ NOT NULL DEFAULT now(),

  CONSTRAINT uq_clientes_cpf UNIQUE (cpf),
  CONSTRAINT uq_clientes_usuario UNIQUE (usuario_id) -- Postgres permite múltiplos NULL numa coluna UNIQUE, então não afeta os demais clientes
);

-- Índice para a busca mais comum do dia a dia: "achar cliente pelo telefone"
CREATE INDEX ix_clientes_telefone ON clientes (telefone);

CREATE TRIGGER trg_clientes_atualizado
  BEFORE UPDATE ON clientes
  FOR EACH ROW EXECUTE FUNCTION atualizar_timestamp();


-- =====================================================================
-- SERVICOS — catálogo (corte, barba, sobrancelha, combo, etc.)
-- =====================================================================

CREATE TABLE servicos (
  id                BIGINT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
  nome              VARCHAR(100) NOT NULL,
  descricao         TEXT,
  duracao_minutos   SMALLINT NOT NULL CHECK (duracao_minutos > 0),
  preco             NUMERIC(10,2) NOT NULL CHECK (preco >= 0), -- NUMERIC, nunca FLOAT, para dinheiro
  categoria         INT NOT NULL DEFAULT 6 CHECK (categoria IN (0, 1, 2, 3, 4, 5, 6)),
                      -- 0=Corte, 1=Barba, 2=ComboCorteEBarba, 3=Sobrancelha, 4=Coloracao,
                      -- 5=Tratamento, 6=Outro (ver 07_migracao_categoria_servico.sql) —
                      -- só organiza a tela de Serviços, não afeta preço/duração/agendamento
  status            INT NOT NULL DEFAULT 1 CHECK (status IN (0, 1, 2)),
                      -- 0=Inativo, 1=Ativo, 2=Bloqueado
  criado_em         TIMESTAMPTZ NOT NULL DEFAULT now(),
  atualizado_em     TIMESTAMPTZ NOT NULL DEFAULT now(),

  CONSTRAINT uq_servicos_nome UNIQUE (nome)
);

CREATE TRIGGER trg_servicos_atualizado
  BEFORE UPDATE ON servicos
  FOR EACH ROW EXECUTE FUNCTION atualizar_timestamp();


-- =====================================================================
-- PLANOS_ASSINATURA — ex: "Plano Mensal Corte", "Plano Premium"
-- =====================================================================

CREATE TABLE planos_assinatura (
  id              BIGINT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
  nome            VARCHAR(100) NOT NULL,
  descricao       TEXT,
  preco_mensal    NUMERIC(10,2) NOT NULL CHECK (preco_mensal >= 0),
  status          INT NOT NULL DEFAULT 1 CHECK (status IN (0, 1, 2)),
                    -- 0=Inativo, 1=Ativo, 2=Bloqueado
  criado_em       TIMESTAMPTZ NOT NULL DEFAULT now(),
  atualizado_em   TIMESTAMPTZ NOT NULL DEFAULT now(),

  CONSTRAINT uq_planos_nome UNIQUE (nome)
);

CREATE TRIGGER trg_planos_atualizado
  BEFORE UPDATE ON planos_assinatura
  FOR EACH ROW EXECUTE FUNCTION atualizar_timestamp();


-- =====================================================================
-- PLANO_SERVICOS — tabela de junção N:N entre planos e serviços
--
-- Um plano pode incluir vários serviços, e cada serviço pode aparecer
-- em vários planos, cada um com um limite mensal diferente
-- (ex.: "Plano Básico" = 2 cortes/mês; "Plano Premium" = 4 cortes/mês
-- + 2 barbas/mês). Por isso a chave primária é composta
-- (plano_id, servico_id): o par não pode se repetir, mas cada linha
-- carrega seu próprio limite_mensal.
-- =====================================================================

CREATE TABLE plano_servicos (
  plano_id        BIGINT NOT NULL REFERENCES planos_assinatura(id) ON DELETE CASCADE,
  servico_id      BIGINT NOT NULL REFERENCES servicos(id) ON DELETE RESTRICT,
  limite_mensal   SMALLINT NOT NULL CHECK (limite_mensal > 0),

  PRIMARY KEY (plano_id, servico_id)
);


-- =====================================================================
-- ASSINATURAS — vínculo de um cliente a um plano, com vigência
-- =====================================================================

CREATE TABLE assinaturas (
  id                BIGINT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
  cliente_id        BIGINT NOT NULL REFERENCES clientes(id) ON DELETE RESTRICT,
  plano_id          BIGINT NOT NULL REFERENCES planos_assinatura(id) ON DELETE RESTRICT,
  data_inicio       DATE NOT NULL,
  data_fim          DATE, -- NULL = sem previsão de término (renovação automática)
  data_vencimento   DATE NOT NULL, -- data EXATA do próximo vencimento (não é mais "dia do mês" — ver Assinatura.cs)
  status            INT NOT NULL DEFAULT 0 CHECK (status IN (0, 1, 2, 3)),
                      -- 0=Ativa, 1=Suspensa, 2=Cancelada, 3=Expirada
  criado_em         TIMESTAMPTZ NOT NULL DEFAULT now(),
  atualizado_em     TIMESTAMPTZ NOT NULL DEFAULT now(),

  CONSTRAINT ck_assinaturas_datas CHECK (data_fim IS NULL OR data_fim >= data_inicio),
  CONSTRAINT ck_assinaturas_vencimento CHECK (data_vencimento >= data_inicio)
);

CREATE INDEX ix_assinaturas_cliente ON assinaturas (cliente_id);
-- Índice parcial: só indexa o que realmente é consultado com frequência
-- (verificar assinaturas ativas de um cliente), economizando espaço.
CREATE INDEX ix_assinaturas_status_ativa ON assinaturas (cliente_id) WHERE status = 0; -- 0=Ativa

CREATE TRIGGER trg_assinaturas_atualizado
  BEFORE UPDATE ON assinaturas
  FOR EACH ROW EXECUTE FUNCTION atualizar_timestamp();


-- =====================================================================
-- SOLICITACOES_PLANO — pedido de um usuário Comum pra assinar um
-- plano, aguardando o Admin/Barbeiro aceitar (o que cria a Assinatura
-- de verdade, com as datas que só o staff informa) ou rejeitar. Mesma
-- ideia do fluxo de solicitação de agendamento, aplicada a plano — ver
-- 06_migracao_solicitacao_plano.sql / SolicitacaoPlanoService.
-- =====================================================================

CREATE TABLE solicitacoes_plano (
  id                BIGINT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
  usuario_id        BIGINT NOT NULL REFERENCES usuarios(id) ON DELETE CASCADE,
  plano_id          BIGINT NOT NULL REFERENCES planos_assinatura(id) ON DELETE RESTRICT,
  email             VARCHAR(150) NOT NULL,
  telefone          VARCHAR(20) NOT NULL,
  status            INT NOT NULL DEFAULT 0 CHECK (status IN (0, 1, 2)),
                      -- 0=Pendente, 1=Aceita, 2=Rejeitada
  mensagem_resposta TEXT,
  assinatura_id     BIGINT REFERENCES assinaturas(id) ON DELETE SET NULL,
                      -- preenchido só quando Aceita (ver SolicitacaoPlano.Aceitar)
  criado_em         TIMESTAMPTZ NOT NULL DEFAULT now(),
  atualizado_em     TIMESTAMPTZ NOT NULL DEFAULT now()
);

CREATE INDEX ix_solicitacoes_plano_usuario ON solicitacoes_plano (usuario_id);

CREATE TRIGGER trg_solicitacoes_plano_atualizado
  BEFORE UPDATE ON solicitacoes_plano
  FOR EACH ROW EXECUTE FUNCTION atualizar_timestamp();


-- =====================================================================
-- HORARIOS_TRABALHO — agenda semanal fixa de cada barbeiro
-- (ex.: Terça a Sábado, 09:00–18:00)
-- =====================================================================

CREATE TABLE horarios_trabalho (
  id            BIGINT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
  barbeiro_id   BIGINT NOT NULL REFERENCES barbeiros(id) ON DELETE CASCADE,
  dia_semana    SMALLINT NOT NULL CHECK (dia_semana BETWEEN 0 AND 6), -- 0 = domingo ... 6 = sábado
  hora_inicio   TIME NOT NULL,
  hora_fim      TIME NOT NULL,

  CONSTRAINT ck_horarios_intervalo CHECK (hora_fim > hora_inicio),
  CONSTRAINT uq_horarios_barbeiro_dia UNIQUE (barbeiro_id, dia_semana, hora_inicio)
);


-- =====================================================================
-- BLOQUEIOS_AGENDA — férias, atestados, ausências pontuais
-- =====================================================================

CREATE TABLE bloqueios_agenda (
  id            BIGINT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
  barbeiro_id   BIGINT NOT NULL REFERENCES barbeiros(id) ON DELETE CASCADE,
  inicio        TIMESTAMPTZ NOT NULL,
  fim           TIMESTAMPTZ NOT NULL,
  motivo        VARCHAR(200),

  CONSTRAINT ck_bloqueios_intervalo CHECK (fim > inicio)
);

CREATE INDEX ix_bloqueios_barbeiro ON bloqueios_agenda (barbeiro_id);


-- =====================================================================
-- AGENDAMENTOS — o coração do sistema
--
-- Ponto de destaque: o EXCLUDE constraint abaixo impede, DENTRO DO
-- PRÓPRIO BANCO, que o mesmo barbeiro fique com dois agendamentos que
-- se sobrepõem no tempo. Isso é uma garantia muito mais forte do que
-- validar "na mão" na API: mesmo que dois pedidos cheguem ao mesmo
-- tempo (condição de corrida) ou que alguém insira direto no banco,
-- é fisicamente impossível gravar um conflito de horário.
--
-- tstzrange(inicio, fim) transforma as duas colunas num "intervalo de
-- tempo com timezone" (tstzrange, porque inicio/fim são TIMESTAMPTZ —
-- usar tsrange aqui daria erro de tipo), e o operador && verifica se
-- dois intervalos se sobrepõem.
-- A cláusula WHERE restringe a regra a agendamentos que ainda ocupam
-- a agenda (cancelados e não-comparecidos liberam o horário).
-- =====================================================================

CREATE TABLE agendamentos (
  id                BIGINT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
  cliente_id        BIGINT NOT NULL REFERENCES clientes(id) ON DELETE RESTRICT,
  barbeiro_id       BIGINT NOT NULL REFERENCES barbeiros(id) ON DELETE RESTRICT,
  servico_id        BIGINT NOT NULL REFERENCES servicos(id) ON DELETE RESTRICT,
  assinatura_id     BIGINT REFERENCES assinaturas(id) ON DELETE SET NULL, -- preenchido só se o corte foi "usando o plano"
  inicio            TIMESTAMPTZ NOT NULL,
  fim               TIMESTAMPTZ NOT NULL,
  status            INT NOT NULL DEFAULT 0 CHECK (status IN (0, 1, 2, 3, 4, 5, 6, 7)),
                      -- 0=Agendado, 1=Confirmado, 2=EmAtendimento, 3=Concluido,
                      -- 4=Cancelado, 5=NaoCompareceu, 6=Pendente, 7=Rejeitado
                      -- (6 e 7 são do fluxo de SOLICITAÇÃO do próprio cliente — ver Agendamento.Solicitar no Domain)
  preco_cobrado     NUMERIC(10,2) NOT NULL CHECK (preco_cobrado >= 0),
  observacoes       TEXT,
  mensagem_resposta TEXT, -- recado opcional do Admin/Barbeiro pro cliente ao confirmar/rejeitar/cancelar/etc.
  criado_em         TIMESTAMPTZ NOT NULL DEFAULT now(),
  atualizado_em     TIMESTAMPTZ NOT NULL DEFAULT now(),

  CONSTRAINT ck_agendamentos_intervalo CHECK (fim > inicio),

  CONSTRAINT sem_conflito_horario EXCLUDE USING gist (
    barbeiro_id WITH =,
    tstzrange(inicio, fim) WITH &&
  ) WHERE (status NOT IN (4, 5, 7)) -- exclui Cancelado(4), NaoCompareceu(5) e Rejeitado(7): esses liberam o horário. Pendente(6) NÃO libera — reserva o horário até o barbeiro decidir.
);

CREATE INDEX ix_agendamentos_cliente ON agendamentos (cliente_id);
CREATE INDEX ix_agendamentos_barbeiro_inicio ON agendamentos (barbeiro_id, inicio);
-- Consulta mais comum da tela de agenda: "o que tem marcado hoje/nesta semana"
CREATE INDEX ix_agendamentos_inicio ON agendamentos (inicio);

CREATE TRIGGER trg_agendamentos_atualizado
  BEFORE UPDATE ON agendamentos
  FOR EACH ROW EXECUTE FUNCTION atualizar_timestamp();


-- =====================================================================
-- PAGAMENTOS
--
-- Separado de agendamentos porque um pagamento pode não estar ligado
-- a um agendamento específico (ex.: pagamento da mensalidade da
-- assinatura), e porque o ciclo de vida de "cobrança" é diferente do
-- ciclo de vida de "atendimento".
-- =====================================================================

CREATE TABLE pagamentos (
  id              BIGINT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
  agendamento_id  BIGINT REFERENCES agendamentos(id) ON DELETE SET NULL,
  assinatura_id   BIGINT REFERENCES assinaturas(id) ON DELETE SET NULL,
  cliente_id      BIGINT NOT NULL REFERENCES clientes(id) ON DELETE RESTRICT,
  valor           NUMERIC(10,2) NOT NULL CHECK (valor >= 0),
  forma           INT NOT NULL CHECK (forma IN (0, 1, 2, 3, 4)),
                    -- 0=Dinheiro, 1=Pix, 2=CartaoCredito, 3=CartaoDebito, 4=Assinatura
  status          INT NOT NULL DEFAULT 0 CHECK (status IN (0, 1, 2, 3)),
                    -- 0=Pendente, 1=Pago, 2=Cancelado, 3=Reembolsado
  pago_em         TIMESTAMPTZ,
  criado_em       TIMESTAMPTZ NOT NULL DEFAULT now(),
  atualizado_em   TIMESTAMPTZ NOT NULL DEFAULT now(),

  CONSTRAINT ck_pagamentos_origem CHECK (agendamento_id IS NOT NULL OR assinatura_id IS NOT NULL)
);

CREATE INDEX ix_pagamentos_cliente ON pagamentos (cliente_id);
CREATE INDEX ix_pagamentos_status ON pagamentos (status);

CREATE TRIGGER trg_pagamentos_atualizado
  BEFORE UPDATE ON pagamentos
  FOR EACH ROW EXECUTE FUNCTION atualizar_timestamp();


-- =====================================================================
-- CONFIGURACOES_SITE — marca/aparência do site (nome exibido, logo, cor
-- de destaque) e as informações públicas do negócio (descrição,
-- endereço, telefone, Instagram, horário de funcionamento). UMA LINHA
-- SÓ (id sempre 1, ver Barbearia.Domain.Entities.ConfiguracaoSite.IdUnico)
-- — não existe "criar outra" nem "listar"; só leitura (pública, inclusive
-- na tela de Login). A alteração se divide em dois níveis (ver
-- ConfiguracaoSiteController): nome/logo/cor é decisão de dono do
-- negócio, só Admin; as demais colunas (a partir de descricao, ver
-- 12_migracao_perfil_barbearia.sql) e a galeria de fotos (tabela
-- fotos_barbearia, abaixo) são operação do dia a dia, Admin OU Barbeiro.
-- Ver também 09_migracao_configuracao_site.sql.
-- =====================================================================

CREATE TABLE configuracoes_site (
  id                      BIGINT PRIMARY KEY,
  nome_barbearia          VARCHAR(100) NOT NULL DEFAULT 'Barbearia',
  logo_url                VARCHAR(500),
  cor_primaria            VARCHAR(7), -- "#334562" — null usa a paleta padrão do sistema
  descricao               TEXT,
  endereco                VARCHAR(300),
  telefone                VARCHAR(20),
  instagram               VARCHAR(100),
  horario_funcionamento   VARCHAR(200)
);

INSERT INTO configuracoes_site (id, nome_barbearia) VALUES (1, 'Barbearia');


-- =====================================================================
-- FOTOS_BARBEARIA — galeria de fotos do espaço, exibida na aba "Sobre a
-- barbearia" (ver ConfiguracaoSite.Fotos/paginas/SobreABarbearia.jsx).
-- Sempre pertence à ÚNICA linha de configuracoes_site (não existe outra
-- galeria) — igual horarios_trabalho pertence a UM barbeiro, aqui cada
-- foto pertence à barbearia como um todo. Sem coluna de ordem: a ordem
-- de exibição é a de inserção (id crescente), suficiente pra uma
-- galeria que só cresce por "adicionar mais uma foto".
-- =====================================================================

CREATE TABLE fotos_barbearia (
  id                     BIGINT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
  configuracao_site_id   BIGINT NOT NULL REFERENCES configuracoes_site(id) ON DELETE CASCADE,
  url                    VARCHAR(500) NOT NULL
);

CREATE INDEX ix_fotos_barbearia_configuracao_site_id ON fotos_barbearia (configuracao_site_id);


-- =====================================================================
-- PREMIOS_RANKING — prêmio configurado pelo Admin/Barbeiro para uma
-- posição do pódio do Ranking de Cortes de um mês específico. NÃO existe
-- tabela "ranking": quem cortou mais é sempre calculado na hora, direto
-- de agendamentos.status = Concluido (ver RankingService) — só o prêmio
-- de cada posição precisa ficar salvo, porque é reconfigurado do zero
-- todo mês (sem cópia automática do mês anterior). Ver
-- 10_migracao_ranking.sql.
-- =====================================================================

CREATE TABLE premios_ranking (
  id            BIGINT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
  mes           SMALLINT NOT NULL CHECK (mes BETWEEN 1 AND 12),
  ano           SMALLINT NOT NULL CHECK (ano >= 2000),
  posicao       SMALLINT NOT NULL CHECK (posicao >= 1), -- 1 = campeão do mês, 2 = vice, ...
  descricao     VARCHAR(200) NOT NULL,
  criado_em     TIMESTAMPTZ NOT NULL DEFAULT now(),
  atualizado_em TIMESTAMPTZ NOT NULL DEFAULT now(),

  CONSTRAINT uq_premios_ranking_mes_ano_posicao UNIQUE (mes, ano, posicao)
);

CREATE TRIGGER trg_premios_ranking_atualizado
  BEFORE UPDATE ON premios_ranking
  FOR EACH ROW EXECUTE FUNCTION atualizar_timestamp();

-- =====================================================================
-- Registro de migrações já cobertas por este schema.
--
-- Este arquivo já nasce com TUDO que as migrações incrementais
-- (database/NN_migracao_*.sql) fariam num banco mais antigo — é assim
-- que cada uma delas se apresenta ("só é necessária se você já criou seu
-- banco ANTES desta mudança; criando do zero, ignore este arquivo").
--
-- Mas o MigrationRunner (ver Barbearia.Infrastructure/Persistencia/
-- MigrationRunner.cs) não tem como adivinhar isso sozinho: ele só sabe o
-- que já rodou olhando a tabela migracoes_aplicadas. Sem este INSERT, um
-- banco criado direto deste 01_schema.sql nasce com essa tabela vazia, e
-- a Api tentaria reaplicar cada migração por cima de uma estrutura que
-- já as tem — na melhor das hipóteses um no-op (graças ao "IF NOT
-- EXISTS"/DO $$ de cada uma), na pior um erro de "já existe" (foi o que
-- aconteceu com o gatilho de premios_ranking, antes de ele ganhar essa
-- mesma proteção).
--
-- Criar a tabela aqui também (com "IF NOT EXISTS") é seguro: é a mesma
-- criação que o MigrationRunner faz sozinho ao subir, só que adiantada.
--
-- IMPORTANTE pra quem for mexer no schema depois: toda vez que uma nova
-- migração "NN_migracao_*.sql" for criada E incorporada aqui no
-- 01_schema.sql (regenerando este arquivo pra refletir o estado final),
-- o nome dela precisa ser adicionado na lista abaixo também — senão um
-- banco novo, criado direto daqui, ficaria com esse gatilho ligado de
-- novo pra tentar reaplicar algo que já está presente.
-- =====================================================================

CREATE TABLE IF NOT EXISTS migracoes_aplicadas (
  nome_arquivo VARCHAR(255) PRIMARY KEY,
  aplicado_em  TIMESTAMPTZ NOT NULL DEFAULT now()
);

INSERT INTO migracoes_aplicadas (nome_arquivo) VALUES
  ('03_migracao_tipo_comum.sql'),
  ('04_migracao_solicitacao_agendamento.sql'),
  ('05_migracao_ausencia_barbeiro.sql'),
  ('06_migracao_solicitacao_plano.sql'),
  ('07_migracao_categoria_servico.sql'),
  ('08_migracao_perfil_usuario.sql'),
  ('09_migracao_configuracao_site.sql'),
  ('10_migracao_ranking.sql'),
  ('11_migracao_data_vencimento.sql'),
  ('12_migracao_perfil_barbearia.sql')
ON CONFLICT (nome_arquivo) DO NOTHING;

-- =====================================================================
-- EXCEÇÃO DELIBERADA: 13_migracao_multi_barbearia.sql (modelo
-- multi-barbearia/multi-tenant) NÃO está incorporada acima nas
-- CREATE TABLE deste arquivo, e o nome dela também NÃO entra na lista
-- de INSERT acima — diferente de toda migração anterior (03-12).
--
-- Motivo: 13_migracao_multi_barbearia.sql é bem mais complexa que as
-- anteriores (cria tabela nova, mexe em índices únicos existentes,
-- converte uma coluna de id pra identity) e, sem conseguir compilar/
-- rodar testes neste ambiente, o risco de reescrever tudo isso "na mão"
-- aqui de novo — e os dois arquivos saírem sutilmente diferentes um do
-- outro — é maior do que o benefício de pular sua execução num banco
-- novo. Ela já é 100% idempotente (todo ADD COLUMN/CREATE INDEX/etc é
-- "IF NOT EXISTS"), então deixá-la de fora desta lista só significa que
-- um banco criado do zero a partir deste 01_schema.sql também vai
-- rodá-la de verdade na primeira subida da Api (MigrationRunner vê que
-- '13_migracao_multi_barbearia.sql' não está em migracoes_aplicadas) —
-- o que é seguro e correto, só um pouco mais lento que pular. QUALQUER
-- migração 13+ que vier depois desta deve seguir a mesma regra: só
-- volte a "assar" migrações neste 01_schema.sql (e marcá-las na lista
-- acima) se/quando este projeto ganhar um jeito de compilar e testar de
-- verdade antes de mexer nisso.
-- =====================================================================
