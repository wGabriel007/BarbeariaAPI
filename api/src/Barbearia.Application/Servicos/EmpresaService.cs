using Barbearia.Application.Abstracoes;
using Barbearia.Application.Comum;
using Barbearia.Application.Dtos.Empresas;
using Barbearia.Domain.Comum;
using Barbearia.Domain.Entidades;
using Barbearia.Domain.Enumeracoes;

namespace Barbearia.Application.Servicos;

/// <summary>
/// Só o SuperAdmin usa isto (ver EmpresasController, [Authorize(Roles =
/// "SuperAdmin")]) — cadastra e ativa/desativa barbearias na plataforma.
/// Nada aqui aparece pras telas de gestão de UMA barbearia (Admin/Barbeiro
/// de uma Empresa não sabem que outras existem).
/// </summary>
public class EmpresaService
{
    private readonly IEmpresaRepository _empresas;
    private readonly IUsuarioRepository _usuarios;
    private readonly IConfiguracaoSiteRepository _configuracoesSite;
    private readonly IPasswordHasher _hasher;
    private readonly IUnitOfWork _uow;

    public EmpresaService(
        IEmpresaRepository empresas,
        IUsuarioRepository usuarios,
        IConfiguracaoSiteRepository configuracoesSite,
        IPasswordHasher hasher,
        IUnitOfWork uow)
    {
        _empresas = empresas;
        _usuarios = usuarios;
        _configuracoesSite = configuracoesSite;
        _hasher = hasher;
        _uow = uow;
    }

    /// <summary>
    /// Cria a barbearia inteira de um golpe só: a Empresa, a primeira
    /// conta Admin (com a senha que o SuperAdmin escolheu — repassada ao
    /// dono por fora, ex.: WhatsApp) e a configuração inicial do site.
    /// Tudo numa transação só (um único FnSalvarAsync no final) — se algo
    /// falhar no meio, nada fica pela metade.
    /// </summary>
    public async Task<EmpresaCriadaResponse> FnCriarAsync(CriarEmpresaRequest request, CancellationToken ct = default)
    {
        if (await _empresas.FnObterPorSlugAsync(request.Slug.Trim().ToLowerInvariant(), ct) is not null)
            throw new DomainException($"Já existe uma barbearia com o apelido '{request.Slug}'.");

        var empresa = Empresa.FnCriar(request.Nome, request.Slug);
        await _empresas.FnAdicionarAsync(empresa, ct);
        await _uow.FnSalvarAsync(ct); // precisa do Id gerado da Empresa antes de ligar Usuario/ConfiguracaoSite a ela

        var hash = _hasher.FnHash(request.SenhaAdmin);
        var admin = Usuario.FnCriar(request.NomeCompletoAdmin, request.EmailAdmin, hash, TipoUsuario.Admin);
        admin.FnAtribuirEmpresa(empresa.Id);
        await _usuarios.FnAdicionarAsync(admin, ct);

        var configuracao = ConfiguracaoSite.FnCriarPadrao(empresa.Id, request.Nome);
        await _configuracoesSite.FnAdicionarAsync(configuracao, ct);

        await _uow.FnSalvarAsync(ct);

        return new EmpresaCriadaResponse(FnMapear(empresa), request.EmailAdmin, request.SenhaAdmin);
    }

    public async Task<List<EmpresaResponse>> FnListarAsync(CancellationToken ct = default)
    {
        var empresas = await _empresas.FnListarAsync(ct);
        return empresas.Select(FnMapear).ToList();
    }

    public async Task FnAtivarAsync(long id, CancellationToken ct = default)
    {
        var empresa = await _empresas.FnObterPorIdAsync(id, ct)
            ?? throw NotFoundException.FnPara("Empresa", id);

        empresa.FnAtivar();
        await _uow.FnSalvarAsync(ct);
    }

    public async Task FnInativarAsync(long id, CancellationToken ct = default)
    {
        var empresa = await _empresas.FnObterPorIdAsync(id, ct)
            ?? throw NotFoundException.FnPara("Empresa", id);

        empresa.FnInativar();
        await _uow.FnSalvarAsync(ct);
    }

    private static EmpresaResponse FnMapear(Empresa e) => new(e.Id, e.Nome, e.Slug, e.Status, e.CriadoEm);
}
