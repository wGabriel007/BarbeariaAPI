using Barbearia.Application.Dtos.Empresas;
using Barbearia.Application.Servicos;
using Barbearia.Domain.Enumeracoes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Barbearia.Api.Controladores;

/// <summary>
/// Gestão das barbearias cadastradas na plataforma — só o SuperAdmin (ver
/// TipoUsuario.SuperAdmin) acessa isto; o [Authorize(Roles=...)] aqui
/// substitui o FallbackPolicy padrão (autenticado de QUALQUER tipo) por
/// um mais restrito, só pra este Controller.
/// </summary>
[ApiController]
[Route("api/empresas")]
[Authorize(Roles = nameof(TipoUsuario.SuperAdmin))]
public class EmpresasController : ControllerBase
{
    private readonly EmpresaService _service;

    public EmpresasController(EmpresaService service)
    {
        _service = service;
    }

    [HttpPost]
    public async Task<ActionResult<EmpresaCriadaResponse>> FnCriar(CriarEmpresaRequest request, CancellationToken ct) =>
        Ok(await _service.FnCriarAsync(request, ct));

    [HttpGet]
    public async Task<ActionResult<List<EmpresaResponse>>> FnListar(CancellationToken ct) =>
        Ok(await _service.FnListarAsync(ct));

    [HttpPost("{id:long}/ativar")]
    public async Task<ActionResult> FnAtivar(long id, CancellationToken ct)
    {
        await _service.FnAtivarAsync(id, ct);
        return NoContent();
    }

    [HttpPost("{id:long}/inativar")]
    public async Task<ActionResult> FnInativar(long id, CancellationToken ct)
    {
        await _service.FnInativarAsync(id, ct);
        return NoContent();
    }
}
