using Barbearia.Application.Dtos.Clientes;
using Barbearia.Application.Servicos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Barbearia.Api.Controladores;

/// <summary>
/// Controller inteiro é Admin/Barbeiro — diferente de Serviços/Barbeiros/
/// Planos (onde Comum lê o catálogo, só não vê Inativo), aqui um usuário
/// Comum não vê a lista de clientes de jeito nenhum, nem os Ativos. É
/// dado do negócio (quem é cliente, telefone, etc.), não um catálogo
/// público como serviço/plano.
/// </summary>
[ApiController]
[Route("api/clientes")]
[Authorize(Roles = "Admin,Barbeiro")]
public class ClientesController : ControllerBase
{
    private readonly ClienteService _service;

    public ClientesController(ClienteService service)
    {
        _service = service;
    }

    /// <summary>GET /api/clientes</summary>
    [HttpGet]
    public async Task<ActionResult<List<ClienteResponse>>> FnListar(CancellationToken ct) =>
        Ok(await _service.FnListarAsync(ct));

    /// <summary>GET /api/clientes/5</summary>
    [HttpGet("{id:long}")]
    public async Task<ActionResult<ClienteResponse>> FnObterPorId(long id, CancellationToken ct) =>
        Ok(await _service.FnObterPorIdAsync(id, ct));

    /// <summary>
    /// GET /api/clientes/por-usuario/5 — o "cartão do cliente" (dados +
    /// quantos cortes já fez) aberto a partir da aba Usuários, que só
    /// conhece o Id do Usuario, não o do Cliente. 404 se este usuário
    /// ainda não tiver sido vinculado como cliente (ver ClienteDetalheResponse).
    /// </summary>
    [HttpGet("por-usuario/{usuarioId:long}")]
    public async Task<ActionResult<ClienteDetalheResponse>> FnObterDetalhePorUsuario(long usuarioId, CancellationToken ct) =>
        Ok(await _service.FnObterDetalhePorUsuarioIdAsync(usuarioId, ct));

    /// <summary>POST /api/clientes — vincula um Usuario Comum já existente como Cliente (ver PromoverClienteRequest).</summary>
    [HttpPost]
    public async Task<ActionResult<ClienteResponse>> FnPromover(PromoverClienteRequest request, CancellationToken ct)
    {
        var cliente = await _service.FnPromoverAsync(request, ct);
        // 201 Created + header "Location: /api/clientes/{id}" — é o
        // jeito "correto" de responder um POST que cria recurso (em vez
        // de simplesmente 200 Ok), e ainda dá de graça a URL pra buscar
        // o recurso recém-criado.
        return CreatedAtAction(nameof(FnObterPorId), new { id = cliente.Id }, cliente);
    }

    /// <summary>PUT /api/clientes/5</summary>
    [HttpPut("{id:long}")]
    public async Task<ActionResult<ClienteResponse>> FnAtualizar(long id, AtualizarClienteRequest request, CancellationToken ct) =>
        Ok(await _service.FnAtualizarAsync(id, request, ct));

    /// <summary>POST /api/clientes/5/inativar</summary>
    [HttpPost("{id:long}/inativar")]
    public async Task<IActionResult> FnInativar(long id, CancellationToken ct)
    {
        await _service.FnInativarAsync(id, ct);
        return NoContent();
    }

    /// <summary>POST /api/clientes/5/bloquear</summary>
    [HttpPost("{id:long}/bloquear")]
    public async Task<IActionResult> FnBloquear(long id, CancellationToken ct)
    {
        await _service.FnBloquearAsync(id, ct);
        return NoContent();
    }

    /// <summary>POST /api/clientes/5/ativar</summary>
    [HttpPost("{id:long}/ativar")]
    public async Task<IActionResult> FnAtivar(long id, CancellationToken ct)
    {
        await _service.FnAtivarAsync(id, ct);
        return NoContent();
    }
}
