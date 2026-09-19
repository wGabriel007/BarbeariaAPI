using Barbearia.Api.Comum;
using Barbearia.Application.Dtos.ConfiguracoesSite;
using Barbearia.Application.Servicos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Barbearia.Api.Controladores;

/// <summary>
/// Marca/aparência do site (nome exibido, logo, cor de destaque) e as
/// informações públicas do negócio (descrição, endereço, telefone,
/// Instagram, horário de funcionamento, galeria de fotos) — ver
/// ConfiguracaoSiteService. Leitura é pública (a tela de FnLogin precisa
/// disso ANTES de qualquer login existir). A alteração se divide em dois
/// níveis: nome/logo/cor é só Admin (decisão de dono do negócio, ver
/// FnAtualizar/FnEnviarLogo/FnRemoverLogo); as informações/fotos (a
/// partir de FnAtualizarInformacoes) são Admin OU Barbeiro — operação do
/// dia a dia, igual Serviços/Barbeiros/Planos (ver paginas/SobreABarbearia.jsx).
/// </summary>
[ApiController]
[Route("api/configuracao-site")]
public class ConfiguracaoSiteController : ControllerBase
{
    private readonly ConfiguracaoSiteService _service;

    public ConfiguracaoSiteController(ConfiguracaoSiteService service)
    {
        _service = service;
    }

    [AllowAnonymous]
    [HttpGet]
    public async Task<ActionResult<ConfiguracaoSiteResponse>> FnObter(CancellationToken ct) =>
        Ok(await _service.FnObterAsync(ct));

    [Authorize(Roles = "Admin")]
    [HttpPut]
    public async Task<ActionResult<ConfiguracaoSiteResponse>> FnAtualizar(AtualizarConfiguracaoSiteRequest request, CancellationToken ct) =>
        Ok(await _service.FnAtualizarAsync(request, ct));

    [Authorize(Roles = "Admin")]
    [HttpPost("logo")]
    public async Task<ActionResult<ConfiguracaoSiteResponse>> FnEnviarLogo(IFormFile arquivo, CancellationToken ct)
    {
        var extensao = ValidacaoUpload.FnValidarImagem(arquivo);

        await using var conteudo = arquivo.OpenReadStream();
        return Ok(await _service.FnAtualizarLogoAsync(conteudo, extensao, ct));
    }

    [Authorize(Roles = "Admin")]
    [HttpDelete("logo")]
    public async Task<ActionResult<ConfiguracaoSiteResponse>> FnRemoverLogo(CancellationToken ct) =>
        Ok(await _service.FnRemoverLogoAsync(ct));

    /// <summary>
    /// PUT /api/configuracao-site/informacoes — descrição, endereço,
    /// telefone, Instagram, horário de funcionamento. Admin OU Barbeiro
    /// (diferente do PUT sem sufixo acima, que é nome/cor, só Admin).
    /// </summary>
    [Authorize(Roles = "Admin,Barbeiro")]
    [HttpPut("informacoes")]
    public async Task<ActionResult<ConfiguracaoSiteResponse>> FnAtualizarInformacoes(AtualizarInformacoesBarbeariaRequest request, CancellationToken ct) =>
        Ok(await _service.FnAtualizarInformacoesAsync(request, ct));

    [Authorize(Roles = "Admin,Barbeiro")]
    [HttpPost("fotos")]
    public async Task<ActionResult<ConfiguracaoSiteResponse>> FnAdicionarFoto(IFormFile arquivo, CancellationToken ct)
    {
        var extensao = ValidacaoUpload.FnValidarImagem(arquivo);

        await using var conteudo = arquivo.OpenReadStream();
        return Ok(await _service.FnAdicionarFotoAsync(conteudo, extensao, ct));
    }

    [Authorize(Roles = "Admin,Barbeiro")]
    [HttpDelete("fotos/{fotoId:long}")]
    public async Task<ActionResult<ConfiguracaoSiteResponse>> FnRemoverFoto(long fotoId, CancellationToken ct) =>
        Ok(await _service.FnRemoverFotoAsync(fotoId, ct));
}
