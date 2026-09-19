using Barbearia.Domain.Comum;
using Barbearia.Domain.Enumeracoes;

namespace Barbearia.Domain.Entidades;

/// <summary>
/// Pedido de um usuário Comum pra assinar um plano — nasce Pendente,
/// esperando o Admin/Barbeiro aceitar (ver FnAceitar) ou rejeitar (ver
/// FnRejeitar) na aba "Solicitações". Guarda Email/Telefone digitados na
/// hora do pedido (e não os que já estavam na conta) porque é esse
/// contato que vira o do Cliente auto-provisionado quando aceito — ver
/// SolicitacaoPlanoService.FnAceitarAsync.
/// </summary>
public class SolicitacaoPlano : AuditableEntity
{
    public long UsuarioId { get; private set; }
    public long PlanoId { get; private set; }
    public string Email { get; private set; } = string.Empty;
    public string Telefone { get; private set; } = string.Empty;
    public StatusSolicitacaoPlano Status { get; private set; }
    public string? MensagemResposta { get; private set; }

    /// <summary>Preenchido só quando Aceita — a assinatura que nasceu a partir deste pedido.</summary>
    public long? AssinaturaId { get; private set; }

    private SolicitacaoPlano()
    {
    }

    public static SolicitacaoPlano FnCriar(long usuarioId, long planoId, string email, string telefone)
    {
        if (usuarioId <= 0)
            throw new DomainException("UsuarioId inválido.");

        if (planoId <= 0)
            throw new DomainException("PlanoId inválido.");

        if (string.IsNullOrWhiteSpace(email))
            throw new DomainException("E-mail é obrigatório.");

        if (string.IsNullOrWhiteSpace(telefone))
            throw new DomainException("Telefone é obrigatório.");

        return new SolicitacaoPlano
        {
            UsuarioId = usuarioId,
            PlanoId = planoId,
            Email = email.Trim(),
            Telefone = telefone.Trim(),
            Status = StatusSolicitacaoPlano.Pendente
        };
    }

    public void FnAceitar(long assinaturaId, string? mensagem = null)
    {
        if (Status != StatusSolicitacaoPlano.Pendente)
            throw new DomainException("Só é possível aceitar uma solicitação Pendente.");

        Status = StatusSolicitacaoPlano.Aceita;
        AssinaturaId = assinaturaId;
        MensagemResposta = mensagem;
    }

    public void FnRejeitar(string? mensagem = null)
    {
        if (Status != StatusSolicitacaoPlano.Pendente)
            throw new DomainException("Só é possível rejeitar uma solicitação Pendente.");

        Status = StatusSolicitacaoPlano.Rejeitada;
        MensagemResposta = mensagem;
    }
}
