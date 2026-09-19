using Barbearia.Domain.Comum;
using Barbearia.Domain.Enumeracoes;

namespace Barbearia.Domain.Entidades;

/// <summary>
/// Não herda de AuditableEntity: a tabela horarios_trabalho no
/// 01_schema.sql não tem criado_em/atualizado_em.
/// </summary>
public class HorarioTrabalho : Entity
{
    public long BarbeiroId { get; private set; }
    public DiaSemana DiaSemana { get; private set; }
    public TimeOnly HoraInicio { get; private set; }
    public TimeOnly HoraFim { get; private set; }

    private HorarioTrabalho()
    {
    }

    internal static HorarioTrabalho FnCriar(long barbeiroId, DiaSemana diaSemana, TimeOnly horaInicio, TimeOnly horaFim)
    {
        if (horaFim <= horaInicio)
            throw new DomainException("Hora de término precisa ser depois da hora de início.");

        return new HorarioTrabalho
        {
            BarbeiroId = barbeiroId,
            DiaSemana = diaSemana,
            HoraInicio = horaInicio,
            HoraFim = horaFim
        };
    }
}
