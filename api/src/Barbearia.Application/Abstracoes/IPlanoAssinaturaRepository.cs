using Barbearia.Application.Comum;
using Barbearia.Domain.Entidades;

namespace Barbearia.Application.Abstracoes;

public interface IPlanoAssinaturaRepository : IRepository<PlanoAssinatura>
{
    /// <summary>Traz o plano já com ServicosInclusos carregado (join), necessário pra FnIncluirServico funcionar sem duplicar.</summary>
    Task<PlanoAssinatura?> FnObterComServicosAsync(long id, CancellationToken ct = default);
}
