using Barbearia.Domain.Entidades;

namespace Barbearia.Application.Abstracoes;

/// <summary>
/// Não herda de IRepository&lt;T&gt; de propósito: ConfiguracaoSite é um
/// singleton (uma linha só, ver ConfiguracaoSite.IdUnico) — não existe
/// "adicionar" outra, nem "listar", nem "remover". Só FnObterAsync, porque
/// a linha já nasce pronta pela migração (ver
/// 09_migracao_configuracao_site.sql).
/// </summary>
public interface IConfiguracaoSiteRepository
{
    Task<ConfiguracaoSite> FnObterAsync(CancellationToken ct = default);
}
