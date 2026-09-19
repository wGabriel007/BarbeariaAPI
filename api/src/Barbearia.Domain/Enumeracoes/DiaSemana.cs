namespace Barbearia.Domain.Enumeracoes;

/// <summary>
/// Espelha o CHECK (dia_semana BETWEEN 0 AND 6) de horarios_trabalho no
/// 01_schema.sql — mesma convenção do Postgres (extract(dow ...)):
/// 0 = domingo ... 6 = sábado.
/// </summary>
public enum DiaSemana
{
    Domingo = 0,
    Segunda = 1,
    Terca = 2,
    Quarta = 3,
    Quinta = 4,
    Sexta = 5,
    Sabado = 6
}
