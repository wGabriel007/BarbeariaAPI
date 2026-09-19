namespace Barbearia.Domain.Enumeracoes;

/// <summary>Espelha o CHECK de assinaturas.status no 01_schema.sql.</summary>
public enum StatusAssinatura
{
    Ativa = 0,
    Suspensa = 1,
    Cancelada = 2,
    Expirada = 3
}
