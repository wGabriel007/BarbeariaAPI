using Barbearia.Domain.Enumeracoes;

namespace Barbearia.Application.Dtos.Usuarios;

/// <summary>
/// Senha em texto puro chega até aqui (é a última camada que ainda a
/// vê) — o Service transforma em hash ANTES de chegar no Domain
/// (Usuario.FnCriar só aceita SenhaHash). Nunca logue este DTO.
/// </summary>
public sealed record CriarUsuarioRequest(string NomeCompleto, string Email, string Senha, TipoUsuario Tipo);

/// <summary>
/// EhCliente NÃO é uma permissão nem muda o Tipo da conta (ver
/// ClienteService.FnPromoverAsync — vincular um Usuario Comum a um Cliente
/// nunca troca o Tipo dele) — é só informação extra pra tela de Usuários
/// trocar o RÓTULO exibido de "Comum" pra "Cliente" quando for o caso,
/// deixando claro que essa conta já tem um cadastro de cliente por trás
/// (ver Usuarios.jsx/rotuloTipoUsuario no front). Continua sendo, pra
/// todo efeito de login/autorização, um usuário Comum normal.
/// </summary>
public sealed record UsuarioResponse(
    long Id,
    string NomeCompleto,
    string Email,
    string? Telefone,
    string? FotoUrl,
    TipoUsuario Tipo,
    StatusRegistro Status,
    bool EhCliente);

/// <summary>Aba "Meu perfil" — mesmos dados de UsuarioResponse (incluindo EhCliente, ver o comentário lá), mas com um nome próprio porque é o formato da rota de self-service (ver PerfilController), independente de qualquer mudança futura no formato usado pela gestão de contas (UsuariosController).</summary>
public sealed record PerfilResponse(
    long Id,
    string NomeCompleto,
    string Email,
    string? Telefone,
    string? FotoUrl,
    TipoUsuario Tipo,
    StatusRegistro Status,
    bool EhCliente);

// Telefone não-nullable igual RegistrarRequest — obrigatório também aqui
// (não só no autocadastro), senão uma conta antiga sem telefone (criada
// antes desta mudança, ou pelo Admin em Usuários, que não pede telefone)
// nunca seria forçada a completar esse dado (ver validação em
// UsuarioService.FnAtualizarPerfilAsync).
public sealed record AtualizarPerfilRequest(string NomeCompleto, string Email, string Telefone);

public sealed record AlterarSenhaRequest(string SenhaAtual, string NovaSenha);
