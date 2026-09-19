namespace Barbearia.Application.Dtos.Ranking;

public sealed record ItemPremioRequest(int Posicao, string Descricao);

/// <summary>
/// Substitui de uma vez TODOS os prêmios do mês atual pela lista enviada
/// — sem "adicionar um prêmio de cada vez": o Admin/Barbeiro reconfigura
/// o pódio inteiro sempre que quiser, e uma posição que não vier mais
/// nesta lista perde o prêmio que tinha (ver RankingService.FnDefinirPremiosAsync).
/// </summary>
public sealed record DefinirPremiosRequest(List<ItemPremioRequest> Premios);

/// <summary>Um cliente no pódio do mês — "cortes" aqui conta agendamentos concluídos DESTE cliente, não de um barbeiro.</summary>
public sealed record PosicaoRankingResponse(
    int Posicao,
    long ClienteId,
    string NomeCliente,
    string? FotoUrl,
    int TotalCortes,
    string? Premio);

public sealed record RankingMensalResponse(int Mes, int Ano, bool MesAtual, List<PosicaoRankingResponse> Posicoes);

public sealed record CampeaoHistoricoResponse(
    int Mes,
    int Ano,
    long ClienteId,
    string NomeCliente,
    string? FotoUrl,
    int TotalCortes,
    string? Premio);
