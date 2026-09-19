using Barbearia.Domain.Comum;

namespace Barbearia.Domain.Entidades;

/// <summary>Férias, atestados, ausências pontuais de um barbeiro.</summary>
public class BloqueioAgenda : Entity
{
    public long BarbeiroId { get; private set; }
    public DateTimeOffset Inicio { get; private set; }
    public DateTimeOffset Fim { get; private set; }
    public string? Motivo { get; private set; }

    private BloqueioAgenda()
    {
    }

    public static BloqueioAgenda FnCriar(long barbeiroId, DateTimeOffset inicio, DateTimeOffset fim, string? motivo = null)
    {
        if (barbeiroId <= 0)
            throw new DomainException("BarbeiroId inválido.");

        if (fim <= inicio)
            throw new DomainException("Fim do bloqueio precisa ser depois do início.");

        return new BloqueioAgenda
        {
            BarbeiroId = barbeiroId,
            Inicio = inicio,
            Fim = fim,
            Motivo = motivo
        };
    }
}
