using Barbearia.Application.Abstracoes;
using Barbearia.Domain.Entidades;
using Microsoft.EntityFrameworkCore;

namespace Barbearia.Infrastructure.Persistencia.Repositorios;

public class AssinaturaRepository : EfRepository<Assinatura>, IAssinaturaRepository
{
    public AssinaturaRepository(BarbeariaDbContext context) : base(context)
    {
    }

    public async Task<List<Assinatura>> FnListarPorClienteAsync(long clienteId, CancellationToken ct = default) =>
        await Context.Assinaturas.Where(a => a.ClienteId == clienteId).AsNoTracking().ToListAsync(ct);
}
