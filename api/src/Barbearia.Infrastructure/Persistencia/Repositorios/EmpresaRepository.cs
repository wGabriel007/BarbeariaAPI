using Barbearia.Application.Abstracoes;
using Barbearia.Domain.Entidades;
using Microsoft.EntityFrameworkCore;

namespace Barbearia.Infrastructure.Persistencia.Repositorios;

/// <summary>
/// Não herda EfRepository&lt;T&gt; de propósito (ver IEmpresaRepository): a
/// tabela empresas não tem HasQueryFilter nenhum (ela é a raiz do
/// isolamento, não algo isolado), então nem precisaria de
/// IgnoreQueryFilters — mas ela também não é acessada como toda outra
/// entidade multi-tenant (por isso a interface própria, mais enxuta).
/// </summary>
public class EmpresaRepository : IEmpresaRepository
{
    private readonly BarbeariaDbContext _context;

    public EmpresaRepository(BarbeariaDbContext context)
    {
        _context = context;
    }

    public async Task<Empresa?> FnObterPorIdAsync(long id, CancellationToken ct = default) =>
        await _context.Empresas.FindAsync(new object[] { id }, ct);

    public async Task<Empresa?> FnObterPorSlugAsync(string slug, CancellationToken ct = default) =>
        await _context.Empresas.AsNoTracking().FirstOrDefaultAsync(e => e.Slug == slug, ct);

    public async Task<List<Empresa>> FnListarAsync(CancellationToken ct = default) =>
        await _context.Empresas.AsNoTracking().OrderBy(e => e.Nome).ToListAsync(ct);

    public async Task FnAdicionarAsync(Empresa empresa, CancellationToken ct = default) =>
        await _context.Empresas.AddAsync(empresa, ct);
}
