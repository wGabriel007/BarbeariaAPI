using Barbearia.Domain.Enumeracoes;

namespace Barbearia.Application.Dtos.Clientes;

// 'record' em vez de 'class' pra DTO é a escolha certa aqui: DTO é só
// um pacote de dados que entra/sai pela API, sem comportamento — record
// já dá igualdade por valor e imutabilidade de graça, sem boilerplate.
//
// O tipo do campo Status é o enum StatusRegistro mesmo (não string,
// não int): no JSON de resposta ele vai aparecer como texto ("Ativo"),
// porque em Barbearia.Api/Program.cs configuramos um
// JsonStringEnumConverter global — assim NENHUM DTO precisa converter
// manualmente enum -> string, e o cliente da API nunca vê o número
// interno (0/1/2), só o nome.

/// <summary>
/// UsuarioId precisa ser de um Usuario Comum já existente — não existe
/// mais "criar um cliente do zero" digitando nome/telefone: o Admin
/// escolhe um usuário Comum já cadastrado e o Service vincula esse
/// Cliente à conta dele (ver ClienteService.FnPromoverAsync). Nome/e-mail
/// vêm do próprio Usuario; telefone e demais dados dá pra completar
/// depois via AtualizarClienteRequest.
/// </summary>
public sealed record PromoverClienteRequest(long UsuarioId);

public sealed record AtualizarClienteRequest(
    string NomeCompleto,
    string Telefone,
    string? Email,
    string? Observacoes);

public sealed record ClienteResponse(
    long Id,
    string NomeCompleto,
    // Nullable porque um Cliente sempre nasce (seja por auto-provisionamento
    // no primeiro agendamento pedido pelo app, seja pela promoção manual
    // do Admin — ver PromoverClienteRequest) a partir de uma conta Comum,
    // sem telefone nenhum ainda; quem quiser completar isso usa AtualizarClienteRequest.
    string? Telefone,
    string? Email,
    string? Cpf,
    DateOnly? DataNascimento,
    string? Observacoes,
    StatusRegistro Status,
    DateTimeOffset CriadoEm,
    // Nullable por causa de clientes antigos, cadastrados manualmente
    // antes desta mudança (sem login nenhum por trás) — todo Cliente
    // criado a partir de agora sempre nasce com um. Exposto aqui pra o
    // front saber, ex.: se esse Cliente já tem uma conta (só quem tem
    // dá pra promover a Barbeiro).
    long? UsuarioId);

/// <summary>
/// "Cartão do cliente" aberto a partir da aba Usuários (ver
/// UsuariosController) — não é pensado pra listar (custaria varrer o
/// histórico de agendamentos de todo mundo), só pra abrir UM cliente por
/// vez com dado cadastral + o que o Admin/Barbeiro quer saber na hora de
/// atender: quantos cortes essa pessoa já fez, quanto ela já gastou aqui
/// e quando foi a última vez. Tudo calculado na hora a partir de
/// Agendamento (nada disso fica salvo em coluna própria).
/// </summary>
public sealed record ClienteDetalheResponse(
    ClienteResponse Dados,
    int TotalCortes,
    int CortesEsteMes,
    decimal ValorTotalGasto,
    DateTimeOffset? UltimoCorte);
