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
    public BarbeariaDbContext(DbContextOptions<BarbeariaDbContext> options) : base(options)
    {
    }

    // O nome de cada DbSet é o que define o nome da tabela (depois de
    // passar pela convenção snake_case) — por isso os nomes abaixo
    // batem exatamente com os nomes das tabelas no 01_schema.sql.
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
    }
}
