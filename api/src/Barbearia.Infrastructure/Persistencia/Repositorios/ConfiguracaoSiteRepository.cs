using Barbearia.Application.Abstracoes;
using Barbearia.Domain.Entidades;
using Microsoft.EntityFrameworkCore;

namespace Barbearia.Infrastructure.Persistencia.Repositorios;

public class ConfiguracaoSiteRepository : IConfiguracaoSiteRepository
{
    private readonly BarbeariaDbContext _context;

    public ConfiguracaoSiteRepository(BarbeariaDbContext context)
    {
        _context = context;
    }

    // SingleAsync (não FirstOrDefault) de propósito: se essa linha não
    // existir, é porque a migração 09 não rodou — melhor um erro claro
    // aqui do que silenciosamente tratar "sem configuração" como um
    // estado válido em todo o resto do sistema.
    //
    // SEM .AsNoTracking(): diferente da maioria dos repositórios de
    // leitura deste sistema, esta linha quase sempre é buscada pra em
    // seguida ser alterada (FnAtualizar/FnAtualizarInformacoes/FnAdicionarFoto/
    // FnRemoverFoto, todos no mesmo Service) — o EF Core precisa
    // continuar rastreando ela (e a coleção Fotos) pra um FnSalvarAsync
    // subsequente gerar o UPDATE/INSERT/DELETE certo.
    public async Task<ConfiguracaoSite> FnObterAsync(CancellationToken ct = default) =>
        await _context.ConfiguracoesSite
            .Include(c => c.Fotos)
            .SingleAsync(c => c.Id == ConfiguracaoSite.IdUnico, ct);
}
