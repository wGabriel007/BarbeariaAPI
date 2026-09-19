using Barbearia.Api.Extensoes;
using Barbearia.Application.Dtos.Planos;
using Barbearia.Application.Servicos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Barbearia.Api.Controladores;

/// <summary>
/// Pedidos de assinatura de plano feitos por um usuário Comum — mesmo
/// desenho do fluxo de agendamento (SolicitacaoPlano nasce Pendente,
/// staff aceita/rejeita na aba "Solicitações"). Ver SolicitacaoPlanoService.
/// </summary>
[ApiController]
[Route("api/solicitacoes-planos")]
public class SolicitacoesPlanoController : ControllerBase
{
    private readonly SolicitacaoPlanoService _service;

    public SolicitacoesPlanoController(SolicitacaoPlanoService service)
    {
        _service = service;
    }

    /// <summary>POST /api/solicitacoes-planos — qualquer usuário logado pode pedir um plano pra si mesmo.</summary>
    [HttpPost]
    public async Task<ActionResult<SolicitacaoPlanoResponse>> FnSolicitar(SolicitarPlanoRequest request, CancellationToken ct)
    {
        var solicitacao = await _service.FnSolicitarAsync(User.FnObterUsuarioId(), request, ct);
        return StatusCode(StatusCodes.Status201Created, solicitacao);
    }

    /// <summary>GET /api/solicitacoes-planos/pendentes — "aba de solicitações" do staff.</summary>
    [Authorize(Roles = "Admin,Barbeiro")]
    [HttpGet("pendentes")]
    public async Task<ActionResult<List<SolicitacaoPlanoResponse>>> FnListarPendentes(CancellationToken ct) =>
        Ok(await _service.FnListarPendentesAsync(ct));

    /// <summary>GET /api/solicitacoes-planos/meus — "Meus planos" do usuário logado.</summary>
    [HttpGet("meus")]
    public async Task<ActionResult<List<SolicitacaoPlanoResponse>>> FnListarMeus(CancellationToken ct) =>
        Ok(await _service.FnListarMeusAsync(User.FnObterUsuarioId(), ct));

    /// <summary>POST /api/solicitacoes-planos/5/aceitar — cria a Assinatura de verdade (ver AceitarSolicitacaoPlanoRequest).</summary>
    [Authorize(Roles = "Admin,Barbeiro")]
    [HttpPost("{id:long}/aceitar")]
    public async Task<ActionResult<SolicitacaoPlanoResponse>> FnAceitar(long id, AceitarSolicitacaoPlanoRequest request, CancellationToken ct) =>
        Ok(await _service.FnAceitarAsync(id, request, ct));

    /// <summary>POST /api/solicitacoes-planos/5/rejeitar</summary>
    [Authorize(Roles = "Admin,Barbeiro")]
    [HttpPost("{id:long}/rejeitar")]
    public async Task<ActionResult<SolicitacaoPlanoResponse>> FnRejeitar(long id, RejeitarSolicitacaoPlanoRequest request, CancellationToken ct) =>
        Ok(await _service.FnRejeitarAsync(id, request, ct));
}
