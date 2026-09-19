using Barbearia.Domain.Enumeracoes;

namespace Barbearia.Application.Dtos.Planos;

public sealed record CriarPlanoRequest(string Nome, string? Descricao, decimal PrecoMensal);

public sealed record IncluirServicoNoPlanoRequest(long ServicoId, short LimiteMensal);

// NomeServico/StatusServico vêm do serviço de verdade (join), não são
// digitados de novo aqui — é o que evita a tela mostrar "Serviço #1"
// quando o serviço vinculado está Inativo (o Comum já não vê esse
// serviço na lista de catálogo, mas o plano continua sabendo o nome).
public sealed record ServicoIncluidoResponse(long ServicoId, string NomeServico, StatusRegistro StatusServico, short LimiteMensal);

public sealed record PlanoResponse(
    long Id,
    string Nome,
    string? Descricao,
    decimal PrecoMensal,
    StatusRegistro Status,
    IReadOnlyCollection<ServicoIncluidoResponse> ServicosInclusos);
