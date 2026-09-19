-- =====================================================================
-- Migração pontual: aba "Sobre a barbearia" — informações públicas do
-- negócio (descrição, endereço, telefone, Instagram, horário de
-- funcionamento) + uma galeria de fotos, e um perfil profissional
-- (bio/especialidade) pra cada barbeiro apresentar o próprio trabalho.
--
-- Diferente de nome/logo/cor (09_migracao_configuracao_site.sql, só
-- Admin), estas novas colunas/tabela são configuráveis por Admin OU
-- Barbeiro — é operação do dia a dia, não decisão de dono do negócio
-- (ver ConfiguracaoSiteController).
--
-- Só é necessária se você já criou seu banco a partir do 01_schema.sql
-- ANTES desta mudança. Se você está criando o banco pela primeira vez,
-- ignore este arquivo — o 01_schema.sql já vem com tudo isso incluído.
--
-- Você não precisa rodar isto manualmente: a própria Api aplica
-- automaticamente qualquer migração pendente assim que sobe — ver
-- Barbearia.Infrastructure/Persistencia/MigrationRunner.cs.

ALTER TABLE configuracoes_site
  ADD COLUMN IF NOT EXISTS descricao              TEXT,
  ADD COLUMN IF NOT EXISTS endereco                VARCHAR(300),
  ADD COLUMN IF NOT EXISTS telefone                VARCHAR(20),
  ADD COLUMN IF NOT EXISTS instagram               VARCHAR(100),
  ADD COLUMN IF NOT EXISTS horario_funcionamento   VARCHAR(200);

CREATE TABLE IF NOT EXISTS fotos_barbearia (
  id                     BIGINT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
  configuracao_site_id   BIGINT NOT NULL REFERENCES configuracoes_site(id) ON DELETE CASCADE,
  url                    VARCHAR(500) NOT NULL
);

CREATE INDEX IF NOT EXISTS ix_fotos_barbearia_configuracao_site_id ON fotos_barbearia (configuracao_site_id);

ALTER TABLE barbeiros
  ADD COLUMN IF NOT EXISTS bio             TEXT,
  ADD COLUMN IF NOT EXISTS especialidade   VARCHAR(150);
