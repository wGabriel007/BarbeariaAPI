using Barbearia.Application.Comum;
using Barbearia.Application.Dtos.Servicos;
using Barbearia.Application.Servicos;
using Barbearia.Api.Extensoes;
using Barbearia.Domain.Enumeracoes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Barbearia.Api.Controladores;

[ApiController]
[Route("api/servicos")]
public class ServicosController : ControllerBase
{
    private readonly ServicoService _service;

    public ServicosController(ServicoService service)
    {
        _service = service;
    }

    /// <summary>
    /// GET /api/servicos — catálogo de serviços (corte, barba, etc.). Um
    /// usuário Comum não vê os Inativos (só Admin/Barbeiro) — o filtro é
    /// feito aqui, depois do Service devolver a lista completa, porque
    /// "quem pode ver o quê" é uma decisão de apresentação/autorização
    /// (Api), não uma regra de negócio (Application).
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<List<ServicoResponse>>> FnListar(CancellationToken ct)
    {
        var lista = await _service.FnListarAsync(ct);
        if (!User.FnEhStaff())
            lista = lista.Where(s => s.Status != StatusRegistro.Inativo).ToList();

        return Ok(lista);
    }

    [HttpGet("{id:long}")]
    public async Task<ActionResult<ServicoResponse>> FnObterPorId(long id, CancellationToken ct)
    {
        var servico = await _service.FnObterPorIdAsync(id, ct);
        if (!User.FnEhStaff() && servico.Status == StatusRegistro.Inativo)
            throw NotFoundException.FnPara("Serviço", id);

        return Ok(servico);
    }

    [Authorize(Roles = "Admin,Barbeiro")]
    [HttpPost]
    public async Task<ActionResult<ServicoResponse>> FnCriar(CriarServicoRequest request, CancellationToken ct)
    {
        var servico = await _service.FnCriarAsync(request, ct);
        return CreatedAtAction(nameof(FnObterPorId), new { id = servico.Id }, servico);
    }

    /// <summary>PATCH /api/servicos/5/preco — só o preço muda por aqui (duração/nome exigiriam outra regra de negócio).</summary>
    [Authorize(Roles = "Admin,Barbeiro")]
    [HttpPatch("{id:long}/preco")]
    public async Task<ActionResult<ServicoResponse>> FnAtualizarPreco(long id, AtualizarPrecoServicoRequest request, CancellationToken ct) =>
        Ok(await _service.FnAtualizarPrecoAsync(id, request, ct));

    /// <summary>PATCH /api/servicos/5/categoria — reclassifica um serviço já cadastrado.</summary>
    [Authorize(Roles = "Admin,Barbeiro")]
    [HttpPatch("{id:long}/categoria")]
    public async Task<ActionResult<ServicoResponse>> FnAtualizarCategoria(long id, AtualizarCategoriaServicoRequest request, CancellationToken ct) =>
        Ok(await _service.FnAtualizarCategoriaAsync(id, request, ct));

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
