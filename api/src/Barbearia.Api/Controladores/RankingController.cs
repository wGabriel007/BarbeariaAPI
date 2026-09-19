using Barbearia.Application.Dtos.Ranking;
using Barbearia.Application.Servicos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Barbearia.Api.Controladores;

/// <summary>
/// Ranking de Clientes — sem [Authorize(Roles=...)] nos GETs de propósito:
/// o ponto todo do ranking é incentivar o próprio CLIENTE (Comum) a voltar
/// mais vezes pra aparecer no pódio, então ele precisa poder ver a
/// classificação igual a qualquer staff (a Api só exige estar logado, via
/// FallbackPolicy — ver Program.cs). Só configurar os prêmios é tarefa de
/// staff.
/// </summary>
[ApiController]
[Route("api/ranking")]
public class RankingController : ControllerBase
{
    private readonly RankingService _service;

    public RankingController(RankingService service)
    {
        _service = service;
    }

    /// <summary>GET /api/ranking?mes=9&amp;ano=2026 — pódio de um mês (sem parâmetros, o mês corrente).</summary>
    [HttpGet]
    public async Task<ActionResult<RankingMensalResponse>> FnObter([FromQuery] int? mes, [FromQuery] int? ano, CancellationToken ct) =>
        Ok(await _service.FnObterRankingAsync(mes, ano, ct));

    /// <summary>GET /api/ranking/historico?meses=12 — campeões dos últimos meses já fechados (não inclui o mês corrente, ainda em andamento).</summary>
    [HttpGet("historico")]
    public async Task<ActionResult<List<CampeaoHistoricoResponse>>> FnHistorico([FromQuery] int meses, CancellationToken ct) =>
        Ok(await _service.FnObterHistoricoAsync(meses <= 0 ? 12 : meses, ct));

    /// <summary>PUT /api/ranking/premios — reconfigura o pódio (prêmios) do mês atual inteiro. Só Admin/Barbeiro.</summary>
    [Authorize(Roles = "Admin,Barbeiro")]
    [HttpPut("premios")]
    public async Task<ActionResult<RankingMensalResponse>> FnDefinirPremios(DefinirPremiosRequest request, CancellationToken ct) =>
        Ok(await _service.FnDefinirPremiosAsync(request, ct));
}
