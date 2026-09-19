-- =====================================================================
-- Migração pontual: fluxo de SOLICITAÇÃO de agendamento pelo próprio
-- cliente (Comum), com confirmação/rejeição do barbeiro.
--
-- Só é necessária se você já criou seu banco a partir do 01_schema.sql
-- ANTES desta mudança. Se você está criando o banco pela primeira vez,
-- ignore este arquivo — o 01_schema.sql já vem com tudo isso incluído.
--
-- O que muda:
--   1. clientes.telefone deixa de ser obrigatório — um Cliente agora
--      também pode nascer sozinho (auto-provisionado), na hora em que
--      um usuário Comum pede seu primeiro agendamento, e nesse caso
--      ainda não temos telefone nenhum, só o que já está na conta dele.
--   2. clientes ganha usuario_id (opcional, único) — liga o Cliente à
--      conta de login (usuarios) de quem o "é", quando existir uma.
--   3. agendamentos ganha dois status novos: 6=Pendente (solicitação
--      aguardando o barbeiro confirmar/rejeitar) e 7=Rejeitado. O
--      EXCLUDE constraint que impede conflito de horário passa a
--      considerar que Rejeitado também libera o horário (Pendente
--      continua RESERVANDO, até o barbeiro decidir).
--   4. agendamentos ganha mensagem_resposta — recado opcional do
--      Admin/Barbeiro pro cliente ao confirmar/rejeitar/cancelar/etc.
--
-- Você NÃO precisa mais rodar isto manualmente: a própria Api aplica
-- automaticamente qualquer migração pendente (arquivos "NN_migracao_*.sql"
-- desta pasta) assim que sobe, e lembra o que já aplicou numa tabela de
-- controle (migracoes_aplicadas) — ver Barbearia.Infrastructure/
-- Persistence/MigrationRunner.cs. Todo statement abaixo é escrito pra
-- ser seguro de rodar mais de uma vez (idempotente), justamente por
-- causa disso. Rodar manualmente (psql -U postgres -d barbearia -f
-- 04_migracao_solicitacao_agendamento.sql) ainda funciona, se preferir.

-- 1. Telefone do cliente deixa de ser obrigatório.
ALTER TABLE clientes ALTER COLUMN telefone DROP NOT NULL;

-- 2. Vínculo opcional cliente -> usuario (não confundir com barbeiros.usuario_id,
--    que é obrigatório — aqui a MAIORIA dos clientes continua sem nenhum usuario_id).
ALTER TABLE clientes ADD COLUMN IF NOT EXISTS usuario_id BIGINT REFERENCES usuarios(id) ON DELETE SET NULL;

-- Postgres não tem "ADD CONSTRAINT IF NOT EXISTS" (só existe pra DROP),
-- então o jeito idempotente de adicionar uma constraint é checar antes
-- num bloco DO.
DO $$
BEGIN
  IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'uq_clientes_usuario') THEN
    ALTER TABLE clientes ADD CONSTRAINT uq_clientes_usuario UNIQUE (usuario_id);
  END IF;
END $$;

-- 3a. Novos valores aceitos em agendamentos.status (mesmo nome de
--     constraint que o Postgres dá sozinho a um CHECK inline sem nome
--     explícito — confira com \d agendamentos se você já renomeou).
ALTER TABLE agendamentos DROP CONSTRAINT IF EXISTS agendamentos_status_check;
ALTER TABLE agendamentos ADD CONSTRAINT agendamentos_status_check CHECK (status IN (0, 1, 2, 3, 4, 5, 6, 7));

-- 3b. Troca o EXCLUDE constraint pra incluir Rejeitado(7) na lista de
--     status que liberam o horário. Não existe "ALTER CONSTRAINT" pra
--     isso no Postgres — precisa dropar e recriar (drop-then-add já é
--     idempotente por natureza: não importa se já existia ou não).
ALTER TABLE agendamentos DROP CONSTRAINT IF EXISTS sem_conflito_horario;
ALTER TABLE agendamentos ADD CONSTRAINT sem_conflito_horario EXCLUDE USING gist (
  barbeiro_id WITH =,
  tstzrange(inicio, fim) WITH &&
) WHERE (status NOT IN (4, 5, 7));

-- 4. Recado opcional do Admin/Barbeiro pro cliente.
ALTER TABLE agendamentos ADD COLUMN IF NOT EXISTS mensagem_resposta TEXT;
