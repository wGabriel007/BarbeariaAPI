using System.Text.RegularExpressions;
using Barbearia.Domain.Comum;
using Barbearia.Domain.Enumeracoes;

namespace Barbearia.Domain.Entidades;

/// <summary>
/// Uma barbearia cadastrada na plataforma (multi-tenant) — chamada de
/// "Empresa" no código (não "Barbearia") só pra não colidir com o nome
/// do projeto inteiro (namespace Barbearia.*, BarbeariaDbContext etc.).
/// Toda entidade "de baixo" (Usuario, Cliente, Servico, PlanoAssinatura,
/// Assinatura, Agendamento, Pagamento, SolicitacaoPlano, ConfiguracaoSite,
/// PremioRanking) carrega um EmpresaId apontando pra uma linha desta
/// tabela — é isso que separa os dados de uma barbearia dos de outra,
/// mesmo compartilhando o mesmo banco (ver BarbeariaDbContext.OnModelCreating,
/// os HasQueryFilter por EmpresaId).
///
/// Slug é o "apelido" que vira parte do link que os CLIENTES da barbearia
/// acessam (ex.: seusite.vercel.app/barbearia-do-joao) — cadastrado uma
/// vez, na criação, e nunca muda depois (mudar quebraria um link já
/// divulgado pelo dono da barbearia).
/// </summary>
public class Empresa : AuditableEntity
{
    private static readonly Regex FormatoSlug = new("^[a-z0-9]+(-[a-z0-9]+)*$", RegexOptions.Compiled);

    // Palavras que o FRONT já usa como rota de nível raiz (ver
    // frontend/src/App.jsx: "/admin/..." é a área do SuperAdmin, "/login"
    // e "/cadastro" seriam ambíguas com "/:slug/login" de uma barbearia
    // chamada literalmente "login") — se uma barbearia pudesse nascer com
    // um desses slugs, o link dela colidiria com essas rotas fixas e uma
    // das duas coisas quebraria. Mantida aqui (Domain), não no front, por
    // ser regra de negócio de verdade (o que é um slug válido).
    private static readonly HashSet<string> SlugsReservados = new()
    {
        "admin", "login", "cadastro", "api", "app", "static", "assets", "favicon.ico",
    };

    public string Nome { get; private set; } = string.Empty;
    public string Slug { get; private set; } = string.Empty;
    public StatusRegistro Status { get; private set; }

    private Empresa()
    {
        // Construtor privado sem parâmetros: só o EF Core usa (reflexão).
        // Código de aplicação usa Empresa.FnCriar(...).
    }

    public static Empresa FnCriar(string nome, string slug)
    {
        if (string.IsNullOrWhiteSpace(nome))
            throw new DomainException("Nome da barbearia é obrigatório.");

        if (string.IsNullOrWhiteSpace(slug))
            throw new DomainException("O 'apelido' da barbearia (usado no link) é obrigatório.");

        var slugLimpo = slug.Trim().ToLowerInvariant();

        if (!FormatoSlug.IsMatch(slugLimpo))
            throw new DomainException(
                "O 'apelido' da barbearia só pode ter letras minúsculas, números e hífen (ex.: barbearia-do-joao), sem espaços ou acentos.");

        if (slugLimpo.Length > 80)
            throw new DomainException("O 'apelido' da barbearia não pode passar de 80 caracteres.");

        if (SlugsReservados.Contains(slugLimpo))
            throw new DomainException($"O 'apelido' '{slugLimpo}' é reservado pelo sistema — escolha outro.");

        return new Empresa
        {
            Nome = nome.Trim(),
            Slug = slugLimpo,
            Status = StatusRegistro.Ativo
        };
    }

    public void FnAtivar() => Status = StatusRegistro.Ativo;

    /// <summary>
    /// Desativa a barbearia inteira — ninguém dela (dono, barbeiros,
    /// clientes) consegue mais logar nem acessar o site enquanto estiver
    /// assim (ver EmpresaResolverMiddleware, que recusa uma Empresa fora
    /// de StatusRegistro.Ativo). Não apaga NADA — é reversível a
    /// qualquer momento com FnAtivar().
    /// </summary>
    public void FnInativar() => Status = StatusRegistro.Inativo;
}
