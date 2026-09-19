using Barbearia.Application.Abstracoes;
using Barbearia.Domain.Entidades;
using Microsoft.EntityFrameworkCore;

namespace Barbearia.Infrastructure.Persistencia.Repositorios;

public class PagamentoRepository : EfRepository<Pagamento>, IPagamentoRepository
{
    public PagamentoRepository(BarbeariaDbContext context) : base(context)
    {
    }

    public async Task<List<Pagamento>> FnListarPorPeriodoAsync(DateTimeOffset de, DateTimeOffset ate, CancellationToken ct = default) =>
        await Context.Pagamentos
            .Where(p => p.CriadoEm >= de && p.CriadoEm < ate)
            .OrderByDescending(p => p.CriadoEm)
            .AsNoTracking()
            .ToListAsync(ct);

    public async Task<List<Pagamento>> FnListarPorClienteAsync(long clienteId, CancellationToken ct = default) =>
        await Context.Pagamentos
            .Where(p => p.ClienteId == clienteId)
            .OrderByDescending(p => p.CriadoEm)
            .AsNoTracking()
            .ToListAsync(ct);
}
