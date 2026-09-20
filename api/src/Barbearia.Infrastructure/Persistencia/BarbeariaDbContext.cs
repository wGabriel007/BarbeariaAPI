using Barbearia.Application.Abstracoes;
using Barbearia.Domain.Entidades;
using Microsoft.EntityFrameworkCore;

namespace Barbearia.Infrastructure.Persistencia;

/// <summary>
/// IMPORTANTE — decisão de arquitetura desta Fase 2: este projeto NÃO
/// usa EF Core Migrations. O schema do banco já existe, foi desenhado
/// e testado manualmente em SQL puro na Fase 1 (ver pasta /database:
/// 01_schema.sql e 02_seed.sql) — inclusive com recursos que o EF Core
/// não sabe gerar via Fluent API, como o EXCLUDE constraint que
/// impede conflito de horário em agendamentos.
///
/// Esse DbContext só MAPEIA para o schema que já existe (a fonte da
/// verdade do schema continua sendo os arquivos .sql). Se um dia
/// precisar mudar o schema, o caminho é: alterar/criar um arquivo
/// "NN_migracao_*.sql" em /database, e SÓ DEPOIS espelhar a mudança
/// aqui nas entidades/configurações.
///
/// Por isso você NÃO vai encontrar aqui: CHECK constraints, triggers,
/// EXCLUDE constraint, índices parciais — tudo isso já existe no
/// banco via SQL, e duplicá-lo aqui não teria efeito nenhum (nunca
/// chamamos dbContext.Database.Migrate() ou EnsureCreated() — isso
/// continua true, o projeto não usa EF Core Migrations).
///
/// O que MUDOU: os arquivos "NN_migracao_*.sql" não precisam mais ser
/// rodados manualmente (psql/pgAdmin) — a própria Api aplica qualquer
/// um pendente sozinha ao subir (ver MigrationRunner, chamado no
/// Program.cs antes de app.Run()). 01_schema.sql e 02_seed.sql
/// continuam sendo passo manual único, só pra criar um banco do zero.
/// </summary>
public class BarbeariaDbContext : DbContext
{
    // Injetado (Scoped, igual o próprio DbContext) — ver comentário
    // completo em ICurrentTenantService sobre quem implementa isto e
    // como é resolvido a cada requisição. Guardado num campo pra poder
    // ser referenciado dentro dos HasQueryFilter em OnModelCreating (uma
    // classe IEntityTypeConfiguration<T> não serve pra isto, porque o EF
    // Core instancia essas classes sozinho via reflexão, sem injeção de
    // dependência — por isso os filtros multi-tenant moram AQUI, não em
    // Persistencia/Configuracoes/*.cs).
    private readonly ICurrentTenantService _tenant;

    public BarbeariaDbContext(DbContextOptions<BarbeariaDbContext> options, ICurrentTenantService tenant) : base(options)
    {
        _tenant = tenant;
    }

    // O nome de cada DbSet é o que define o nome da tabela (depois de
    // passar pela convenção snake_case) — por isso os nomes abaixo
    // batem exatamente com os nomes das tabelas no 01_schema.sql.
    public DbSet<Empresa> Empresas => Set<Empresa>();
    public DbSet<Usuario> Usuarios => Set<Usuario>();
    public DbSet<Barbeiro> Barbeiros => Set<Barbeiro>();
    public DbSet<Cliente> Clientes => Set<Cliente>();
    public DbSet<Servico> Servicos => Set<Servico>();
    public DbSet<PlanoAssinatura> PlanosAssinatura => Set<PlanoAssinatura>();
    public DbSet<PlanoServico> PlanoServicos => Set<PlanoServico>();
    public DbSet<Assinatura> Assinaturas => Set<Assinatura>();
    public DbSet<HorarioTrabalho> HorariosTrabalho => Set<HorarioTrabalho>();
    public DbSet<BloqueioAgenda> BloqueiosAgenda => Set<BloqueioAgenda>();
    public DbSet<Agendamento> Agendamentos => Set<Agendamento>();
    public DbSet<Pagamento> Pagamentos => Set<Pagamento>();
    public DbSet<SolicitacaoPlano> SolicitacoesPlano => Set<SolicitacaoPlano>();
    public DbSet<ConfiguracaoSite> ConfiguracoesSite => Set<ConfiguracaoSite>();
    public DbSet<FotoBarbearia> FotosBarbearia => Set<FotoBarbearia>();
    public DbSet<PremioRanking> PremiosRanking => Set<PremioRanking>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Aplica toda classe IEntityTypeConfiguration<T> que existir
        // neste assembly (uma por entidade, em Persistence/Configurations/)
        // em vez de configurar tudo aqui dentro — mantém este arquivo
        // pequeno e cada configuração isolada por entidade.
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(BarbeariaDbContext).Assembly);

        // -----------------------------------------------------------------
        // Isolamento multi-tenant (ver 14_migracao_multi_barbearia.sql e
        // ICurrentTenantService) — UM HasQueryFilter por entidade que tem
        // EmpresaId. A partir daqui, TODA consulta LINQ feita através
        // deste DbContext (inclusive as que já existiam em cada
        // Repositorio, sem precisar mudar nenhuma) devolve só as linhas
        // da barbearia da requisição atual — automaticamente. Comparação
        // com EmpresaId (long) de um lado e _tenant.EmpresaId (long?) do
        // outro: se _tenant.EmpresaId for null (barbearia não resolvida),
        // a comparação nunca bate com nada — seguro por padrão.
        //
        // EhSuperAdmin ignora o filtro (usado por EmpresaService, que
        // administra a tabela empresas em si — Empresa não entra
        // aqui porque ELA é a raiz do isolamento, não algo isolado).
        //
        // Se algum dia isto precisar ser "furado" de propósito (ex.: um
        // relatório cross-tenant do SuperAdmin), use
        // .IgnoreQueryFilters() na consulta específica — nunca remova o
        // filtro daqui.
        modelBuilder.Entity<Usuario>().HasQueryFilter(u => _tenant.EhSuperAdmin || u.EmpresaId == _tenant.EmpresaId);
        modelBuilder.Entity<Cliente>().HasQueryFilter(c => _tenant.EhSuperAdmin || c.EmpresaId == _tenant.EmpresaId);
        modelBuilder.Entity<Barbeiro>().HasQueryFilter(b => _tenant.EhSuperAdmin || b.EmpresaId == _tenant.EmpresaId);
        modelBuilder.Entity<Servico>().HasQueryFilter(s => _tenant.EhSuperAdmin || s.EmpresaId == _tenant.EmpresaId);
        modelBuilder.Entity<PlanoAssinatura>().HasQueryFilter(p => _tenant.EhSuperAdmin || p.EmpresaId == _tenant.EmpresaId);
        modelBuilder.Entity<Assinatura>().HasQueryFilter(a => _tenant.EhSuperAdmin || a.EmpresaId == _tenant.EmpresaId);
        modelBuilder.Entity<Agendamento>().HasQueryFilter(a => _tenant.EhSuperAdmin || a.EmpresaId == _tenant.EmpresaId);
        modelBuilder.Entity<Pagamento>().HasQueryFilter(p => _tenant.EhSuperAdmin || p.EmpresaId == _tenant.EmpresaId);
        modelBuilder.Entity<SolicitacaoPlano>().HasQueryFilter(s => _tenant.EhSuperAdmin || s.EmpresaId == _tenant.EmpresaId);
        modelBuilder.Entity<ConfiguracaoSite>().HasQueryFilter(c => _tenant.EhSuperAdmin || c.EmpresaId == _tenant.EmpresaId);
        modelBuilder.Entity<PremioRanking>().HasQueryFilter(p => _tenant.EhSuperAdmin || p.EmpresaId == _tenant.EmpresaId);

        // PlanoServico, HorarioTrabalho, BloqueioAgenda e FotoBarbearia NÃO
        // têm EmpresaId próprio de propósito — são sempre acessados só
        // através da entidade "pai" já filtrada (PlanoAssinatura, Barbeiro,
        // ConfiguracaoSite), nunca consultados soltos por um Repositorio.
    }
}
