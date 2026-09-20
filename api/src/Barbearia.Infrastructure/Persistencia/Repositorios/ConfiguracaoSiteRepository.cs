using Barbearia.Application.Abstracoes;
using Barbearia.Application.Comum;
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

    // SingleOrDefaultAsync (não SingleAsync) de propósito: o HasQueryFilter
    // por EmpresaId (ver BarbeariaDbContext) já restringe esta consulta à
    // barbearia da requisição atual — normalmente sobra exatamente UMA
    // linha (a que EmpresaService.FnCriarAsync criou junto com a Empresa).
    // MAS, diferente de antes (quando só existia uma barbearia no sistema
    // inteiro), agora "nenhuma linha" é um caso ESPERADO e não um bug: é
    // o que acontece quando alguém acessa um link com slug digitado
    // errado, ou de uma barbearia inativa/removida (ver
    // EmpresaResolverMiddleware — nesses casos o EmpresaId do tenant
    // nunca é resolvido, e o filtro não bate com nenhuma linha). Por
    // isso vira NotFoundException (-> HTTP 404, com mensagem amigável),
    // não mais um erro genérico de "estado inconsistente" (-> HTTP 500
    // cru, que o front não sabia distinguir de um bug de verdade — ver
    // paginas/BarbeariaNaoEncontrada.jsx no front, que trata esse 404).
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
            .SingleOrDefaultAsync(ct)
        ?? throw new NotFoundException("Barbearia não encontrada. Confira o link e tente novamente.");

    public async Task FnAdicionarAsync(ConfiguracaoSite configuracao, CancellationToken ct = default) =>
        await _context.ConfiguracoesSite.AddAsync(configuracao, ct);
}
