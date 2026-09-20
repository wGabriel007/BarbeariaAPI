using Barbearia.Application.Abstracoes;
using Barbearia.Application.Comum;
using Barbearia.Application.Dtos.Clientes;
using Barbearia.Domain.Comum;
using Barbearia.Domain.Entidades;
using Barbearia.Domain.Enumeracoes;

namespace Barbearia.Application.Servicos;

public class ClienteService
{
    private readonly IClienteRepository _repositorio;
    private readonly IUsuarioRepository _usuarios;
    private readonly IBarbeiroRepository _barbeiros;
    private readonly IAgendamentoRepository _agendamentos;
    private readonly IUnitOfWork _uow;

    public ClienteService(
        IClienteRepository repositorio,
        IUsuarioRepository usuarios,
        IBarbeiroRepository barbeiros,
        IAgendamentoRepository agendamentos,
        IUnitOfWork uow)
    {
        _repositorio = repositorio;
        _usuarios = usuarios;
        _barbeiros = barbeiros;
        _agendamentos = agendamentos;
        _uow = uow;
    }

    /// <summary>
    /// Vincula um Usuario Comum já existente como Cliente — não existe
    /// mais "criar um cliente do zero" digitando os dados: o Admin
    /// escolhe alguém que já se cadastrou no sistema. Nome/e-mail vêm do
    /// próprio Usuario; telefone/CPF/data de nascimento ficam pra
    /// completar depois (AtualizarClienteRequest), já que aqui a gente
    /// só sabe o que a conta de login já tinha.
    /// </summary>
    public async Task<ClienteResponse> FnPromoverAsync(PromoverClienteRequest request, CancellationToken ct = default)
    {
        var usuario = await _usuarios.FnObterPorIdAsync(request.UsuarioId, ct)
            ?? throw NotFoundException.FnPara("Usuario", request.UsuarioId);

        if (usuario.Tipo != TipoUsuario.Comum)
            throw new DomainException("Só é possível vincular um usuário do tipo Comum como cliente.");

        if (await _repositorio.FnObterPorUsuarioIdAsync(request.UsuarioId, ct) is not null)
            throw new DomainException("Este usuário já tem um cadastro de cliente.");

        if (await _barbeiros.FnExistePorUsuarioIdAsync(request.UsuarioId, ct))
            throw new DomainException("Este usuário já é um barbeiro.");

        var cliente = Cliente.FnCriar(usuario.NomeCompleto, telefone: null, email: usuario.Email, usuarioId: usuario.Id);
        cliente.FnAtribuirEmpresa(usuario.EmpresaId!.Value);

        await _repositorio.FnAdicionarAsync(cliente, ct);
        await _uow.FnSalvarAsync(ct);

        return FnMapear(cliente);
    }

    public async Task<ClienteResponse> FnObterPorIdAsync(long id, CancellationToken ct = default)
    {
        var cliente = await _repositorio.FnObterPorIdAsync(id, ct)
            ?? throw NotFoundException.FnPara("Cliente", id);

        return FnMapear(cliente);
    }

    public async Task<List<ClienteResponse>> FnListarAsync(CancellationToken ct = default)
    {
        var clientes = await _repositorio.FnListarAsync(ct);
        return clientes.Select(FnMapear).ToList();
    }

    /// <summary>
    /// "Cartão do cliente" pedido a partir da aba Usuários — por isso
    /// busca pelo UsuarioId (o que aquela tela tem à mão), não pelo Id do
    /// próprio Cliente. 404 se este usuário não tiver nenhum cadastro de
    /// cliente vinculado (usuário "Comum" puro, nunca promovido — ver
    /// FnPromoverAsync). As estatísticas são todas calculadas na hora a
    /// partir do histórico de agendamentos — nada fica salvo em coluna
    /// própria, igual o Ranking (ver RankingService).
    /// </summary>
    public async Task<ClienteDetalheResponse> FnObterDetalhePorUsuarioIdAsync(long usuarioId, CancellationToken ct = default)
    {
        var cliente = await _repositorio.FnObterPorUsuarioIdAsync(usuarioId, ct)
            ?? throw new NotFoundException("Este usuário não tem cadastro de cliente vinculado.");

        var historico = await _agendamentos.FnListarPorClienteAsync(cliente.Id, ct);
        var concluidos = historico.Where(a => a.Status == StatusAgendamento.Concluido).ToList();

        var agora = DateTimeOffset.UtcNow;
        var inicioMes = new DateTimeOffset(agora.Year, agora.Month, 1, 0, 0, 0, TimeSpan.Zero);
        var cortesEsteMes = concluidos.Count(a => a.Inicio >= inicioMes);

        return new ClienteDetalheResponse(
            FnMapear(cliente),
            TotalCortes: concluidos.Count,
            CortesEsteMes: cortesEsteMes,
            ValorTotalGasto: concluidos.Sum(a => a.PrecoCobrado),
            UltimoCorte: concluidos.Count > 0 ? concluidos.Max(a => a.Inicio) : null);
    }

    public async Task<ClienteResponse> FnAtualizarAsync(long id, AtualizarClienteRequest request, CancellationToken ct = default)
    {
        var cliente = await _repositorio.FnObterPorIdAsync(id, ct)
            ?? throw NotFoundException.FnPara("Cliente", id);

        cliente.FnAtualizarDados(request.NomeCompleto, request.Telefone, request.Email, request.Observacoes);
        await _uow.FnSalvarAsync(ct);

        return FnMapear(cliente);
    }

    public async Task FnInativarAsync(long id, CancellationToken ct = default)
    {
        var cliente = await _repositorio.FnObterPorIdAsync(id, ct)
            ?? throw NotFoundException.FnPara("Cliente", id);

        cliente.FnInativar();
        await _uow.FnSalvarAsync(ct);
    }

    public async Task FnBloquearAsync(long id, CancellationToken ct = default)
    {
        var cliente = await _repositorio.FnObterPorIdAsync(id, ct)
            ?? throw NotFoundException.FnPara("Cliente", id);

        cliente.FnBloquear();
        await _uow.FnSalvarAsync(ct);
    }

    public async Task FnAtivarAsync(long id, CancellationToken ct = default)
    {
        var cliente = await _repositorio.FnObterPorIdAsync(id, ct)
            ?? throw NotFoundException.FnPara("Cliente", id);

        cliente.FnAtivar();
        await _uow.FnSalvarAsync(ct);
    }

    private static ClienteResponse FnMapear(Cliente c) => new(
        c.Id, c.NomeCompleto, c.Telefone, c.Email, c.Cpf, c.DataNascimento, c.Observacoes, c.Status, c.CriadoEm, c.UsuarioId);
}
