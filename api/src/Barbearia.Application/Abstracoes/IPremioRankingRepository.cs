using Barbearia.Application.Comum;
using Barbearia.Domain.Entidades;

namespace Barbearia.Application.Abstracoes;

public interface IPremioRankingRepository : IRepository<PremioRanking>
{
    /// <summary>Todos os prêmios configurados para um mês específico, ordenados por posição (1º, 2º, 3º...).</summary>
    Task<List<PremioRanking>> FnListarPorMesAsync(int mes, int ano, CancellationToken ct = default);
}
