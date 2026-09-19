-- =====================================================================
-- Migração pontual: Ranking de Cortes — quem mais concluiu atendimentos
-- no mês, com prêmios configuráveis pelo Admin/Barbeiro (ver
-- Barbearia.Application.Services.RankingService / RankingController).
--
-- Só é necessária se você já criou seu banco a partir do 01_schema.sql
-- ANTES desta mudança. Se você está criando o banco pela primeira vez,
-- ignore este arquivo — o 01_schema.sql já vem com tudo isso incluído.
--
-- NÃO existe uma tabela "ranking" — a contagem de cortes é sempre
-- calculada na hora, direto de agendamentos.status = Concluido (ver
-- RankingService), pra qualquer mês, passado ou presente. Só o PRÊMIO de
-- cada posição do pódio precisa ficar salvo, porque é reconfigurado do
-- zero todo mês pelo Admin/Barbeiro (não existe cópia automática do mês
-- anterior — é assim que "reinicia todo mês" fica garantido).
--
-- Você não precisa rodar isto manualmente: a própria Api aplica
-- automaticamente qualquer migração pendente assim que sobe — ver
-- Barbearia.Infrastructure/Persistence/MigrationRunner.cs.

CREATE TABLE IF NOT EXISTS premios_ranking (
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
