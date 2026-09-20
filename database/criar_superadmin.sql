-- =====================================================================
-- Cria a PRIMEIRA conta de SuperAdmin (dono da plataforma) — você.
--
-- Diferente de tudo mais no sistema, isto NÃO roda sozinho quando a Api
-- sobe (não tem "_migracao_" no nome, de propósito — ver o padrão
-- "*_migracao_*.sql" em Barbearia.Infrastructure.csproj): é rodado
-- UMA VEZ, na mão, por você, direto no banco (ver instruções no arquivo
-- GUIA_MULTI_BARBEARIA.md, seção "Criar sua conta de SuperAdmin").
--
-- Não existe outro jeito de criar essa conta (nem tela de cadastro, nem
-- endpoint) de propósito: SuperAdmin é o dono do sistema inteiro, então
-- só quem já tem acesso direto ao banco (você) pode criar um.
--
-- Login: gabrielmoreira9699@gmail.com
-- Senha inicial: BarbeariaAdmin2026!
--   (troque essa senha assim que entrar pela primeira vez, em
--   "/admin" -> menu do seu nome -> ainda não existe tela própria pra
--   isso no painel do SuperAdmin nesta versão, mas o endpoint por trás
--   já existe — peça pra eu adicionar essa tela se quiser trocar por
--   ali; por enquanto, se quiser trocar agora, me avise que eu gero um
--   novo hash e um novo UPDATE pra você rodar aqui, do mesmo jeito.)
--
-- Seguro rodar mais de uma vez: o ON CONFLICT abaixo evita duplicar se
-- você já tiver rodado isto antes. O "WHERE empresa_id IS NULL" no final
-- não é filtro de dado nenhum — é só apontando pro índice único certo
-- (ux_usuarios_email_superadmin, ver 13_migracao_multi_barbearia.sql),
-- que só existe pra linhas de SuperAdmin (email único SÓ entre
-- SuperAdmins, já que dentro de cada barbearia o email é único à parte).
-- =====================================================================

INSERT INTO usuarios (nome_completo, email, senha_hash, tipo, status)
VALUES (
  'Gabriel',
  'gabrielmoreira9699@gmail.com',
  '$2b$11$VEkNJuzgi3PMjdjKYQAPOeSS93.LJuO2xghhBUHhCmBf9nZle5GNe',
  3, -- TipoUsuario.SuperAdmin (ver Barbearia.Domain.Enumeracoes.TipoUsuario)
  1  -- StatusRegistro.Ativo
)
ON CONFLICT (email) WHERE empresa_id IS NULL DO NOTHING;
