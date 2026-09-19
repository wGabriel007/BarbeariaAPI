using Barbearia.Domain.Enumeracoes;

namespace Barbearia.Application.Dtos.Pagamentos;

public sealed record PagamentoResponse(
    long Id,
    long? AgendamentoId,
    long? AssinaturaId,
    long ClienteId,
    decimal Valor,
    FormaPagamento Forma,
    StatusPagamento Status,
    DateTimeOffset? PagoEm,
    DateTimeOffset CriadoEm);

/// <summary>Forma escolhida pelo barbeiro no momento em que confirma o recebimento.</summary>
public sealed record ConfirmarPagamentoRequest(FormaPagamento Forma);
