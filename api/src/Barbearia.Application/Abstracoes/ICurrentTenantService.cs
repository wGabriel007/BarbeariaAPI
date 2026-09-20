namespace Barbearia.Application.Abstracoes;

/// <summary>
/// "Qual barbearia (Empresa) é esta requisição" — resolvido uma vez por
/// requisição, bem cedo no pipeline (ver EmpresaResolverMiddleware na
/// Api), e usado em dois lugares:
///
///   1. BarbeariaDbContext.OnModelCreating — os HasQueryFilter de cada
///      entidade multi-tenant (Usuario, Cliente, Servico, ...) comparam
///      EmpresaId da linha com EmpresaId.Value daqui. É isso que separa
///      os dados de cada barbearia sem precisar mudar uma linha nos
///      Services/Repositórios já existentes (ver comentário completo em
///      BarbeariaDbContext).
///
///   2. Os poucos Services que criam uma entidade "raiz" do zero
///      (UsuarioService, ServicoService, PlanoAssinaturaService,
///      RankingService) — usam EmpresaId pra chamar FnAtribuirEmpresa
///      logo após FnCriar. Os outros Services (Cliente, Barbeiro,
///      Assinatura, Agendamento, Pagamento, SolicitacaoPlano) NÃO
///      precisam disto: eles derivam o EmpresaId de uma entidade "pai"
///      já carregada (o Usuario ou o Cliente), que já veio filtrada.
///
/// Implementação mora na Api (Barbearia.Api.Seguranca.CurrentTenantService),
/// não na Infrastructure nem aqui: ela lê de HttpContext.User (claims do
/// JWT) ou de HttpContext.Items (resolvido pelo middleware a partir do
/// header X-Empresa-Slug, pra requisições ainda sem token — login,
/// registrar, leitura pública da configuração do site). A Application e a
/// Infrastructure só conhecem esta interface — Clean Architecture de
/// novo: quem PRECISA disto não sabe COMO é resolvido.
/// </summary>
public interface ICurrentTenantService
{
    /// <summary>
    /// Id da barbearia (Empresa) da requisição atual — null quando ainda
    /// não foi possível resolver (ex.: header/slug ausente ou barbearia
    /// não encontrada/inativa). Nesse caso os HasQueryFilter não batem
    /// com NENHUMA linha (empresa_id nunca é null nas entidades
    /// multi-tenant) — resultado seguro por padrão: nada é exposto.
    /// </summary>
    long? EmpresaId { get; }

    /// <summary>
    /// True só pro SuperAdmin (dono da plataforma) logado via
    /// /api/auth/login-admin — ver TipoUsuario.SuperAdmin. Usado pra
    /// ignorar os filtros por barbearia em telas que o próprio
    /// SuperAdmin usa (ex.: EmpresaService, que gerencia a tabela de
    /// barbearias em si).
    /// </summary>
    bool EhSuperAdmin { get; }
}
