using Barbearia.Api.Extensoes;
using Barbearia.Application.Dtos.Agendamentos;
using Barbearia.Application.Servicos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Barbearia.Api.Controladores;

/// <summary>
/// Divisão de acesso deste Controller (a única do sistema que mistura os
/// dois grupos): GETs de detalhe/histórico, FnSolicitar e FnCancelar ficam
/// abertos pra qualquer usuário autenticado e ativo, INCLUSIVE Comum — é
/// o que permite ele pedir/cancelar o próprio horário. Já as transições
/// que só fazem sentido do lado de quem atende (FnConfirmar, FnRejeitar,
/// Iniciar atendimento, FnConcluir, Não compareceu) e a agenda completa de
/// um barbeiro (que expõe dados de outros clientes) exigem Admin/Barbeiro.
/// O resto da Api segue a regra simples "Comum só lê, Admin/Barbeiro
/// gerencia".
/// </summary>
[ApiController]
[Route("api/agendamentos")]
public class AgendamentosController : ControllerBase
{
    private readonly AgendamentoService _service;

    public AgendamentosController(AgendamentoService service)
    {
        _service = service;
    }

    [HttpGet("{id:long}")]
    public async Task<ActionResult<AgendamentoResponse>> FnObterPorId(long id, CancellationToken ct) =>
        Ok(await _service.FnObterPorIdAsync(id, ct));

    /// <summary>
    /// GET /api/agendamentos/por-barbeiro/5?de=2026-09-15T00:00:00-03:00&amp;ate=2026-09-22T00:00:00-03:00
    /// Agenda completa (todos os clientes) de um barbeiro num período —
    /// só Admin/Barbeiro, porque expõe nome/serviço/preço de OUTROS
    /// clientes (a tela de "Agenda" do front, que é staff-only).
    /// </summary>
    [Authorize(Roles = "Admin,Barbeiro")]
    [HttpGet("por-barbeiro/{barbeiroId:long}")]
    public async Task<ActionResult<List<AgendamentoResponse>>> FnListarPorBarbeiroEPeriodo(
        long barbeiroId, [FromQuery] DateTimeOffset de, [FromQuery] DateTimeOffset ate, CancellationToken ct) =>
        Ok(await _service.FnListarPorBarbeiroEPeriodoAsync(barbeiroId, de, ate, ct));

    /// <summary>GET /api/agendamentos/por-cliente/5 — histórico de agendamentos de um cliente. Só Admin/Barbeiro (dado de outro cliente) — o próprio Comum usa GET /meus.</summary>
    [Authorize(Roles = "Admin,Barbeiro")]
    [HttpGet("por-cliente/{clienteId:long}")]
    public async Task<ActionResult<List<AgendamentoResponse>>> FnListarPorCliente(long clienteId, CancellationToken ct) =>
        Ok(await _service.FnListarPorClienteAsync(clienteId, ct));

    /// <summary>
    /// GET /api/agendamentos/meus — os agendamentos do PRÓPRIO usuário
    /// logado (pendente, confirmado, rejeitado, etc.), pela conta dele,
    /// nunca por um Id que o cliente escolhe. É a "aba" que qualquer
    /// usuário Comum vê pra acompanhar suas solicitações.
    /// </summary>
    [HttpGet("meus")]
    public async Task<ActionResult<List<AgendamentoResponse>>> FnListarMeus(CancellationToken ct) =>
        Ok(await _service.FnListarMeusAsync(User.FnObterUsuarioId(), ct));

    /// <summary>
    /// GET /api/agendamentos/pendentes — a "aba de solicitações" do
    /// staff. Um Admin vê as de todos os barbeiros; um Barbeiro (que não
    /// seja também Admin) vê só as PRÓPRIAS, porque é ele quem decide se
    /// aceita o horário na própria agenda.
    /// </summary>
    [Authorize(Roles = "Admin,Barbeiro")]
    [HttpGet("pendentes")]
    public async Task<ActionResult<List<AgendamentoResponse>>> FnListarPendentes(CancellationToken ct) =>
        Ok(await _service.FnListarPendentesAsync(User.FnObterUsuarioId(), User.IsInRole("Admin"), ct));

    /// <summary>
    /// GET /api/agendamentos/minha-fila — a fila de HOJE de quem está
    /// logado. Pra um CLIENTE: pra cada agendamento de hoje já
    /// Confirmado, devolve a fila inteira daquele barbeiro (com
    /// nome/horário de quem está na frente). Pra um BARBEIRO com agenda
    /// própria (mesmo sendo também Admin): devolve a própria fila de hoje
    /// (quem ele vai atender, na ordem). Pra um ADMIN puro (sem cadastro
    /// de Barbeiro/Cliente): devolve a fila de hoje de TODOS os barbeiros
    /// — ver AgendamentoService.FnListarMinhaFilaAsync. Aberto pra
    /// qualquer usuário autenticado, igual GET /meus.
    /// </summary>
    [HttpGet("minha-fila")]
    public async Task<ActionResult<List<FilaDoBarbeiroResponse>>> FnListarMinhaFila(CancellationToken ct) =>
        Ok(await _service.FnListarMinhaFilaAsync(User.FnObterUsuarioId(), User.IsInRole("Admin"), ct));

    /// <summary>
    /// POST /api/agendamentos — cria o agendamento DIRETO (uso do staff:
    /// walk-in, telefone), já como Agendado. Fim e PrecoCobrado NÃO vêm
    /// no request (o Service calcula a partir do Servico) e um conflito
    /// de horário responde 409 (ver ExceptionHandlingMiddleware).
    /// </summary>
    [Authorize(Roles = "Admin,Barbeiro")]
    [HttpPost]
    public async Task<ActionResult<AgendamentoResponse>> FnCriar(CriarAgendamentoRequest request, CancellationToken ct)
    {
        var agendamento = await _service.FnCriarAsync(request, ct);
        return CreatedAtAction(nameof(FnObterPorId), new { id = agendamento.Id }, agendamento);
    }

    /// <summary>
    /// POST /api/agendamentos/solicitar — o CLIENTE (Comum) pedindo um
    /// horário. Nasce Pendente, dentro do expediente cadastrado do
    /// barbeiro (ver AgendamentoService.FnSolicitarAsync) — fora disso, ou
    /// em conflito com outro agendamento/solicitação, a Api responde 400
    /// ou 409, respectivamente.
    /// </summary>
    [HttpPost("solicitar")]
    public async Task<ActionResult<AgendamentoResponse>> FnSolicitar(SolicitarAgendamentoRequest request, CancellationToken ct)
    {
        var agendamento = await _service.FnSolicitarAsync(User.FnObterUsuarioId(), request, ct);
        return CreatedAtAction(nameof(FnObterPorId), new { id = agendamento.Id }, agendamento);
    }

    /// <summary>
    /// Estas rotas mapeiam a máquina de estados de Agendamento (Domain):
    /// Agendado/Pendente -> Confirmado -> EmAtendimento -> Concluido, com
    /// FnRejeitar (só a partir de Pendente), FnCancelar e FnMarcarNaoCompareceu
    /// podendo interromper o fluxo. 'mensagem' é o recado opcional que o
    /// Admin/Barbeiro deixa pro cliente (ex.: "Cheguei 10 min atrasado,
    /// pode ser?") — sempre opcional, pra não quebrar quem já chama sem
    /// ele. Uma transição inválida (ex.: concluir um já cancelado) lança
    /// DomainException dentro da entidade -> 400 pelo middleware.
    /// </summary>
    [Authorize(Roles = "Admin,Barbeiro")]
    [HttpPost("{id:long}/confirmar")]
    public async Task<IActionResult> FnConfirmar(long id, [FromQuery] string? mensagem, CancellationToken ct)
    {
        await _service.FnConfirmarAsync(id, mensagem, ct);
        return NoContent();
    }

    /// <summary>POST /api/agendamentos/5/rejeitar?mensagem=... — recusa uma solicitação (Status Pendente) do cliente.</summary>
    [Authorize(Roles = "Admin,Barbeiro")]
    [HttpPost("{id:long}/rejeitar")]
    public async Task<IActionResult> FnRejeitar(long id, [FromQuery] string? mensagem, CancellationToken ct)
    {
        await _service.FnRejeitarAsync(id, mensagem, ct);
        return NoContent();
    }

    [Authorize(Roles = "Admin,Barbeiro")]
    [HttpPost("{id:long}/iniciar-atendimento")]
    public async Task<IActionResult> FnIniciarAtendimento(long id, CancellationToken ct)
    {
        await _service.FnIniciarAtendimentoAsync(id, ct);
        return NoContent();
    }

    [Authorize(Roles = "Admin,Barbeiro")]
    [HttpPost("{id:long}/concluir")]
    public async Task<IActionResult> FnConcluir(long id, CancellationToken ct)
    {
        await _service.FnConcluirAsync(id, ct);
        return NoContent();
    }

    /// <summary>
    /// FnCancelar fica aberto pro próprio Comum (desistir do horário
    /// pedido/marcado) — por isso NÃO tem [Authorize(Roles=...)] aqui,
    /// diferente das outras transições acima. 'mensagem' normalmente só
    /// é usado quando é o STAFF cancelando (avisando o motivo pro
    /// cliente); o próprio cliente cancelando não precisa mandar nada.
    /// </summary>
    [HttpPost("{id:long}/cancelar")]
    public async Task<IActionResult> FnCancelar(long id, [FromQuery] string? mensagem, CancellationToken ct)
    {
        await _service.FnCancelarAsync(id, mensagem, ct);
        return NoContent();
    }

    [Authorize(Roles = "Admin,Barbeiro")]
    [HttpPost("{id:long}/nao-compareceu")]
    public async Task<IActionResult> FnMarcarNaoCompareceu(long id, [FromQuery] string? mensagem, CancellationToken ct)
    {
        await _service.FnMarcarNaoCompareceuAsync(id, mensagem, ct);
        return NoContent();
    }
}
