using Barbearia.Application.Dtos.Usuarios;
using Barbearia.Application.Servicos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Barbearia.Api.Controladores;

/// <summary>
/// Gestão das contas de login do sistema — serve pra criar o Usuario que
/// depois se transforma num Barbeiro (ver BarbeirosController) ou noutro
/// Admin. Controller inteiro é Admin/Barbeiro: um usuário Comum não tem
/// motivo pra ver ou criar contas de acesso de terceiros (ele só gerencia
/// a própria conta, via /api/auth).
/// </summary>
[ApiController]
[Route("api/usuarios")]
[Authorize(Roles = "Admin,Barbeiro")]
public class UsuariosController : ControllerBase
{
    private readonly UsuarioService _service;

    public UsuariosController(UsuarioService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<ActionResult<List<UsuarioResponse>>> FnListar(CancellationToken ct) =>
        Ok(await _service.FnListarAsync(ct));

    [HttpPost]
    public async Task<ActionResult<UsuarioResponse>> FnCriar(CriarUsuarioRequest request, CancellationToken ct)
    {
        var usuario = await _service.FnCriarAsync(request, ct);
        return StatusCode(StatusCodes.Status201Created, usuario);
    }
}
