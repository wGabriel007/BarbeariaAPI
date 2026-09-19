using Barbearia.Application.Abstracoes;
using Barbearia.Domain.Entidades;
using Microsoft.EntityFrameworkCore;

namespace Barbearia.Infrastructure.Persistencia.Repositorios;

public class BarbeiroRepository : EfRepository<Barbeiro>, IBarbeiroRepository
{
    public BarbeiroRepository(BarbeariaDbContext context) : base(context)
    {
    }

    public async Task<Barbeiro?> FnObterComHorariosAsync(long id, CancellationToken ct = default) =>
        await Context.Barbeiros.Include(b => b.Horarios).Include(b => b.Usuario).FirstOrDefaultAsync(b => b.Id == id, ct);

    public async Task<bool> FnExistePorUsuarioIdAsync(long usuarioId, CancellationToken ct = default) =>
        await Context.Barbeiros.AnyAsync(b => b.UsuarioId == usuarioId, ct);

    public async Task<Barbeiro?> FnObterPorUsuarioIdAsync(long usuarioId, CancellationToken ct = default) =>
        await Context.Barbeiros.FirstOrDefaultAsync(b => b.UsuarioId == usuarioId, ct);

    // Sobrescrito de propósito: a versão genérica (EfRepository<T>) não
    // inclui Horarios nem Usuario, e a tela de listagem de barbeiros
    // também precisa dos dois (horários pra mostrar a agenda, Usuario
    // pra mostrar o NOME em vez de "Barbeiro #1") — sem este override,
    // BarbeiroService.FnListarAsync mostraria isso tudo vazio por engano.
    public override async Task<List<Barbeiro>> FnListarAsync(CancellationToken ct = default) =>
        await Context.Barbeiros.Include(b => b.Horarios).Include(b => b.Usuario).AsNoTracking().ToListAsync(ct);
}
