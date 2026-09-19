using Barbearia.Application.Comum;
using Barbearia.Domain.Entidades;

namespace Barbearia.Application.Abstracoes;

public interface IPagamentoRepository : IRepository<Pagamento>
{
    /// <summary>
    /// Todo pagamento criado no intervalo [de, ate) — usado tanto pra "os
    /// pagamentos de hoje" (intervalo de um dia) quanto pro histórico
    /// (intervalo maior, ex.: últimos 7 dias). Ver PagamentoService.
    /// </summary>
    Task<List<Pagamento>> FnListarPorPeriodoAsync(DateTimeOffset de, DateTimeOffset ate, CancellationToken ct = default);

    /// <summary>Histórico de pagamentos (qualquer status) de UM cliente — base do "cartão do cliente" na aba Usuários (ver ClienteService).</summary>
    Task<List<Pagamento>> FnListarPorClienteAsync(long clienteId, CancellationToken ct = default);
}
