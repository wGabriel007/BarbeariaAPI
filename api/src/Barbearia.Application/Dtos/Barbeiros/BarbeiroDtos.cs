using Barbearia.Domain.Enumeracoes;

namespace Barbearia.Application.Dtos.Barbeiros;

/// <summary>
/// UsuarioId precisa ser de um Usuario já existente do tipo Comum — o
/// Service promove esse usuário a Barbeiro sozinho (muda o Tipo dele e
/// desfaz o vínculo de Cliente, se houver um) em vez de exigir que o
/// Admin já tenha preparado isso na mão (ver BarbeiroService.FnPromoverAsync).
/// </summary>
public sealed record PromoverBarbeiroRequest(long UsuarioId, string? Telefone);

public sealed record AdicionarHorarioRequest(DiaSemana DiaSemana, TimeOnly HoraInicio, TimeOnly HoraFim);

public sealed record DefinirAusenciaRequest(bool Ausente);

/// <summary>
/// Apresentação profissional self-service (ver Barbeiro.FnAtualizarPerfil,
/// aba "Sobre a barbearia") — Bio/Especialidade são livres, sem
/// obrigatoriedade nenhuma.
/// </summary>
public sealed record AtualizarPerfilBarbeiroRequest(string? Bio, string? Especialidade);

public sealed record HorarioResponse(long Id, DiaSemana DiaSemana, TimeOnly HoraInicio, TimeOnly HoraFim);

public sealed record BarbeiroResponse(
    long Id,
    long UsuarioId,
    string NomeCompleto,
    string Email,
    string? Telefone,
    string? FotoUrl,
    StatusRegistro Status,
    bool Ausente,
    string? Bio,
    string? Especialidade,
    IReadOnlyCollection<HorarioResponse> Horarios);
