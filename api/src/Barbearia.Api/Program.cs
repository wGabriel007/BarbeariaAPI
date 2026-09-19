using System.Text;
using System.Text.Json.Serialization;
using Barbearia.Api.Middleware;
using Barbearia.Application.Servicos;
using Barbearia.Infrastructure;
using Barbearia.Infrastructure.Seguranca;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Tokens;

// Precisa ser a PRIMEIRA coisa do programa, antes de qualquer uso do
// Npgsql (mesmo indireto, tipo montar o DbContext): desde a versão 6 o
// Npgsql passou a exigir que todo DateTimeOffset gravado num
// "timestamp with time zone" já venha com Offset=0 (UTC) — qualquer
// outro offset (ex.: os -03:00 que este sistema usa pra calcular "hoje"
// no fuso da loja, ver AgendamentoService.FusoLoja) dá
// "Cannot write DateTimeOffset with Offset=-03:00:00..." na hora de
// montar a query. Essa switch liga de volta o comportamento antigo (o
// Npgsql converte pra UTC nos bastidores em vez de recusar), que é o que
// este código sempre assumiu.
AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

// Carrega o arquivo .env (se existir) ANTES de WebApplication.CreateBuilder,
// porque CreateBuilder é o momento em que o ASP.NET Core lê as variáveis
// de ambiente do processo — carregar depois seria tarde demais. DotNetEnv
// só copia cada linha do .env para uma variável de ambiente do processo;
// dali em diante é o próprio ASP.NET Core (sem nenhum código nosso) que já
// sabe ler "ConnectionStrings__Barbearia" como ConnectionStrings:Barbearia
// (o "__" é como se escreve ":" numa variável de ambiente).
//
// Em produção normalmente não existe um arquivo .env (as variáveis vêm
// do próprio sistema/host) — daí o "if": sem o arquivo, isso não faz
// nada, e a Api segue lendo appsettings.json normalmente.
if (File.Exists(".env"))
{
    DotNetEnv.Env.Load();
}

var builder = WebApplication.CreateBuilder(args);

// ---------------------------------------------------------------------
// Controllers + serialização JSON
// ---------------------------------------------------------------------
builder.Services
    .AddControllers()
    .AddJsonOptions(options =>
    {
        // Isto é o que faz TODO enum (StatusRegistro, StatusAgendamento,
        // TipoUsuario, etc.) aparecer no JSON como texto ("Ativo") em vez
        // do número interno (1) — tanto ao serializar respostas quanto ao
        // desserializar requisições (aceita tanto "Ativo" quanto 1, o
        // que ajuda quem ainda está testando via Swagger/Postman).
        // Configurado uma vez aqui: nenhum DTO precisa saber disso.
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });

// ---------------------------------------------------------------------
// Infrastructure (DbContext, repositórios, UnitOfWork, hasher de senha)
// se liga com uma linha só — Program.cs não precisa saber que por trás
// tem EF Core, Npgsql, BCrypt etc.
// ---------------------------------------------------------------------
builder.Services.FnAddInfrastructure(builder.Configuration);

// ---------------------------------------------------------------------
// Services do Application. Ficam registrados aqui (em vez de um
// AddApplication() dentro do próprio Application, como fizemos com o
// FnAddInfrastructure) de propósito: assim o projeto Application continua
// com ZERO pacotes NuGet — nem mesmo o pacote de abstrações de DI —
// porque quem sabe montar um IServiceCollection é responsabilidade da
// Api (o "composition root"), não da camada de regras de negócio.
//
// Scoped: cada Service depende de repositórios/IUnitOfWork, que por sua
// vez dependem do DbContext — e o DbContext é Scoped (uma instância por
// requisição HTTP). FnRegistrar como Singleton aqui seria um bug: o
// Service ficaria "preso" ao DbContext da primeira requisição pra
// sempre.
// ---------------------------------------------------------------------
builder.Services.AddScoped<ClienteService>();
builder.Services.AddScoped<ServicoService>();
builder.Services.AddScoped<BarbeiroService>();
builder.Services.AddScoped<PlanoAssinaturaService>();
builder.Services.AddScoped<AssinaturaService>();
builder.Services.AddScoped<AgendamentoService>();
builder.Services.AddScoped<UsuarioService>();
builder.Services.AddScoped<AutenticacaoService>();
builder.Services.AddScoped<PagamentoService>();
builder.Services.AddScoped<SolicitacaoPlanoService>();
builder.Services.AddScoped<ConfiguracaoSiteService>();
builder.Services.AddScoped<RankingService>();

// ---------------------------------------------------------------------
// Autenticação (JWT) — a Api só sabe VALIDAR o token aqui (assinatura,
// emissor, audiência, validade). Quem GERA o token é a Infrastructure
// (JwtTokenGenerator) — ver comentário em JwtSettings.FnLerDaConfiguracao
// sobre por que os dois lados compartilham a mesma leitura de config.
// ---------------------------------------------------------------------
var jwtSettings = JwtSettings.FnLerDaConfiguracao(builder.Configuration);

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtSettings.Issuer,
            ValidateAudience = true,
            ValidAudience = jwtSettings.Audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.Key)),
            ValidateLifetime = true,
            // Sem isso, o .NET aceita o token até 5 minutos depois de
            // expirado "por tolerância" — ok pra maioria dos sistemas,
            // mas preferimos ser estritos aqui: expirou, expirou.
            ClockSkew = TimeSpan.Zero,
        };
    });

// FallbackPolicy = toda rota exige um usuário autenticado POR PADRÃO,
// mesmo sem escrever [Authorize] em cada Controller — só o
// AuthController (login/registrar) marca [AllowAnonymous] pra abrir uma
// exceção. Isso é mais seguro do que o padrão contrário (tudo público a
// menos que eu lembre de marcar [Authorize]): esquecer de proteger uma
// rota nova é um erro fácil de cometer e fácil de não notar.
builder.Services.AddAuthorization(options =>
{
    options.FallbackPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();
});

// ---------------------------------------------------------------------
// Swagger / OpenAPI
// ---------------------------------------------------------------------
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "Barbearia API",
        Version = "v1",
        Description = "API para gestão de clientes, barbeiros, serviços, planos de assinatura e agendamentos de uma barbearia.",
    });

    // Adiciona o botão "Authorize" no Swagger UI — depois de logar via
    // /api/auth/login, cola o token ali (só o token, o Swagger já
    // adiciona o prefixo "Bearer ") e testa as rotas protegidas direto
    // pelo navegador, sem precisar configurar nada no Postman.
    var esquemaBearer = new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Description = "Cole aqui só o token (sem escrever \"Bearer \" — o Swagger já adiciona).",
    };
    options.AddSecurityDefinition("Bearer", esquemaBearer);
    options.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        { esquemaBearer, Array.Empty<string>() },
    });
});

// ---------------------------------------------------------------------
// CORS — liberado para o front (React) rodando em outra origem/porta
// durante o desenvolvimento (ex.: http://localhost:5173 no Vite).
// Em produção, troque "AllowAnyOrigin" pela URL real do front.
// ---------------------------------------------------------------------
const string CorsPolicyDev = "FrontendDev";
builder.Services.AddCors(options =>
{
    options.AddPolicy(CorsPolicyDev, policy => policy
        .AllowAnyOrigin()
        .AllowAnyMethod()
        .AllowAnyHeader());
});

var app = builder.Build();

// ---------------------------------------------------------------------
// Migrações pendentes (ver Barbearia.Infrastructure/Persistence/
// MigrationRunner.cs) — aplicadas ANTES de começar a atender qualquer
// requisição, dentro de um scope próprio (o DbContext é Scoped, e aqui
// fora do pipeline HTTP não existe nenhum scope de requisição pronto).
// Se der erro, deixa a exceção subir e derrubar a Api aqui mesmo — é
// melhor um crash claro no console ao ligar do que rodar com o banco
// desatualizado e dar erro 500 confuso na primeira tela que usar.
// ---------------------------------------------------------------------
using (var scope = app.Services.CreateScope())
{
    var migrationRunner = scope.ServiceProvider.GetRequiredService<Barbearia.Infrastructure.Persistencia.MigrationRunner>();
    await migrationRunner.FnAplicarPendentesAsync();
}

// ---------------------------------------------------------------------
// Pipeline HTTP
// ---------------------------------------------------------------------

// Precisa ser o PRIMEIRO middleware do pipeline: se ficar depois de
// algum outro, uma exceção lançada por ESSE outro middleware não seria
// capturada por ele.
app.FnUseExceptionHandling();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Redireciona http -> https só fora de Development. Em Development o
// front (Vite, http://localhost:5173) chama a Api em HTTP mesmo — se a
// Api redireciona pra https://localhost:7289, o navegador tenta seguir
// o redirect, desconfia do certificado de desenvolvimento (autoassinado)
// e a requisição morre antes de chegar em qualquer Controller. Em
// produção, com um certificado de verdade, o redirect volta a valer.
if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseCors(CorsPolicyDev);

// Serve os arquivos enviados por upload (foto de usuário, logo da
// barbearia — ver Barbearia.Infrastructure/Storage/ArmazenamentoArquivosLocal)
// como arquivos estáticos comuns, em /uploads/... — sem exigir login pra
// ver uma foto (senão nem a tela de FnLogin, que ainda não tem token
// nenhum, conseguiria mostrar a logo). Garante que a pasta exista ANTES
// de registrar o middleware — em uma instalação nova, ninguém fez
// upload de nada ainda, então wwwroot/uploads não existe até aqui.
Directory.CreateDirectory(Path.Combine(app.Environment.ContentRootPath, "wwwroot", "uploads"));
app.UseStaticFiles();

// Authentication ANTES de Authorization, sempre — Authentication
// descobre "quem é" (lê e valida o token, preenche HttpContext.User);
// Authorization decide "pode ou não" com base nisso. Na ordem trocada,
// Authorization sempre veria um usuário anônimo.
app.UseAuthentication();

// Depois de Authentication (precisa de HttpContext.User já preenchido) e
// antes de Authorization: garante que um usuário bloqueado/inativo perde
// o acesso na hora, mesmo com um token ainda válido — ver o comentário
// completo em StatusUsuarioMiddleware.cs.
app.FnUseStatusUsuario();

app.UseAuthorization();

app.MapControllers();

app.Run();
