-- =====================================================================
-- Migração pontual: configuração visual do site (nome exibido, logo,
-- cor de destaque) — customizável só pelo Admin (ver
-- ConfiguracaoSiteController), lida por qualquer um (inclusive
-- deslogado, na tela de Login) pra já mostrar a marca certa da
-- barbearia antes mesmo de entrar.
--
-- Só é necessária se você já criou seu banco a partir do 01_schema.sql
-- ANTES desta mudança. Se você está criando o banco pela primeira vez,
-- ignore este arquivo — o 01_schema.sql já vem com tudo isso incluído.
--
-- UMA LINHA SÓ (id sempre 1, ver Barbearia.Domain.Entities.ConfiguracaoSite.IdUnico)
-- — por isso o INSERT abaixo com ON CONFLICT DO NOTHING em vez de um
-- CREATE TABLE + seed em arquivos separados: garante que a linha sempre
-- exista, mesmo rodando esta migração mais de uma vez.
--
-- Você não precisa rodar isto manualmente: a própria Api aplica
-- automaticamente qualquer migração pendente assim que sobe — ver
-- Barbearia.Infrastructure/Persistence/MigrationRunner.cs.

CREATE TABLE IF NOT EXISTS configuracoes_site (
  id              BIGINT PRIMARY KEY,
  nome_barbearia  VARCHAR(100) NOT NULL DEFAULT 'Barbearia',
  logo_url        VARCHAR(500),
  cor_primaria    VARCHAR(7) -- "#334562" — null usa a paleta padrão do sistema
);

INSERT INTO configuracoes_site (id, nome_barbearia) VALUES (1, 'Barbearia')
ON CONFLICT (id) DO NOTHING;
