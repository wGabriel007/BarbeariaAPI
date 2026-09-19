using Barbearia.Api.Extensoes;
using Barbearia.Application.Dtos.Assinaturas;
using Barbearia.Application.Servicos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Barbearia.Api.Controladores;

[ApiController]
[Route("api/assinaturas")]
public class AssinaturasController : ControllerBase
{
    private readonly AssinaturaService _service;

    public AssinaturasController(AssinaturaService service)
    {
        _service = service;
    }

    /// <summary>GET /api/assinaturas/por-cliente/5</summary>
    [HttpGet("por-cliente/{clienteId:long}")]
    public async Task<ActionResult<List<AssinaturaResponse>>> FnListarPorCliente(long clienteId, CancellationToken ct) =>
        Ok(await _service.FnListarPorClienteAsync(clienteId, ct));

    /// <summary>GET /api/assinaturas/minhas — a aba "Meu Plano" de quem está logado.</summary>
    [HttpGet("minhas")]
    public async Task<ActionResult<List<AssinaturaResponse>>> FnListarMinhas(CancellationToken ct) =>
        Ok(await _service.FnListarMinhasAsync(User.FnObterUsuarioId(), ct));

    /// <summary>
    /// POST /api/assinaturas — só Admin/Barbeiro. Um usuário Comum não
    /// pode criar assinatura (pra ele mesmo ou pra outro cliente).
    /// </summary>
    [Authorize(Roles = "Admin,Barbeiro")]
    [HttpPost]
    public async Task<ActionResult<AssinaturaResponse>> FnCriar(CriarAssinaturaRequest request, CancellationToken ct)
    {
        var assinatura = await _service.FnCriarAsync(request, ct);
        // Não existe um "GET /api/assinaturas/{id}" isolado (só listagem
        // por cliente), então respondemos 201 sem Location — ainda é o
        // status certo pra "recurso criado", só sem o link de retorno.
        return StatusCode(StatusCodes.Status201Created, assinatura);
    }

    [Authorize(Roles = "Admin,Barbeiro")]
    [HttpPost("{id:long}/suspender")]
    public async Task<IActionResult> FnSuspender(long id, CancellationToken ct)
    {
        await _service.FnSuspenderAsync(id, ct);
        return NoContent();
    }

    [Authorize(Roles = "Admin,Barbeiro")]
    [HttpPost("{id:long}/reativar")]
    public async Task<IActionResult> FnReativar(long id, CancellationToken ct)
    {
        await _service.FnReativarAsync(id, ct);
        return NoContent();
    }

    /// <summary>POST /api/assinaturas/5/cancelar?dataCancelamento=2026-09-30</summary>
    [Authorize(Roles = "Admin,Barbeiro")]
    [HttpPost("{id:long}/cancelar")]
    public async Task<IActionResult> FnCancelar(long id, [FromQuery] DateOnly dataCancelamento, CancellationToken ct)
    {
        await _service.FnCancelarAsync(id, dataCancelamento, ct);
        return NoContent();
    }

    /// <summary>
    /// POST /api/assinaturas/5/cancelar-minha — o próprio cliente
    /// cancelando a própria assinatura (self-service, sem
    /// [Authorize(Roles=...)]: qualquer usuário logado pode chamar, o
    /// Service é quem confere a posse). Diferente da rota acima, que é
    /// só do staff e aceita uma data escolhida.
    /// </summary>
    [HttpPost("{id:long}/cancelar-minha")]
    public async Task<IActionResult> FnCancelarMinha(long id, CancellationToken ct)
    {
        await _service.FnCancelarMinhaAsync(id, User.FnObterUsuarioId(), ct);
        return NoContent();
    }
}
