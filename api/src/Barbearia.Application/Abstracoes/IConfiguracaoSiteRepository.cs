using Barbearia.Domain.Entidades;

namespace Barbearia.Application.Abstracoes;

/// <summary>
/// Não herda de IRepository&lt;T&gt; de propósito: uma barbearia tem NO
/// MÁXIMO uma linha de ConfiguracaoSite (ver EmpresaId e o índice único
/// em EmpresaConfiguration... digo, ConfiguracaoSiteConfiguration) — não
/// existe "listar" nem "remover", só FnObterAsync (a linha da PRÓPRIA
/// barbearia, resolvida pelo HasQueryFilter) e FnAdicionarAsync (chamado
/// uma única vez, por EmpresaService.FnCriarAsync, quando a barbearia
/// nasce).
/// </summary>
public interface IConfiguracaoSiteRepository
{
    Task<ConfiguracaoSite> FnObterAsync(CancellationToken ct = default);

    Task FnAdicionarAsync(ConfiguracaoSite configuracao, CancellationToken ct = default);
}
