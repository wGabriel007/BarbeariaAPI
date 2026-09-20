using Barbearia.Domain.Comum;
using Barbearia.Domain.Enumeracoes;

namespace Barbearia.Domain.Entidades;

public class Pagamento : AuditableEntity
{
    public long? AgendamentoId { get; private set; }
    public long? AssinaturaId { get; private set; }
    public long ClienteId { get; private set; }
    public decimal Valor { get; private set; }
    public FormaPagamento Forma { get; private set; }
    public StatusPagamento Status { get; private set; }
    public DateTimeOffset? PagoEm { get; private set; }

    /// <summary>Barbearia (Empresa) dona deste pagamento — sempre igual ao EmpresaId do Cliente (ver FnAtribuirEmpresa).</summary>
    public long EmpresaId { get; private set; }

    private Pagamento()
    {
    }

    public static Pagamento FnCriar(
        long clienteId,
        decimal valor,
        FormaPagamento forma,
        long? agendamentoId = null,
        long? assinaturaId = null)
    {
        if (clienteId <= 0)
            throw new DomainException("ClienteId inválido.");

        if (valor < 0)
            throw new DomainException("Valor não pode ser negativo.");

        if (agendamentoId is null && assinaturaId is null)
            throw new DomainException("Um pagamento precisa estar ligado a um agendamento ou a uma assinatura.");

        return new Pagamento
        {
            ClienteId = clienteId,
            Valor = valor,
            Forma = forma,
            AgendamentoId = agendamentoId,
            AssinaturaId = assinaturaId,
            Status = StatusPagamento.Pendente
        };
    }

    /// <summary>
    /// Recebe a forma de pagamento aqui (e não em FnCriar) de propósito:
    /// quando o pagamento nasce Pendente (ex.: logo que um atendimento é
    /// concluído — ver AgendamentoService.FnConcluirAsync), ainda não se
    /// sabe COMO o cliente vai pagar. "Forma" só é um fato real no
    /// momento em que o barbeiro confirma o pagamento, então é aqui que
    /// ela é registrada de verdade (substituindo o valor provisório
    /// gravado em FnCriar).
    /// </summary>
    public void FnConfirmarPagamento(DateTimeOffset pagoEm, FormaPagamento forma)
    {
        if (Status != StatusPagamento.Pendente)
            throw new DomainException($"Não é possível confirmar um pagamento com status {Status}.");

        Status = StatusPagamento.Pago;
        PagoEm = pagoEm;
        Forma = forma;
    }

    public void FnCancelar()
    {
        if (Status == StatusPagamento.Pago)
            throw new DomainException("Não é possível cancelar um pagamento já confirmado — use FnReembolsar().");

        Status = StatusPagamento.Cancelado;
    }

    public void FnReembolsar()
    {
        if (Status != StatusPagamento.Pago)
            throw new DomainException("Só é possível reembolsar um pagamento que já foi pago.");

        Status = StatusPagamento.Reembolsado;
    }

    /// <summary>Chamado uma única vez, logo após FnCriar, sempre com o EmpresaId do Cliente dono deste pagamento.</summary>
    public void FnAtribuirEmpresa(long empresaId)
    {
        if (EmpresaId != 0)
            throw new DomainException("Este pagamento já pertence a uma barbearia.");

        if (empresaId <= 0)
            throw new DomainException("EmpresaId inválido.");

        EmpresaId = empresaId;
    }
}
