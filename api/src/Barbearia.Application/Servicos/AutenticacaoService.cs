using Barbearia.Application.Abstracoes;
using Barbearia.Application.Comum;
using Barbearia.Application.Dtos.Autenticacao;
using Barbearia.Application.Dtos.Usuarios;
using Barbearia.Domain.Comum;
using Barbearia.Domain.Entidades;
using Barbearia.Domain.Enumeracoes;

namespace Barbearia.Application.Servicos;

public class AutenticacaoService
{
    private readonly IUsuarioRepository _usuarios;
    private readonly IClienteRepository _clientes;
    private readonly IPasswordHasher _hasher;
    private readonly IJwtTokenGenerator _tokenGenerator;
    private readonly ICurrentTenantService _tenant;
    private readonly IUnitOfWork _uow;

    public AutenticacaoService(
        IUsuarioRepository usuarios,
        IClienteRepository clientes,
        IPasswordHasher hasher,
        IJwtTokenGenerator tokenGenerator,
        ICurrentTenantService tenant,
        IUnitOfWork uow)
    {
        _usuarios = usuarios;
        _clientes = clientes;
        _hasher = hasher;
        _tokenGenerator = tokenGenerator;
        _tenant = tenant;
        _uow = uow;
    }

    public async Task<AuthResponse> FnRegistrarAsync(RegistrarRequest request, CancellationToken ct = default)
    {
        // EmpresaId já foi resolvido pelo EmpresaResolverMiddleware a
        // partir do header X-Empresa-Slug (o front manda o "apelido" da
        // barbearia — a parte /barbearia-do-joao do link — em toda
        // chamada). Sem isso, não tem como saber PRA QUAL barbearia esta
        // conta está nascendo.
        var empresaId = _tenant.EmpresaId
            ?? throw new DomainException("Barbearia não encontrada. Verifique o link usado para acessar o sistema.");

        if (await _usuarios.FnObterPorEmailAsync(request.Email, ct) is not null)
            throw new DomainException($"Já existe uma conta com o e-mail {request.Email}.");

        // Diferente do cadastro manual pelo Admin (UsuarioService.FnCriarAsync),
        // aqui telefone é obrigatório: é a própria pessoa se cadastrando,
        // então não tem motivo pra faltar esse contato (ver comentário em
        // RegistrarRequest e em Usuario.FnCriar).
        if (string.IsNullOrWhiteSpace(request.Telefone))
            throw new DomainException("Telefone é obrigatório.");

        // Bootstrap: a PRIMEIRA conta criada em CADA barbearia vira Admin
        // automaticamente (precisa de alguém pra administrar desde o
        // início) — FnExisteAlgumAsync já passa pelo filtro por EmpresaId
        // (ver BarbeariaDbContext), então isto conta só usuários DESTA
        // barbearia, não da plataforma inteira. Todo autocadastro
        // seguinte NA MESMA barbearia nasce Comum — quem quiser virar
        // Barbeiro ou Admin depois precisa ser promovido por um Admin já
        // existente (POST /api/usuarios, ou direto no banco). Normalmente,
        // porém, a primeira conta Admin de uma barbearia nasce junto com
        // ela (ver EmpresaService.FnCriarAsync) — este caminho aqui cobre
        // o caso de uma barbearia sem NENHUM usuário ainda.
        var jaExisteUsuario = await _usuarios.FnExisteAlgumAsync(ct);
        var tipo = jaExisteUsuario ? TipoUsuario.Comum : TipoUsuario.Admin;

        var hash = _hasher.FnHash(request.Senha);
        var usuario = Usuario.FnCriar(request.NomeCompleto, request.Email, hash, tipo, request.Telefone);
        usuario.FnAtribuirEmpresa(empresaId);

        await _usuarios.FnAdicionarAsync(usuario, ct);
        await _uow.FnSalvarAsync(ct);

        // Depois de criar a conta, a pessoa já sai autenticada — evita
        // ela ter que preencher a tela de login imediatamente depois de
        // se cadastrar (uma etapa em vez de duas).
        return await FnGerarRespostaAsync(usuario, ct);
    }

    public async Task<AuthResponse> FnLoginAsync(LoginRequest request, CancellationToken ct = default)
    {
        var usuario = await _usuarios.FnObterPorEmailAsync(request.Email, ct);

        // Mensagem de propósito genérica ("e-mail OU senha inválidos"),
        // sem dizer qual dos dois está errado — evita que alguém use a
        // tela de login pra descobrir quais e-mails têm conta cadastrada
        // (enumeração de usuários).
        //
        // "usuario.Tipo == SuperAdmin" entra aqui de propósito, por
        // defesa em profundidade: sem X-Empresa-Slug (ou com um slug que
        // não resolve pra nenhuma Empresa), _tenant.EmpresaId fica null —
        // e o HasQueryFilter de Usuario (ver BarbeariaDbContext) compara
        // com "==", que em C#/EF Core é null-safe (u.EmpresaId == null
        // vira "u.empresa_id IS NULL" no SQL gerado). Ou seja, SEM esta
        // linha, uma requisição a /auth/login sem o header certo cairia
        // justamente na única linha com empresa_id NULL — o SuperAdmin —
        // deixando esta rota "normal" logar como SuperAdmin se alguém
        // soubesse o e-mail/senha dele. Esta rota nunca deve devolver um
        // SuperAdmin — esse login tem sua própria rota (ver
        // FnLoginSuperAdminAsync mais abaixo).
        if (usuario is null || usuario.Tipo == TipoUsuario.SuperAdmin || !_hasher.FnVerificar(request.Senha, usuario.SenhaHash))
            throw new DomainException("E-mail ou senha inválidos.");

        if (usuario.Status != StatusRegistro.Ativo)
            throw new DomainException("Esta conta está inativa ou bloqueada. Fale com o administrador.");

        return await FnGerarRespostaAsync(usuario, ct);
    }

    /// <summary>
    /// POST /api/auth/login-admin — login do SuperAdmin (dono da
    /// plataforma), separado do login normal de propósito: não passa
    /// pelo header X-Empresa-Slug nem pelo filtro por barbearia (o
    /// SuperAdmin não pertence a nenhuma — ver
    /// IUsuarioRepository.FnObterSuperAdminPorEmailAsync, que já garante
    /// que só um usuário Tipo=SuperAdmin pode entrar por aqui).
    /// </summary>
    public async Task<AuthResponse> FnLoginSuperAdminAsync(LoginRequest request, CancellationToken ct = default)
    {
        var usuario = await _usuarios.FnObterSuperAdminPorEmailAsync(request.Email, ct);

        if (usuario is null || !_hasher.FnVerificar(request.Senha, usuario.SenhaHash))
            throw new DomainException("E-mail ou senha inválidos.");

        if (usuario.Status != StatusRegistro.Ativo)
            throw new DomainException("Esta conta está inativa ou bloqueada.");

        var token = _tokenGenerator.FnGerarToken(usuario);
        var usuarioResponse = new UsuarioResponse(
            usuario.Id, usuario.NomeCompleto, usuario.Email, usuario.Telefone, usuario.FotoUrl, usuario.Tipo, usuario.Status, ehCliente: false);
        return new AuthResponse(token, usuarioResponse);
    }

    /// <summary>EhCliente vem de uma consulta à parte porque não é um dado do próprio Usuario (ver comentário em UsuarioResponse) — só reflete se existe um Cliente vinculado a ele.</summary>
    private async Task<AuthResponse> FnGerarRespostaAsync(Usuario usuario, CancellationToken ct)
    {
        var token = _tokenGenerator.FnGerarToken(usuario);
        var ehCliente = await _clientes.FnObterPorUsuarioIdAsync(usuario.Id, ct) is not null;
        var usuarioResponse = new UsuarioResponse(
            usuario.Id, usuario.NomeCompleto, usuario.Email, usuario.Telefone, usuario.FotoUrl, usuario.Tipo, usuario.Status, ehCliente);
        return new AuthResponse(token, usuarioResponse);
    }
}
