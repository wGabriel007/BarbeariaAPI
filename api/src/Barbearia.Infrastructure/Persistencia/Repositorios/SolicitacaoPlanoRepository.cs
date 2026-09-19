using Barbearia.Application.Abstracoes;
using Barbearia.Domain.Entidades;
using Barbearia.Domain.Enumeracoes;
using Microsoft.EntityFrameworkCore;

namespace Barbearia.Infrastructure.Persistencia.Repositorios;

public class SolicitacaoPlanoRepository : EfRepository<SolicitacaoPlano>, ISolicitacaoPlanoRepository
{
    public SolicitacaoPlanoRepository(BarbeariaDbContext context) : base(context)
    {
    }

    public async Task<List<SolicitacaoPlano>> FnListarPendentesAsync(CancellationToken ct = default) =>
        await Context.SolicitacoesPlano
            .Where(s => s.Status == StatusSolicitacaoPlano.Pendente)
            .AsNoTracking()
            .ToListAsync(ct);

    public async Task<List<SolicitacaoPlano>> FnListarPorUsuarioIdAsync(long usuarioId, CancellationToken ct = default) =>
        await Context.SolicitacoesPlano
            .Where(s => s.UsuarioId == usuarioId)
            .AsNoTracking()
            .ToListAsync(ct);

    public async Task<bool> FnExistePendentePorUsuarioIdAsync(long usuarioId, CancellationToken ct = default) =>
        await Context.SolicitacoesPlano
            .AnyAsync(s => s.UsuarioId == usuarioId && s.Status == StatusSolicitacaoPlano.Pendente, ct);
}
