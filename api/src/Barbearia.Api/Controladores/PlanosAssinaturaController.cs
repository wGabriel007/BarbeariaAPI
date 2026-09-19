using Barbearia.Api.Extensoes;
using Barbearia.Application.Comum;
using Barbearia.Application.Dtos.Planos;
using Barbearia.Application.Servicos;
using Barbearia.Domain.Enumeracoes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Barbearia.Api.Controladores;

[ApiController]
[Route("api/planos-assinatura")]
public class PlanosAssinaturaController : ControllerBase
{
    private readonly PlanoAssinaturaService _service;

    public PlanosAssinaturaController(PlanoAssinaturaService service)
    {
        _service = service;
    }

    /// <summary>Um usuário Comum não vê os planos Inativos (só Admin/Barbeiro) — mesmo raciocínio do ServicosController.</summary>
    [HttpGet]
    public async Task<ActionResult<List<PlanoResponse>>> FnListar(CancellationToken ct)
    {
        var lista = await _service.FnListarAsync(ct);
        if (!User.FnEhStaff())
            lista = lista.Where(p => p.Status != StatusRegistro.Inativo).ToList();

        return Ok(lista);
    }

    [HttpGet("{id:long}")]
    public async Task<ActionResult<PlanoResponse>> FnObterPorId(long id, CancellationToken ct)
    {
        var plano = await _service.FnObterPorIdAsync(id, ct);
        if (!User.FnEhStaff() && plano.Status == StatusRegistro.Inativo)
            throw NotFoundException.FnPara("Plano", id);

        return Ok(plano);
    }

    [Authorize(Roles = "Admin,Barbeiro")]
    [HttpPost]
    public async Task<ActionResult<PlanoResponse>> FnCriar(CriarPlanoRequest request, CancellationToken ct)
    {
        var plano = await _service.FnCriarAsync(request, ct);
        return CreatedAtAction(nameof(FnObterPorId), new { id = plano.Id }, plano);
    }

    /// <summary>POST /api/planos-assinatura/5/servicos — inclui um serviço do catálogo no plano (ex.: "2 cortes por mês").</summary>
    [Authorize(Roles = "Admin,Barbeiro")]
    [HttpPost("{id:long}/servicos")]
    public async Task<ActionResult<PlanoResponse>> FnIncluirServico(long id, IncluirServicoNoPlanoRequest request, CancellationToken ct) =>
        Ok(await _service.FnIncluirServicoAsync(id, request, ct));

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
}
