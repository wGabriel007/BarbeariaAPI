using Barbearia.Application.Abstracoes;
using Barbearia.Domain.Entidades;
using Microsoft.EntityFrameworkCore;

namespace Barbearia.Infrastructure.Persistencia.Repositorios;

public class PremioRankingRepository : EfRepository<PremioRanking>, IPremioRankingRepository
{
    public PremioRankingRepository(BarbeariaDbContext context) : base(context)
    {
    }

    // SEM AsNoTracking aqui de propósito, diferente da maioria dos outros
    // FnListar*Async deste projeto: RankingService.FnDefinirPremiosAsync
    // reaproveita este mesmo resultado pra CHAMAR FnAtualizarDescricao(...)
    // em cima de um prêmio já existente — precisa vir rastreado pelo EF
    // Core pra esse UPDATE ser salvo em FnSalvarAsync(). O outro uso deste
    // método (montar o ranking pra leitura) não se importa com isso.
    public async Task<List<PremioRanking>> FnListarPorMesAsync(int mes, int ano, CancellationToken ct = default) =>
        await Context.PremiosRanking
            .Where(p => p.Mes == mes && p.Ano == ano)
            .OrderBy(p => p.Posicao)
            .ToListAsync(ct);
}
