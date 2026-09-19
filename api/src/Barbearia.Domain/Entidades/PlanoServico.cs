using Barbearia.Domain.Comum;

namespace Barbearia.Domain.Entidades;

/// <summary>
/// Entidade de junção N:N entre PlanoAssinatura e Servico. Não herda de
/// Entity (não tem Id próprio) — a chave é o par (PlanoId, ServicoId),
/// igual à PRIMARY KEY composta em 01_schema.sql.
/// </summary>
public class PlanoServico
{
    public long PlanoId { get; private set; }
    public long ServicoId { get; private set; }
    public short LimiteMensal { get; private set; }

    public PlanoAssinatura? Plano { get; private set; }
    public Servico? Servico { get; private set; }

    private PlanoServico()
    {
    }

    internal static PlanoServico FnCriar(long planoId, long servicoId, short limiteMensal)
    {
        if (limiteMensal <= 0)
            throw new DomainException("Limite mensal precisa ser maior que zero.");

        return new PlanoServico
        {
            PlanoId = planoId,
            ServicoId = servicoId,
            LimiteMensal = limiteMensal
        };
    }
}
