-- =====================================================================
-- Dados de exemplo para desenvolvimento/aprendizado.
-- NÃO rodar em produção — isso aqui é só pra você ter algo pra testar
-- a API e o front sem precisar cadastrar tudo na mão primeiro.
-- =====================================================================

-- Conta de acesso do dono (admin) e de um barbeiro
-- senha_hash aqui é só um valor de exemplo — a API vai gerar o hash
-- de verdade com BCrypt quando você implementar o cadastro/login.
-- tipo: 0=Admin, 1=Barbeiro, 2=Comum | status: 1=Ativo (omitido, é o DEFAULT)
INSERT INTO usuarios (nome_completo, email, senha_hash, tipo) VALUES
  ('Victor (Dono)', 'victor@barbearia.com', 'HASH_TEMPORARIO_1', 0),
  ('João Barbeiro', 'joao@barbearia.com', 'HASH_TEMPORARIO_2', 1);

INSERT INTO barbeiros (usuario_id, telefone)
SELECT id, '(11) 90000-0001'
FROM usuarios WHERE email = 'joao@barbearia.com';

-- Agenda semanal do João: terça a sábado, 09h-18h
INSERT INTO horarios_trabalho (barbeiro_id, dia_semana, hora_inicio, hora_fim)
SELECT b.id, dia, '09:00', '18:00'
FROM barbeiros b
CROSS JOIN unnest(ARRAY[2,3,4,5,6]) AS dia -- terça(2) a sábado(6)
WHERE b.telefone = '(11) 90000-0001';

-- Serviços
INSERT INTO servicos (nome, descricao, duracao_minutos, preco) VALUES
  ('Corte de cabelo', 'Corte tesoura/máquina', 30, 45.00),
  ('Barba', 'Barba completa com toalha quente', 25, 35.00),
  ('Corte + Barba', 'Combo corte e barba', 50, 70.00),
  ('Sobrancelha', 'Design de sobrancelha na navalha', 15, 20.00);

-- Plano de assinatura mensal
INSERT INTO planos_assinatura (nome, descricao, preco_mensal) VALUES
  ('Plano Básico', '2 cortes por mês', 79.90),
  ('Plano Premium', '4 cortes + 2 barbas por mês', 149.90);

INSERT INTO plano_servicos (plano_id, servico_id, limite_mensal)
SELECT p.id, s.id, 2
FROM planos_assinatura p, servicos s
WHERE p.nome = 'Plano Básico' AND s.nome = 'Corte de cabelo';

INSERT INTO plano_servicos (plano_id, servico_id, limite_mensal)
SELECT p.id, s.id, 4
FROM planos_assinatura p, servicos s
WHERE p.nome = 'Plano Premium' AND s.nome = 'Corte de cabelo';

INSERT INTO plano_servicos (plano_id, servico_id, limite_mensal)
SELECT p.id, s.id, 2
FROM planos_assinatura p, servicos s
WHERE p.nome = 'Plano Premium' AND s.nome = 'Barba';

-- Cliente de exemplo, já assinante do Plano Básico
INSERT INTO clientes (nome_completo, telefone, email) VALUES
  ('Carlos Souza', '(11) 91234-5678', 'carlos@example.com');

-- status: 0=Ativa. Vencimento de exemplo: um mês a partir de hoje (era só
-- "dia 5" antes de data_vencimento virar uma data exata — ver migração 11).
INSERT INTO assinaturas (cliente_id, plano_id, data_inicio, data_vencimento, status)
SELECT c.id, p.id, CURRENT_DATE, CURRENT_DATE + INTERVAL '1 month', 0
FROM clientes c, planos_assinatura p
WHERE c.telefone = '(11) 91234-5678' AND p.nome = 'Plano Básico';
