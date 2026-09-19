using Barbearia.Application.Abstracoes;
using Barbearia.Domain.Entidades;
using Microsoft.EntityFrameworkCore;

namespace Barbearia.Infrastructure.Persistencia.Repositorios;

public class PlanoAssinaturaRepository : EfRepository<PlanoAssinatura>, IPlanoAssinaturaRepository
{
    public PlanoAssinaturaRepository(BarbeariaDbContext context) : base(context)
    {
    }

    public async Task<PlanoAssinatura?> FnObterComServicosAsync(long id, CancellationToken ct = default) =>
        await Context.PlanosAssinatura
            .Include(p => p.ServicosInclusos).ThenInclude(ps => ps.Servico)
            .FirstOrDefaultAsync(p => p.Id == id, ct);

    // Mesmo motivo do override em BarbeiroRepository: sem isso, a
    // listagem de planos mostraria ServicosInclusos sempre vazio — e o
    // .ThenInclude(Servico) é o que permite mostrar o NOME do serviço
    // (mesmo se ele estiver Inativo) em vez de "Serviço #1" (ver
    // PlanoAssinaturaService.FnMapear / ServicoIncluidoResponse).
    public override async Task<List<PlanoAssinatura>> FnListarAsync(CancellationToken ct = default) =>
        await Context.PlanosAssinatura
            .Include(p => p.ServicosInclusos).ThenInclude(ps => ps.Servico)
            .AsNoTracking().ToListAsync(ct);
}
