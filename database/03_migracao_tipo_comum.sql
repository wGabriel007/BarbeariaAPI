-- =====================================================================
-- Migração pontual: adiciona o valor 2=Comum em usuarios.tipo.
--
-- Só é necessária se você já criou seu banco a partir do 01_schema.sql
-- ANTES desta mudança (ou seja, seu banco já existe e você só quer
-- atualizar a estrutura, sem recriar tudo do zero). Se você está
-- criando o banco pela primeira vez, ignore este arquivo — o
-- 01_schema.sql já vem com o "2=Comum" incluído desde o início.
--
-- O que mudou: antes, usuarios.tipo só aceitava 0 (Admin) ou 1
-- (Barbeiro), e todo autocadastro pela tela /cadastro criava uma conta
-- Admin. Agora existe um terceiro valor, 2=Comum, que é o padrão de
-- quem se autocadastra (só a primeira conta do sistema continua virando
-- Admin automaticamente).
--
-- Rode isto uma vez, conectado no banco "barbearia":
--   psql -U postgres -d barbearia -f 03_migracao_tipo_comum.sql
-- =====================================================================

-- O nome "usuarios_tipo_check" é o nome que o Postgres dá sozinho pra um
-- CHECK inline sem nome explícito (padrão "<tabela>_<coluna>_check").
-- Se você já tiver renomeado essa constraint manualmente, ajuste o nome
-- abaixo antes de rodar (confira com: \d usuarios).
ALTER TABLE usuarios DROP CONSTRAINT IF EXISTS usuarios_tipo_check;
ALTER TABLE usuarios ADD CONSTRAINT usuarios_tipo_check CHECK (tipo IN (0, 1, 2));

-- Não muda o DEFAULT nem nenhuma linha existente — isso aqui só amplia o
-- que a coluna aceita. Quem já está cadastrado continua exatamente como
-- estava.
