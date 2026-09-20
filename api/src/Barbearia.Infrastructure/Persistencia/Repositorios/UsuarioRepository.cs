using Barbearia.Application.Abstracoes;
using Barbearia.Domain.Entidades;
using Microsoft.EntityFrameworkCore;

namespace Barbearia.Infrastructure.Persistencia.Repositorios;

public class UsuarioRepository : EfRepository<Usuario>, IUsuarioRepository
{
    public UsuarioRepository(BarbeariaDbContext context) : base(context)
    {
    }

    public async Task<Usuario?> FnObterPorEmailAsync(string email, CancellationToken ct = default) =>
        await Context.Usuarios.FirstOrDefaultAsync(u => u.Email == email, ct);

    public async Task<Usuario?> FnObterSuperAdminPorEmailAsync(string email, CancellationToken ct = default) =>
        await Context.Usuarios.IgnoreQueryFilters()
            .FirstOrDefaultAsync(u => u.Email == email && u.Tipo == Domain.Enumeracoes.TipoUsuario.SuperAdmin, ct);

    public async Task<bool> FnExisteAlgumAsync(CancellationToken ct = default) =>
        await Context.Usuarios.AnyAsync(ct);
}
