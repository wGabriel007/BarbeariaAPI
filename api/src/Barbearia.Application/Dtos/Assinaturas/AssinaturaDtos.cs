using Barbearia.Domain.Enumeracoes;

namespace Barbearia.Application.Dtos.Assinaturas;

public sealed record CriarAssinaturaRequest(long ClienteId, long PlanoId, DateOnly DataInicio, DateOnly DataVencimento);

/// <summary>
/// AtualizadoEm (mesma ideia de AgendamentoResponse/SolicitacaoPlanoResponse)
/// — deixa o front notar quando o STAFF muda o status de uma assinatura
/// (FnSuspender/FnReativar/FnCancelar) sem o cliente precisar ficar
/// recarregando "Meu plano" pra descobrir.
/// </summary>
public sealed record AssinaturaResponse(
    long Id,
    long ClienteId,
    long PlanoId,
    DateOnly DataInicio,
    DateOnly? DataFim,
    DateOnly DataVencimento,
    StatusAssinatura Status,
    DateTimeOffset AtualizadoEm);
