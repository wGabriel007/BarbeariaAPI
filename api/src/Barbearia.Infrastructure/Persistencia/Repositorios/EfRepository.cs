using Barbearia.Application.Comum;
using Barbearia.Domain.Comum;
using Microsoft.EntityFrameworkCore;

namespace Barbearia.Infrastructure.Persistencia.Repositorios;

/// <summary>
/// Implementação genérica de IRepository&lt;T&gt; — cobre o CRUD básico
/// que é sempre igual. Repositórios específicos (ClienteRepository,
/// AgendamentoRepository etc.) herdam desta classe e só adicionam os
/// métodos de consulta que são específicos daquela entidade.
/// </summary>
public class EfRepository<T> : IRepository<T> where T : Entity
{
    protected readonly BarbeariaDbContext Context;

    public EfRepository(BarbeariaDbContext context)
    {
        Context = context;
    }

    public virtual async Task<T?> FnObterPorIdAsync(long id, CancellationToken ct = default) =>
        await Context.Set<T>().FindAsync(new object[] { id }, ct);

    public virtual async Task<List<T>> FnListarAsync(CancellationToken ct = default) =>
        await Context.Set<T>().AsNoTracking().ToListAsync(ct);

    public async Task FnAdicionarAsync(T entidade, CancellationToken ct = default) =>
        await Context.Set<T>().AddAsync(entidade, ct);

    public void FnRemover(T entidade) => Context.Set<T>().Remove(entidade);
}
