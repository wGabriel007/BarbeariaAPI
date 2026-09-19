namespace Barbearia.Domain.Enumeracoes;

/// <summary>Espelha o CHECK de pagamentos.status no 01_schema.sql.</summary>
public enum StatusPagamento
{
    Pendente = 0,
    Pago = 1,
    Cancelado = 2,
    Reembolsado = 3
}
