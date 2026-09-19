namespace Barbearia.Application.Dtos.ConfiguracoesSite;

public sealed record ConfiguracaoSiteResponse(
    string NomeBarbearia,
    string? LogoUrl,
    string? CorPrimaria,
    string? Descricao,
    string? Endereco,
    string? Telefone,
    string? Instagram,
    string? HorarioFuncionamento,
    List<FotoBarbeariaResponse> Fotos);

public sealed record AtualizarConfiguracaoSiteRequest(string NomeBarbearia, string? CorPrimaria);

/// <summary>
/// Informações públicas do negócio (ver
/// ConfiguracaoSite.FnAtualizarInformacoes) — diferente de
/// AtualizarConfiguracaoSiteRequest (nome/cor, decisão de dono do
/// negócio, só Admin), esta aqui é operação do dia a dia: Admin OU
/// Barbeiro (ver ConfiguracaoSiteController).
/// </summary>
public sealed record AtualizarInformacoesBarbeariaRequest(
    string? Descricao,
    string? Endereco,
    string? Telefone,
    string? Instagram,
    string? HorarioFuncionamento);

public sealed record FotoBarbeariaResponse(long Id, string Url);
