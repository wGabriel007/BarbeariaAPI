-- =====================================================================
-- Migração pontual: fluxo de SOLICITAÇÃO de plano pelo próprio usuário
-- Comum, com aceite/rejeição do Admin/Barbeiro (mesma ideia do
-- 04_migracao_solicitacao_agendamento.sql, só que pra plano em vez de
-- horário).
--
-- Só é necessária se você já criou seu banco a partir do 01_schema.sql
-- ANTES desta mudança. Se você está criando o banco pela primeira vez,
-- ignore este arquivo — o 01_schema.sql já vem com tudo isso incluído.
--
-- O que muda: nova tabela solicitacoes_plano — cada linha é um pedido
-- de um usuário Comum pra assinar um plano, com o e-mail/telefone que
-- ele digitou na hora (não os que já estavam na conta — vira o contato
-- do Cliente auto-provisionado se aceito, ver
-- SolicitacaoPlanoService.AceitarAsync) e status Pendente/Aceita/Rejeitada.
--
-- Você não precisa rodar isto manualmente: a própria Api aplica
-- automaticamente qualquer migração pendente assim que sobe — ver
-- Barbearia.Infrastructure/Persistence/MigrationRunner.cs.

CREATE TABLE IF NOT EXISTS solicitacoes_plano (
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

CREATE INDEX IF NOT EXISTS ix_solicitacoes_plano_usuario ON solicitacoes_plano (usuario_id);

DO $$
BEGIN
  IF NOT EXISTS (SELECT 1 FROM pg_trigger WHERE tgname = 'trg_solicitacoes_plano_atualizado') THEN
    CREATE TRIGGER trg_solicitacoes_plano_atualizado
      BEFORE UPDATE ON solicitacoes_plano
      FOR EACH ROW EXECUTE FUNCTION atualizar_timestamp();
  END IF;
END $$;
