using Barbearia.Application.Abstracoes;
using Barbearia.Application.Comum;
using Barbearia.Application.Dtos.Usuarios;
using Barbearia.Domain.Comum;
using Barbearia.Domain.Entidades;
using Barbearia.Domain.Enumeracoes;

namespace Barbearia.Application.Servicos;

/// <summary>
/// Nesta fase, só cadastro de usuário — sem login/JWT ainda (fica pra
/// uma próxima etapa, junto com autenticação/autorização de verdade).
/// Isso já é suficiente pra criar o usuário base que vira um Barbeiro
/// depois (ver BarbeiroService).
/// </summary>
public class UsuarioService
{
    private readonly IUsuarioRepository _repositorio;
    private readonly IPasswordHasher _hasher;
    private readonly IArmazenamentoArquivos _armazenamento;
    private readonly IClienteRepository _clientes;
    private readonly IUnitOfWork _uow;

    public UsuarioService(
        IUsuarioRepository repositorio,
        IPasswordHasher hasher,
        IArmazenamentoArquivos armazenamento,
        IClienteRepository clientes,
        IUnitOfWork uow)
    {
        _repositorio = repositorio;
        _hasher = hasher;
        _armazenamento = armazenamento;
        _clientes = clientes;
        _uow = uow;
    }

    public async Task<UsuarioResponse> FnCriarAsync(CriarUsuarioRequest request, CancellationToken ct = default)
    {
        if (await _repositorio.FnObterPorEmailAsync(request.Email, ct) is not null)
            throw new DomainException($"Já existe um usuário com o e-mail {request.Email}.");

        // Não dá pra criar uma conta já nascendo Barbeiro — esse Tipo só
        // existe depois de uma promoção (ver Usuario.FnPromoverParaBarbeiro
        // / BarbeiroService.FnPromoverAsync), que também cuida de desfazer
        // um eventual vínculo de Cliente. Permitir isso aqui abriria um
        // atalho que deixa esses dois passos dessincronizados.
        if (request.Tipo == TipoUsuario.Barbeiro)
            throw new DomainException("Não é possível criar uma conta já como Barbeiro — promova um usuário Comum existente na tela Barbeiros.");

        var hash = _hasher.FnHash(request.Senha);
        var usuario = Usuario.FnCriar(request.NomeCompleto, request.Email, hash, request.Tipo);

        await _repositorio.FnAdicionarAsync(usuario, ct);
        await _uow.FnSalvarAsync(ct);

        // Acabou de nascer agora mesmo — não tem como já ter um Cliente
        // vinculado, então nem vale a pena consultar.
        return FnMapear(usuario, ehCliente: false);
    }

    public async Task<List<UsuarioResponse>> FnListarAsync(CancellationToken ct = default)
    {
        var usuarios = await _repositorio.FnListarAsync(ct);

        // Um Usuario Comum vinculado a um Cliente (ver ClienteService.
        // FnPromoverAsync) continua Tipo=Comum de verdade — "Cliente" não é
        // um papel de acesso. Isso aqui só monta o conjunto de ids pra
        // trocar o RÓTULO exibido na tela de Usuários (ver UsuarioResponse),
        // numa única consulta em vez de perguntar cliente por cliente.
        var clientes = await _clientes.FnListarAsync(ct);
        var usuarioIdsComCliente = clientes
            .Where(c => c.UsuarioId.HasValue)
            .Select(c => c.UsuarioId!.Value)
            .ToHashSet();

        return usuarios.Select(u => FnMapear(u, usuarioIdsComCliente.Contains(u.Id))).ToList();
    }

    // ---------------------------------------------------------------------
    // Aba "Meu perfil" (self-service — ver PerfilController): qualquer
    // usuário logado, de qualquer Tipo, vê/edita os PRÓPRIOS dados. Nunca
    // recebe um 'id' escolhido por quem chama — sempre o usuarioId do
    // próprio token (ver User.FnObterUsuarioId() no Controller).
    // ---------------------------------------------------------------------

    public async Task<PerfilResponse> FnObterPerfilAsync(long usuarioId, CancellationToken ct = default)
    {
        var usuario = await _repositorio.FnObterPorIdAsync(usuarioId, ct)
            ?? throw NotFoundException.FnPara("Usuario", usuarioId);

        return await FnMapearPerfilAsync(usuario, ct);
    }

    public async Task<PerfilResponse> FnAtualizarPerfilAsync(long usuarioId, AtualizarPerfilRequest request, CancellationToken ct = default)
    {
        var usuario = await _repositorio.FnObterPorIdAsync(usuarioId, ct)
            ?? throw NotFoundException.FnPara("Usuario", usuarioId);

        if (string.IsNullOrWhiteSpace(request.Telefone))
            throw new DomainException("Telefone é obrigatório.");

        var outroComEsseEmail = await _repositorio.FnObterPorEmailAsync(request.Email, ct);
        if (outroComEsseEmail is not null && outroComEsseEmail.Id != usuarioId)
            throw new DomainException($"Já existe um usuário com o e-mail {request.Email}.");

        usuario.FnAtualizarDados(request.NomeCompleto, request.Email, request.Telefone);
        await _uow.FnSalvarAsync(ct);

        return await FnMapearPerfilAsync(usuario, ct);
    }

    public async Task FnAlterarSenhaAsync(long usuarioId, AlterarSenhaRequest request, CancellationToken ct = default)
    {
        var usuario = await _repositorio.FnObterPorIdAsync(usuarioId, ct)
            ?? throw NotFoundException.FnPara("Usuario", usuarioId);

        if (!_hasher.FnVerificar(request.SenhaAtual, usuario.SenhaHash))
            throw new DomainException("Senha atual incorreta.");

        usuario.FnAtualizarSenha(_hasher.FnHash(request.NovaSenha));
        await _uow.FnSalvarAsync(ct);
    }

    public async Task<PerfilResponse> FnAtualizarFotoAsync(long usuarioId, Stream conteudo, string extensao, CancellationToken ct = default)
    {
        var usuario = await _repositorio.FnObterPorIdAsync(usuarioId, ct)
            ?? throw NotFoundException.FnPara("Usuario", usuarioId);

        var urlAntiga = usuario.FotoUrl;

        var novaUrl = await _armazenamento.FnSalvarAsync(conteudo, extensao, "fotos-usuarios", ct);
        usuario.FnDefinirFoto(novaUrl);
        await _uow.FnSalvarAsync(ct);

        _armazenamento.FnRemover(urlAntiga);

        return await FnMapearPerfilAsync(usuario, ct);
    }

    public async Task<PerfilResponse> FnRemoverFotoAsync(long usuarioId, CancellationToken ct = default)
    {
        var usuario = await _repositorio.FnObterPorIdAsync(usuarioId, ct)
            ?? throw NotFoundException.FnPara("Usuario", usuarioId);

        _armazenamento.FnRemover(usuario.FotoUrl);
        usuario.FnRemoverFoto();
        await _uow.FnSalvarAsync(ct);

        return await FnMapearPerfilAsync(usuario, ct);
    }

    private static UsuarioResponse FnMapear(Usuario u, bool ehCliente) =>
        new(u.Id, u.NomeCompleto, u.Email, u.Telefone, u.FotoUrl, u.Tipo, u.Status, ehCliente);

    private async Task<PerfilResponse> FnMapearPerfilAsync(Usuario u, CancellationToken ct)
    {
        var ehCliente = await _clientes.FnObterPorUsuarioIdAsync(u.Id, ct) is not null;
        return new(u.Id, u.NomeCompleto, u.Email, u.Telefone, u.FotoUrl, u.Tipo, u.Status, ehCliente);
    }
}
