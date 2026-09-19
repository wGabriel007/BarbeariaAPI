using Barbearia.Domain.Comum;
using Barbearia.Domain.Enumeracoes;

namespace Barbearia.Domain.Entidades;

public class Assinatura : AuditableEntity
{
    public long ClienteId { get; private set; }
    public long PlanoId { get; private set; }
    public DateOnly DataInicio { get; private set; }
    public DateOnly? DataFim { get; private set; }

    /// <summary>
    /// Data EXATA do próximo vencimento (ex.: 2026-10-17) — não é mais um
    /// "dia do mês" (1-28) que se repete sozinho. Quem cadastra a
    /// assinatura (Admin/Barbeiro, na hora de criar ou de aceitar uma
    /// solicitação de plano) escolhe a data certa, sem precisar fazer
    /// conta de cabeça pra saber "que dia cai o dia 5 esse mês". Quando o
    /// vencimento passa e a pessoa renova, uma nova Assinatura nasce com
    /// a próxima data escolhida (mesma ideia de antes, só que mais
    /// explícita) — não existe um "avançar automaticamente pro próximo
    /// mês" aqui no Domain.
    /// </summary>
    public DateOnly DataVencimento { get; private set; }

    public StatusAssinatura Status { get; private set; }

    private Assinatura()
    {
    }

    public static Assinatura FnCriar(long clienteId, long planoId, DateOnly dataInicio, DateOnly dataVencimento, DateOnly? dataFim = null)
    {
        if (clienteId <= 0)
            throw new DomainException("ClienteId inválido.");

        if (planoId <= 0)
            throw new DomainException("PlanoId inválido.");

        if (dataVencimento < dataInicio)
            throw new DomainException("Data de vencimento não pode ser anterior à data de início.");

        if (dataFim.HasValue && dataFim.Value < dataInicio)
            throw new DomainException("Data de término não pode ser anterior à data de início.");

        return new Assinatura
        {
            ClienteId = clienteId,
            PlanoId = planoId,
            DataInicio = dataInicio,
            DataFim = dataFim,
            DataVencimento = dataVencimento,
            Status = StatusAssinatura.Ativa
        };
    }

    public void FnSuspender()
    {
        if (Status is StatusAssinatura.Cancelada or StatusAssinatura.Expirada)
            throw new DomainException($"Não é possível suspender uma assinatura com status {Status}.");

        Status = StatusAssinatura.Suspensa;
    }

    public void FnReativar()
    {
        if (Status != StatusAssinatura.Suspensa)
            throw new DomainException("Só é possível reativar uma assinatura suspensa.");

        Status = StatusAssinatura.Ativa;
    }

    public void FnCancelar(DateOnly dataCancelamento)
    {
        Status = StatusAssinatura.Cancelada;
        DataFim = dataCancelamento;
    }

    /// <summary>Chamado por um job periódico quando a assinatura passa da data_fim sem renovação.</summary>
    public void FnMarcarExpirada()
    {
        if (Status != StatusAssinatura.Ativa)
            throw new DomainException("Só é possível expirar uma assinatura ativa.");

        Status = StatusAssinatura.Expirada;
    }
}
