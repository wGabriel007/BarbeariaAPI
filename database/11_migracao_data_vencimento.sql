-- =====================================================================
-- Migração pontual: vencimento de assinatura vira uma DATA exata, não um
-- "dia do mês" — ver Barbearia.Domain.Entities.Assinatura.DataVencimento.
--
-- Só é necessária se você já criou seu banco a partir do 01_schema.sql
-- ANTES desta mudança. Se você está criando o banco pela primeira vez,
-- ignore este arquivo — o 01_schema.sql já vem com tudo isso incluído.
--
-- Antes: dia_vencimento (SMALLINT 1-28) — um "dia do mês" que o
-- Admin/Barbeiro tinha que traduzir de cabeça pra saber a data real
-- ("dia 5, então cai quando exatamente?"). Agora: data_vencimento (DATE)
-- — a data certa, escolhida direto na hora de criar a assinatura ou
-- aceitar uma solicitação de plano (ver AssinaturaService/
-- SolicitacaoPlanoService). Não existe "avançar sozinho pro próximo mês"
-- — uma renovação sempre nasce como uma nova Assinatura com a próxima
-- data escolhida por quem atende.
--
-- Escrita pra ser segura de rodar MAIS DE UMA VEZ e em QUALQUER estado
-- intermediário (ver comentário grande em MigrationRunner.cs sobre
-- idempotência) — inclusive se alguém já rodou uma versão anterior desta
-- migração na mão (fora da Api) e ela parou no meio: cada passo confere
-- o que já existe antes de agir, então rodar de novo do zero nunca
-- quebra nem duplica nada.
--
-- Você não precisa rodar isto manualmente: a própria Api aplica
-- automaticamente qualquer migração pendente assim que sobe — ver
-- Barbearia.Infrastructure/Persistence/MigrationRunner.cs.

ALTER TABLE assinaturas ADD COLUMN IF NOT EXISTS data_vencimento DATE;

-- Backfill: só roda se a coluna antiga (dia_vencimento) ainda existir —
-- se ela já foi removida (migração já rodou antes, nem que tenha sido
-- manualmente e pela metade), não tem como nem faz sentido ler o valor
-- antigo de novo.
DO $$
BEGIN
  IF EXISTS (
    SELECT 1 FROM information_schema.columns
    WHERE table_name = 'assinaturas' AND column_name = 'dia_vencimento'
  ) THEN
    -- Tenta o dia antigo dentro do mesmo mês de data_inicio — é a data
    -- mais próxima do que já estava configurado, pra ninguém perder o
    -- vencimento já combinado com o cliente. Mas se esse dia já tiver
    -- passado dentro do próprio mês de início (ex.: data_inicio = 20/06,
    -- dia_vencimento = 5 → cairia em 05/06, ANTES da assinatura nem
    -- existir), empurra pro mesmo dia no mês seguinte.
    UPDATE assinaturas
    SET data_vencimento = (
      CASE
        WHEN (date_trunc('month', data_inicio) + ((dia_vencimento - 1) || ' days')::interval)::date >= data_inicio
          THEN (date_trunc('month', data_inicio) + ((dia_vencimento - 1) || ' days')::interval)::date
        ELSE (date_trunc('month', data_inicio) + interval '1 month' + ((dia_vencimento - 1) || ' days')::interval)::date
      END
    )::date
    WHERE data_vencimento IS NULL;
  END IF;
END $$;

-- Rede de segurança: conserta qualquer linha que ainda tenha sobrado com
-- vencimento anterior ao início — seja de um backfill anterior com o
-- bug antigo (que já rodou em algum banco antes desta correção), seja
-- de qualquer outra causa. Sem isso a constraint logo abaixo rejeitaria
-- a migração de novo.
UPDATE assinaturas
SET data_vencimento = (data_vencimento + interval '1 month')::date
WHERE data_vencimento < data_inicio;

ALTER TABLE assinaturas ALTER COLUMN data_vencimento SET NOT NULL;

-- ADD CONSTRAINT não tem "IF NOT EXISTS" no Postgres — confere na mão
-- pra não falhar tentando criar de novo uma constraint que já existe.
DO $$
BEGIN
  IF NOT EXISTS (
    SELECT 1 FROM pg_constraint WHERE conname = 'ck_assinaturas_vencimento'
  ) THEN
    ALTER TABLE assinaturas ADD CONSTRAINT ck_assinaturas_vencimento CHECK (data_vencimento >= data_inicio);
  END IF;
END $$;

ALTER TABLE assinaturas DROP COLUMN IF EXISTS dia_vencimento;
