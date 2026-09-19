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
    private readonly IUnitOfWork _uow;

    public AutenticacaoService(
        IUsuarioRepository usuarios,
        IClienteRepository clientes,
        IPasswordHasher hasher,
        IJwtTokenGenerator tokenGenerator,
        IUnitOfWork uow)
    {
        _usuarios = usuarios;
        _clientes = clientes;
        _hasher = hasher;
        _tokenGenerator = tokenGenerator;
        _uow = uow;
    }

    public async Task<AuthResponse> FnRegistrarAsync(RegistrarRequest request, CancellationToken ct = default)
    {
        if (await _usuarios.FnObterPorEmailAsync(request.Email, ct) is not null)
            throw new DomainException($"Já existe uma conta com o e-mail {request.Email}.");

        // Diferente do cadastro manual pelo Admin (UsuarioService.FnCriarAsync),
        // aqui telefone é obrigatório: é a própria pessoa se cadastrando,
        // então não tem motivo pra faltar esse contato (ver comentário em
        // RegistrarRequest e em Usuario.FnCriar).
        if (string.IsNullOrWhiteSpace(request.Telefone))
            throw new DomainException("Telefone é obrigatório.");

        // Bootstrap: a PRIMEIRA conta criada no sistema vira Admin
        // automaticamente (precisa de alguém pra administrar desde o
        // início). Todo autocadastro seguinte nasce Comum — quem quiser
        // virar Barbeiro ou Admin depois precisa ser promovido por um
        // Admin já existente (POST /api/usuarios, ou direto no banco).
        var jaExisteUsuario = await _usuarios.FnExisteAlgumAsync(ct);
        var tipo = jaExisteUsuario ? TipoUsuario.Comum : TipoUsuario.Admin;

        var hash = _hasher.FnHash(request.Senha);
        var usuario = Usuario.FnCriar(request.NomeCompleto, request.Email, hash, tipo, request.Telefone);

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
        if (usuario is null || !_hasher.FnVerificar(request.Senha, usuario.SenhaHash))
            throw new DomainException("E-mail ou senha inválidos.");

        if (usuario.Status != StatusRegistro.Ativo)
            throw new DomainException("Esta conta está inativa ou bloqueada. Fale com o administrador.");

        return await FnGerarRespostaAsync(usuario, ct);
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
