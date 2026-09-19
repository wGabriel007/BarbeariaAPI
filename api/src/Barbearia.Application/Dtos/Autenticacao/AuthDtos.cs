using Barbearia.Application.Dtos.Usuarios;

namespace Barbearia.Application.Dtos.Autenticacao;

public sealed record LoginRequest(string Email, string Senha);

// Sem campo "Tipo" de propósito: quem se autocadastra por aqui NUNCA
// escolhe o próprio tipo (senão qualquer um vira "Admin" só editando o
// JSON da requisição). O tipo é decidido pelo servidor — ver
// AutenticacaoService.FnRegistrarAsync. FnCriar um barbeiro ou outro admin
// continua possível depois, por quem já estiver logado, em
// POST /api/usuarios (que aí sim aceita o tipo, mas exige token).
//
// Telefone é obrigatório (não-nullable, igual o padrão já usado em
// AtualizarClienteRequest) — a barbearia precisa de um jeito de contato
// direto com quem se cadastra, além do e-mail (ver validação em
// AutenticacaoService.FnRegistrarAsync).
public sealed record RegistrarRequest(string NomeCompleto, string Email, string Senha, string Telefone);

/// <summary>
/// Devolvida tanto por /login quanto por /registrar — depois de criar a
/// conta, a pessoa já sai logada, sem precisar dar login de novo em
/// seguida (melhor experiência: um passo em vez de dois).
/// </summary>
public sealed record AuthResponse(string Token, UsuarioResponse Usuario);
