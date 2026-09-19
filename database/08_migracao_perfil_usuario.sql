-- =====================================================================
-- Migração pontual: aba "Meu perfil" — cada usuário (Admin, Barbeiro ou
-- Comum) passa a poder guardar um telefone de contato e uma foto,
-- vendo/editando os próprios dados por conta própria (ver
-- PerfilController — self-service, sem depender de um Admin).
--
-- Só é necessária se você já criou seu banco a partir do 01_schema.sql
-- ANTES desta mudança. Se você está criando o banco pela primeira vez,
-- ignore este arquivo — o 01_schema.sql já vem com tudo isso incluído.
--
-- Você não precisa rodar isto manualmente: a própria Api aplica
-- automaticamente qualquer migração pendente assim que sobe — ver
-- Barbearia.Infrastructure/Persistence/MigrationRunner.cs.

ALTER TABLE usuarios ADD COLUMN IF NOT EXISTS telefone VARCHAR(20);

-- Guarda a URL RELATIVA do arquivo (ex.: "/uploads/fotos-usuarios/xxx.jpg"),
-- servido como arquivo estático pela própria Api — ver
-- Barbearia.Infrastructure/Storage/ArmazenamentoArquivosLocal e
-- app.UseStaticFiles() em Program.cs. Nunca a imagem em si dentro do banco.
ALTER TABLE usuarios ADD COLUMN IF NOT EXISTS foto_url VARCHAR(500);
