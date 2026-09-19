using Barbearia.Api.Extensoes;
using Barbearia.Application.Comum;
using Barbearia.Application.Dtos.Barbeiros;
using Barbearia.Application.Servicos;
using Barbearia.Domain.Enumeracoes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Barbearia.Api.Controladores;

[ApiController]
[Route("api/barbeiros")]
public class BarbeirosController : ControllerBase
{
    private readonly BarbeiroService _service;

    public BarbeirosController(BarbeiroService service)
    {
        _service = service;
    }

    /// <summary>
    /// Um usuário Comum/Cliente não vê os barbeiros Inativos, mas os
    /// Ausentes ("de folga hoje") continuam aparecendo normalmente —
    /// só com o status Ausente visível no card (ver Barbeiros.jsx). Quem
    /// realmente impede escolher um barbeiro Ausente pra um horário novo
    /// é AgendamentoService.FnSolicitarAsync (e, antes disso, o próprio
    /// front já tira ele do <select> de "Solicitar agendamento" — ver
    /// MeusAgendamentos.jsx).
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<List<BarbeiroResponse>>> FnListar(CancellationToken ct)
    {
        var lista = await _service.FnListarAsync(ct);
        if (!User.FnEhStaff())
            lista = lista.Where(b => b.Status != StatusRegistro.Inativo).ToList();

        return Ok(lista);
    }

    [HttpGet("{id:long}")]
    public async Task<ActionResult<BarbeiroResponse>> FnObterPorId(long id, CancellationToken ct)
    {
        var barbeiro = await _service.FnObterPorIdAsync(id, ct);
        if (!User.FnEhStaff() && barbeiro.Status == StatusRegistro.Inativo)
            throw NotFoundException.FnPara("Barbeiro", id);

        return Ok(barbeiro);
    }

    /// <summary>
    /// POST /api/barbeiros — promove um Usuario Comum já existente a
    /// Barbeiro (não "cria do zero" mais: ver PromoverBarbeiroRequest e
    /// BarbeiroService.FnPromoverAsync). Só Admin/Barbeiro.
    /// </summary>
    [Authorize(Roles = "Admin,Barbeiro")]
    [HttpPost]
    public async Task<ActionResult<BarbeiroResponse>> FnPromover(PromoverBarbeiroRequest request, CancellationToken ct)
    {
        var barbeiro = await _service.FnPromoverAsync(request, ct);
        return CreatedAtAction(nameof(FnObterPorId), new { id = barbeiro.Id }, barbeiro);
    }

    /// <summary>POST /api/barbeiros/5/horarios — adiciona um horário de trabalho (ex.: Segunda 09:00–18:00).</summary>
    [Authorize(Roles = "Admin,Barbeiro")]
    [HttpPost("{id:long}/horarios")]
    public async Task<ActionResult<BarbeiroResponse>> FnAdicionarHorario(long id, AdicionarHorarioRequest request, CancellationToken ct) =>
        Ok(await _service.FnAdicionarHorarioAsync(id, request, ct));

    /// <summary>DELETE /api/barbeiros/5/horarios/12 — remove um horário de trabalho já cadastrado.</summary>
    [Authorize(Roles = "Admin,Barbeiro")]
    [HttpDelete("{id:long}/horarios/{horarioId:long}")]
    public async Task<ActionResult<BarbeiroResponse>> FnRemoverHorario(long id, long horarioId, CancellationToken ct) =>
        Ok(await _service.FnRemoverHorarioAsync(id, horarioId, ct));

    [Authorize(Roles = "Admin,Barbeiro")]
    [HttpPost("{id:long}/inativar")]
    public async Task<IActionResult> FnInativar(long id, CancellationToken ct)
    {
        await _service.FnInativarAsync(id, ct);
        return NoContent();
    }

    [Authorize(Roles = "Admin,Barbeiro")]
    [HttpPost("{id:long}/ativar")]
    public async Task<IActionResult> FnAtivar(long id, CancellationToken ct)
    {
        await _service.FnAtivarAsync(id, ct);
        return NoContent();
    }

    /// <summary>
    /// POST /api/barbeiros/5/ausencia — liga/desliga a ausência do
    /// barbeiro. Self-service: um Barbeiro só consegue mudar a PRÓPRIA
    /// (o Service confere isso a partir do usuarioId do token, nunca de
    /// um id que o cliente da Api poderia inventar); um Admin pode mudar
    /// a de qualquer barbeiro.
    /// </summary>
    [Authorize(Roles = "Admin,Barbeiro")]
    [HttpPost("{id:long}/ausencia")]
    public async Task<IActionResult> FnDefinirAusencia(long id, DefinirAusenciaRequest request, CancellationToken ct)
    {
        await _service.FnDefinirAusenciaAsync(id, User.FnObterUsuarioId(), User.IsInRole("Admin"), request.Ausente, ct);
        return NoContent();
    }

    /// <summary>
    /// PUT /api/barbeiros/5/perfil — Bio/Especialidade, a apresentação
    /// profissional exibida em "Sobre a barbearia" (ver
    /// paginas/SobreABarbearia.jsx). Self-service, mesma regra de dono do
    /// FnDefinirAusencia acima: um Barbeiro só altera o PRÓPRIO perfil, um
    /// Admin altera o de qualquer um.
    /// </summary>
    [Authorize(Roles = "Admin,Barbeiro")]
    [HttpPut("{id:long}/perfil")]
    public async Task<ActionResult<BarbeiroResponse>> FnAtualizarPerfil(long id, AtualizarPerfilBarbeiroRequest request, CancellationToken ct) =>
        Ok(await _service.FnAtualizarPerfilAsync(id, User.FnObterUsuarioId(), User.IsInRole("Admin"), request, ct));
}
