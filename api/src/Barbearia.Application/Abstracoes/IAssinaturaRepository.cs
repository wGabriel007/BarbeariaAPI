using Barbearia.Application.Comum;
using Barbearia.Domain.Entidades;

namespace Barbearia.Application.Abstracoes;

public interface IAssinaturaRepository : IRepository<Assinatura>
{
    Task<List<Assinatura>> FnListarPorClienteAsync(long clienteId, CancellationToken ct = default);
}
