using Barbearia.Application.Comum;
using Barbearia.Domain.Entidades;

namespace Barbearia.Application.Abstracoes;

/// <summary>
/// Não herda de IRepository&lt;T&gt; genérico de propósito: Empresa é a
/// própria "raiz" do multi-tenant — não faz parte do que é filtrado por
/// EmpresaId (ela É a EmpresaId de todo o resto), então suas consultas
/// são sempre GLOBAIS (ver EmpresaRepository, que usa IgnoreQueryFilters
/// nem precisa, já que Empresa nunca tem HasQueryFilter nenhum).
/// </summary>
public interface IEmpresaRepository
{
    Task<Empresa?> FnObterPorIdAsync(long id, CancellationToken ct = default);

    /// <summary>Usado pelo EmpresaResolverMiddleware pra resolver a barbearia a partir do link (ex.: /barbearia-do-joao) — e por FnCriarAsync pra garantir que o slug escolhido é único.</summary>
    Task<Empresa?> FnObterPorSlugAsync(string slug, CancellationToken ct = default);

    Task<List<Empresa>> FnListarAsync(CancellationToken ct = default);

    Task FnAdicionarAsync(Empresa empresa, CancellationToken ct = default);
}
