namespace Barbearia.Domain.Enumeracoes;

/// <summary>Espelha o CHECK de pagamentos.forma no 01_schema.sql.</summary>
public enum FormaPagamento
{
    Dinheiro = 0,
    Pix = 1,
    CartaoCredito = 2,
    CartaoDebito = 3,
    Assinatura = 4
}
