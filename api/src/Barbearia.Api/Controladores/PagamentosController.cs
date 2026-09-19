using Barbearia.Application.Dtos.Pagamentos;
using Barbearia.Application.Servicos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Barbearia.Api.Controladores;

/// <summary>
/// Controller inteiro é Admin/Barbeiro — status de pagamento de outros
/// clientes é informação de gestão do negócio, um usuário Comum não tem
/// motivo pra ver isso (e muito menos confirmar/cancelar um pagamento).
/// </summary>
[ApiController]
[Route("api/pagamentos")]
[Authorize(Roles = "Admin,Barbeiro")]
public class PagamentosController : ControllerBase
{
    private readonly PagamentoService _service;

    public PagamentosController(PagamentoService service)
    {
        _service = service;
    }

    /// <summary>
    /// GET /api/pagamentos?de=2026-09-15T00:00:00-03:00&amp;ate=2026-09-16T00:00:00-03:00
    /// Mesmo padrão de "de/ate" já usado em
    /// GET /api/agendamentos/por-barbeiro/5 — o front manda um dia só
    /// pra aba "Hoje" e um intervalo maior pra aba "Histórico".
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<List<PagamentoResponse>>> FnListarPorPeriodo(
        [FromQuery] DateTimeOffset de, [FromQuery] DateTimeOffset ate, CancellationToken ct) =>
        Ok(await _service.FnListarPorPeriodoAsync(de, ate, ct));

    /// <summary>GET /api/pagamentos/por-cliente/5 — histórico de pagamentos (qualquer status) de um cliente específico.</summary>
    [HttpGet("por-cliente/{clienteId:long}")]
    public async Task<ActionResult<List<PagamentoResponse>>> FnListarPorCliente(long clienteId, CancellationToken ct) =>
        Ok(await _service.FnListarPorClienteAsync(clienteId, ct));

    /// <summary>POST /api/pagamentos/5/confirmar — barbeiro confere que o cliente pagou.</summary>
    [HttpPost("{id:long}/confirmar")]
    public async Task<ActionResult<PagamentoResponse>> FnConfirmar(long id, ConfirmarPagamentoRequest request, CancellationToken ct) =>
        Ok(await _service.FnConfirmarAsync(id, request, ct));

    /// <summary>POST /api/pagamentos/5/cancelar — barbeiro marca que o cliente não pagou (ficou devendo).</summary>
    [HttpPost("{id:long}/cancelar")]
    public async Task<ActionResult<PagamentoResponse>> FnCancelar(long id, CancellationToken ct) =>
        Ok(await _service.FnCancelarAsync(id, ct));
}
