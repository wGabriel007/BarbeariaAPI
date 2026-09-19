using Barbearia.Api.Comum;
using Barbearia.Api.Extensoes;
using Barbearia.Application.Dtos.Usuarios;
using Barbearia.Application.Servicos;
using Microsoft.AspNetCore.Mvc;

namespace Barbearia.Api.Controladores;

/// <summary>
/// Aba "Meu perfil" — self-service, sem [Authorize(Roles=...)] nenhum de
/// propósito: qualquer usuário logado (Admin, Barbeiro ou Comum) vê e
/// edita os PRÓPRIOS dados por aqui. Diferente de UsuariosController
/// (gestão de contas de terceiros, só Admin/Barbeiro), este Controller
/// nunca recebe um id escolhido por quem chama — sempre
/// User.FnObterUsuarioId(), extraído do próprio token.
/// </summary>
[ApiController]
[Route("api/perfil")]
public class PerfilController : ControllerBase
{
    private readonly UsuarioService _service;

    public PerfilController(UsuarioService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<ActionResult<PerfilResponse>> FnObter(CancellationToken ct) =>
        Ok(await _service.FnObterPerfilAsync(User.FnObterUsuarioId(), ct));

    [HttpPut]
    public async Task<ActionResult<PerfilResponse>> FnAtualizar(AtualizarPerfilRequest request, CancellationToken ct) =>
        Ok(await _service.FnAtualizarPerfilAsync(User.FnObterUsuarioId(), request, ct));

    [HttpPost("senha")]
    public async Task<IActionResult> FnAlterarSenha(AlterarSenhaRequest request, CancellationToken ct)
    {
        await _service.FnAlterarSenhaAsync(User.FnObterUsuarioId(), request, ct);
        return NoContent();
    }

    [HttpPost("foto")]
    public async Task<ActionResult<PerfilResponse>> FnEnviarFoto(IFormFile arquivo, CancellationToken ct)
    {
        var extensao = ValidacaoUpload.FnValidarImagem(arquivo);

        await using var conteudo = arquivo.OpenReadStream();
        return Ok(await _service.FnAtualizarFotoAsync(User.FnObterUsuarioId(), conteudo, extensao, ct));
    }

    [HttpDelete("foto")]
    public async Task<ActionResult<PerfilResponse>> FnRemoverFoto(CancellationToken ct) =>
        Ok(await _service.FnRemoverFotoAsync(User.FnObterUsuarioId(), ct));
}
