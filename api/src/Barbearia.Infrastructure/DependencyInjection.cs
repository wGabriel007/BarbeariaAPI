using Barbearia.Application.Abstracoes;
using Barbearia.Application.Comum;
using Barbearia.Infrastructure.Persistencia;
using Barbearia.Infrastructure.Persistencia.Repositorios;
using Barbearia.Infrastructure.Seguranca;
using Barbearia.Infrastructure.Armazenamento;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Barbearia.Infrastructure;

/// <summary>
/// Ponto único onde a Api "liga" a Infrastructure — Program.cs só chama
/// builder.Services.FnAddInfrastructure(builder.Configuration) e não
/// precisa saber que por trás disso tem EF Core, Npgsql, BCrypt etc.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection FnAddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Barbearia")
            ?? throw new InvalidOperationException(
                "Connection string 'Barbearia' não configurada. Veja appsettings.Development.json ou dotnet user-secrets.");

        services.AddDbContext<BarbeariaDbContext>(options => options
            .UseNpgsql(connectionString)
            .UseSnakeCaseNamingConvention()); // vem do pacote EFCore.NamingConventions

        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<MigrationRunner>();

        services.AddScoped<IClienteRepository, ClienteRepository>();
        services.AddScoped<IServicoRepository, ServicoRepository>();
        services.AddScoped<IUsuarioRepository, UsuarioRepository>();
        services.AddScoped<IBarbeiroRepository, BarbeiroRepository>();
        services.AddScoped<IPlanoAssinaturaRepository, PlanoAssinaturaRepository>();
        services.AddScoped<IAssinaturaRepository, AssinaturaRepository>();
        services.AddScoped<IAgendamentoRepository, AgendamentoRepository>();
        services.AddScoped<IPagamentoRepository, PagamentoRepository>();
        services.AddScoped<ISolicitacaoPlanoRepository, SolicitacaoPlanoRepository>();
        services.AddScoped<IConfiguracaoSiteRepository, ConfiguracaoSiteRepository>();
        services.AddScoped<IPremioRankingRepository, PremioRankingRepository>();

        services.AddSingleton<IPasswordHasher, BCryptPasswordHasher>();

        // Diretório de trabalho durante "dotnet run"/publicação já é a
        // pasta do próprio projeto Api (onde fica wwwroot) — é o mesmo
        // caminho que o ASP.NET Core usaria sozinho como WebRootPath, só
        // que resolvido aqui pra não precisar referenciar um pacote de
        // hosting da Infrastructure (que continua sem saber que existe
        // ASP.NET Core por trás de quem a consome).
        services.AddSingleton<IArmazenamentoArquivos>(
            new ArmazenamentoArquivosLocal(Path.Combine(Directory.GetCurrentDirectory(), "wwwroot")));

        // JwtSettings é lido uma vez aqui e registrado como instância
        // fixa (não muda em runtime) — tanto o JwtTokenGenerator quanto
        // o Program.cs (pra configurar a validação do token recebido)
        // usam o mesmo JwtSettings.FnLerDaConfiguracao (ver o comentário
        // na própria classe).
        var jwtSettings = JwtSettings.FnLerDaConfiguracao(configuration);
        services.AddSingleton(jwtSettings);
        services.AddSingleton<IJwtTokenGenerator, JwtTokenGenerator>();

        return services;
    }
}
