using Barbearia.Application.Comum;
using Barbearia.Domain.Entidades;

namespace Barbearia.Application.Abstracoes;

public interface ISolicitacaoPlanoRepository : IRepository<SolicitacaoPlano>
{
    /// <summary>"Aba de solicitações" do staff — todos os pedidos ainda Pendentes, de qualquer usuário.</summary>
    Task<List<SolicitacaoPlano>> FnListarPendentesAsync(CancellationToken ct = default);

    /// <summary>"Meus planos" do usuário logado — todos os pedidos que ele já fez, qualquer status.</summary>
    Task<List<SolicitacaoPlano>> FnListarPorUsuarioIdAsync(long usuarioId, CancellationToken ct = default);

    /// <summary>
    /// Usado por SolicitacaoPlanoService.FnSolicitarAsync pra impedir um
    /// usuário de ter mais de um pedido de plano em aberto ao mesmo
    /// tempo — só olha Pendente (Aceita/Rejeitada não contam, já foram
    /// decididas).
    /// </summary>
    Task<bool> FnExistePendentePorUsuarioIdAsync(long usuarioId, CancellationToken ct = default);
}
