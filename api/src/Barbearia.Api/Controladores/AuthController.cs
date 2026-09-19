using Barbearia.Application.Dtos.Autenticacao;
using Barbearia.Application.Servicos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Barbearia.Api.Controladores;

/// <summary>
/// Único Controller com [AllowAnonymous] — todo o resto da Api exige
/// token válido por padrão (ver FallbackPolicy em Program.cs). Faz
/// sentido: pra pedir um token, a pessoa ainda não tem um token.
/// </summary>
[ApiController]
[Route("api/auth")]
[AllowAnonymous]
public class AuthController : ControllerBase
{
    private readonly AutenticacaoService _service;

    public AuthController(AutenticacaoService service)
    {
        _service = service;
    }

    /// <summary>POST /api/auth/registrar — cria a conta e já devolve logado (token + dados do usuário).</summary>
    [HttpPost("registrar")]
    public async Task<ActionResult<AuthResponse>> FnRegistrar(RegistrarRequest request, CancellationToken ct) =>
        Ok(await _service.FnRegistrarAsync(request, ct));

    /// <summary>POST /api/auth/login</summary>
    [HttpPost("login")]
    public async Task<ActionResult<AuthResponse>> FnLogin(LoginRequest request, CancellationToken ct) =>
        Ok(await _service.FnLoginAsync(request, ct));
}
