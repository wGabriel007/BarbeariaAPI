using Barbearia.Application.Abstracoes;
using Barbearia.Application.Comum;
using Barbearia.Application.Dtos.Barbeiros;
using Barbearia.Domain.Comum;
using Barbearia.Domain.Entidades;
using Barbearia.Domain.Enumeracoes;

namespace Barbearia.Application.Servicos;

public class BarbeiroService
{
    private readonly IBarbeiroRepository _barbeiros;
    private readonly IUsuarioRepository _usuarios;
    private readonly IClienteRepository _clientes;
    private readonly IUnitOfWork _uow;

    public BarbeiroService(IBarbeiroRepository barbeiros, IUsuarioRepository usuarios, IClienteRepository clientes, IUnitOfWork uow)
    {
        _barbeiros = barbeiros;
        _usuarios = usuarios;
        _clientes = clientes;
        _uow = uow;
    }

    /// <summary>
    /// Promove um usuário Comum já existente a Barbeiro — não existe mais
    /// "criar um barbeiro do zero". O Admin escolhe um usuário Comum (que
    /// pode ou não já ser Cliente — ver FnObterOuCriarClienteDoUsuarioAsync
    /// em AgendamentoService) e esta operação faz tudo de uma vez: muda
    /// o Tipo dele pra Barbeiro (ganhando acesso às abas de staff), desfaz
    /// o vínculo de Cliente se houver um (decisão do Gabriel: a mesma
    /// pessoa não é Cliente e Barbeiro ao mesmo tempo) e cria o cadastro
    /// de Barbeiro em si.
    /// </summary>
    public async Task<BarbeiroResponse> FnPromoverAsync(PromoverBarbeiroRequest request, CancellationToken ct = default)
    {
        var usuario = await _usuarios.FnObterPorIdAsync(request.UsuarioId, ct)
            ?? throw NotFoundException.FnPara("Usuario", request.UsuarioId);

        if (await _barbeiros.FnExistePorUsuarioIdAsync(request.UsuarioId, ct))
            throw new DomainException("Este usuário já tem um cadastro de barbeiro.");

        // Lança DomainException se o usuário não for Comum (ver
        // Usuario.FnPromoverParaBarbeiro) — cobre tanto "já é Admin" quanto
        // "já é Barbeiro" (esse segundo caso também já teria caído no
        // FnExistePorUsuarioIdAsync acima, mas a checagem de Tipo é quem dá
        // a mensagem certa se um dia os dois cadastros ficarem dessincronizados).
        usuario.FnPromoverParaBarbeiro();

        // Se esse usuário já era Cliente (ex.: já tinha pedido um corte
        // pelo próprio app antes de virar barbeiro), desfaz esse vínculo —
        // uma pessoa só tem um papel por vez neste sistema.
        var clienteVinculado = await _clientes.FnObterPorUsuarioIdAsync(request.UsuarioId, ct);
        if (clienteVinculado is not null)
            _clientes.FnRemover(clienteVinculado);

        var barbeiro = Barbeiro.FnCriar(request.UsuarioId, request.Telefone);
        barbeiro.FnAtribuirEmpresa(usuario.EmpresaId!.Value);

        await _barbeiros.FnAdicionarAsync(barbeiro, ct);
        await _uow.FnSalvarAsync(ct);

        // Aqui (e só aqui) 'barbeiro' acabou de ser criado em memória —
        // não veio de uma consulta com .Include(Usuario), então usamos o
        // 'usuario' já buscado no início do método em vez de
        // barbeiro.Usuario (que estaria null).
        return FnMapear(barbeiro, usuario);
    }

    public async Task<BarbeiroResponse> FnObterPorIdAsync(long id, CancellationToken ct = default)
    {
        var barbeiro = await _barbeiros.FnObterComHorariosAsync(id, ct)
            ?? throw NotFoundException.FnPara("Barbeiro", id);

        return FnMapear(barbeiro, barbeiro.Usuario!);
    }

    public async Task<List<BarbeiroResponse>> FnListarAsync(CancellationToken ct = default)
    {
        var barbeiros = await _barbeiros.FnListarAsync(ct);
        return barbeiros.Select(b => FnMapear(b, b.Usuario!)).ToList();
    }

    public async Task<BarbeiroResponse> FnAdicionarHorarioAsync(long barbeiroId, AdicionarHorarioRequest request, CancellationToken ct = default)
    {
        var barbeiro = await _barbeiros.FnObterComHorariosAsync(barbeiroId, ct)
            ?? throw NotFoundException.FnPara("Barbeiro", barbeiroId);

        // Regra de negócio que o Domain (HorarioTrabalho) não consegue
        // validar sozinho, porque exige olhar os OUTROS horários do
        // mesmo barbeiro/dia — isso só a Application, com acesso à
        // coleção já carregada, consegue checar.
        var sobrepoe = barbeiro.Horarios.Any(h =>
            h.DiaSemana == request.DiaSemana &&
            request.HoraInicio < h.HoraFim &&
            request.HoraFim > h.HoraInicio);

        if (sobrepoe)
            throw new DomainException("Esse horário se sobrepõe a outro horário já cadastrado para este dia.");

        barbeiro.FnAdicionarHorario(request.DiaSemana, request.HoraInicio, request.HoraFim);
        await _uow.FnSalvarAsync(ct);

        return FnMapear(barbeiro, barbeiro.Usuario!);
    }

    public async Task<BarbeiroResponse> FnRemoverHorarioAsync(long barbeiroId, long horarioId, CancellationToken ct = default)
    {
        var barbeiro = await _barbeiros.FnObterComHorariosAsync(barbeiroId, ct)
            ?? throw NotFoundException.FnPara("Barbeiro", barbeiroId);

        barbeiro.FnRemoverHorario(horarioId);
        await _uow.FnSalvarAsync(ct);

        return FnMapear(barbeiro, barbeiro.Usuario!);
    }

    public async Task FnInativarAsync(long id, CancellationToken ct = default)
    {
        var barbeiro = await _barbeiros.FnObterPorIdAsync(id, ct)
            ?? throw NotFoundException.FnPara("Barbeiro", id);

        barbeiro.FnInativar();
        await _uow.FnSalvarAsync(ct);
    }

    public async Task FnAtivarAsync(long id, CancellationToken ct = default)
    {
        var barbeiro = await _barbeiros.FnObterPorIdAsync(id, ct)
            ?? throw NotFoundException.FnPara("Barbeiro", id);

        barbeiro.FnAtivar();
        await _uow.FnSalvarAsync(ct);
    }

    /// <summary>
    /// Liga/desliga a ausência ("de folga hoje") do barbeiro — self-service:
    /// um Barbeiro só pode alterar a PRÓPRIA (usuarioIdChamador precisa
    /// bater com o UsuarioId do cadastro), um Admin pode alterar a de
    /// qualquer um (ex.: o próprio barbeiro esqueceu de desmarcar).
    /// </summary>
    public async Task FnDefinirAusenciaAsync(long id, long usuarioIdChamador, bool ehAdmin, bool ausente, CancellationToken ct = default)
    {
        var barbeiro = await _barbeiros.FnObterPorIdAsync(id, ct)
            ?? throw NotFoundException.FnPara("Barbeiro", id);

        if (!ehAdmin && barbeiro.UsuarioId != usuarioIdChamador)
            throw new DomainException("Você só pode alterar sua própria ausência.");

        if (ausente)
            barbeiro.FnMarcarAusente();
        else
            barbeiro.FnMarcarPresente();

        await _uow.FnSalvarAsync(ct);
    }

    /// <summary>
    /// Bio/Especialidade — apresentação profissional self-service (aba
    /// "Sobre a barbearia", ver paginas/SobreABarbearia.jsx). Mesma regra
    /// de dono do FnDefinirAusenciaAsync: um Barbeiro só altera o PRÓPRIO
    /// perfil (usuarioIdChamador precisa bater com o UsuarioId do
    /// cadastro), um Admin altera o de qualquer um.
    /// </summary>
    public async Task<BarbeiroResponse> FnAtualizarPerfilAsync(
        long id, long usuarioIdChamador, bool ehAdmin, AtualizarPerfilBarbeiroRequest request, CancellationToken ct = default)
    {
        var barbeiro = await _barbeiros.FnObterComHorariosAsync(id, ct)
            ?? throw NotFoundException.FnPara("Barbeiro", id);

        if (!ehAdmin && barbeiro.UsuarioId != usuarioIdChamador)
            throw new DomainException("Você só pode alterar o próprio perfil.");

        barbeiro.FnAtualizarPerfil(request.Bio, request.Especialidade);
        await _uow.FnSalvarAsync(ct);

        return FnMapear(barbeiro, barbeiro.Usuario!);
    }

    // Recebe 'usuario' separado (em vez de só ler barbeiro.Usuario) pelo
    // motivo explicado em FnPromoverAsync — nos outros métodos, os dois
    // parâmetros apontam pro mesmo Usuario, só que um veio via EF
    // (navegação já carregada) e o outro foi buscado direto.
    private static BarbeiroResponse FnMapear(Barbeiro b, Usuario usuario) => new(
        b.Id,
        b.UsuarioId,
        usuario.NomeCompleto,
        usuario.Email,
        b.Telefone,
        usuario.FotoUrl,
        b.Status,
        b.Ausente,
        b.Bio,
        b.Especialidade,
        b.Horarios.Select(h => new HorarioResponse(h.Id, h.DiaSemana, h.HoraInicio, h.HoraFim)).ToList());
}
