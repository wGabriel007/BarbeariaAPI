using Barbearia.Api.Seguranca;
using Barbearia.Application.Abstracoes;
using Barbearia.Domain.Enumeracoes;

namespace Barbearia.Api.Middleware;

/// <summary>
/// Resolve "qual barbearia é esta requisição" bem cedo no pipeline,
/// ANTES de qualquer Controller/Service tocar o banco — é o que
/// preenche o CurrentTenantService que o BarbeariaDbContext usa nos
/// HasQueryFilter (ver comentário completo lá).
///
/// Duas fontes, dependendo se já existe um token válido ou não:
///
///   1. Requisição AUTENTICADA (tem um JWT válido, ver
///      app.UseAuthentication() logo acima deste middleware no
///      pipeline): o EmpresaId vem da claim "empresa_id" do PRÓPRIO
///      token — nunca de um header, que dá pra falsear. Um SuperAdmin
///      (sem essa claim, ver Usuario.EmpresaId) marca EhSuperAdmin em
///      vez disso.
///
///   2. Requisição ANÔNIMA (login, registrar, leitura pública da
///      configuração do site — os únicos [AllowAnonymous] do sistema):
///      ainda não existe token nenhum pra tirar um EmpresaId dele. Por
///      isso o FRONT manda o "apelido" da barbearia (a parte
///      /barbearia-do-joao do link, ver App.jsx) no header
///      X-Empresa-Slug, em TODA requisição — aqui ele é resolvido pra
///      um EmpresaId de verdade, consultando a tabela empresas
///      (ignorando barbearias Inativas: ninguém entra numa barbearia
///      desativada, nem pra logar).
///
/// Fica registrado DEPOIS de app.UseAuthentication() (precisa de
/// HttpContext.User já preenchido) e ANTES de app.FnUseStatusUsuario()/
/// app.UseAuthorization() — qualquer coisa que dependa do tenant
/// resolvido precisa vir depois deste middleware.
/// </summary>
public class EmpresaResolverMiddleware
{
    private readonly RequestDelegate _next;

    public EmpresaResolverMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, CurrentTenantService tenant, IEmpresaRepository empresas)
    {
        if (context.User.Identity?.IsAuthenticated == true)
        {
            if (context.User.IsInRole(nameof(TipoUsuario.SuperAdmin)))
            {
                tenant.EhSuperAdmin = true;
            }
            else
            {
                var claim = context.User.FindFirst("empresa_id")?.Value;
                if (long.TryParse(claim, out var empresaIdDoToken))
                    tenant.EmpresaId = empresaIdDoToken;
            }
        }
        else
        {
            var slug = context.Request.Headers["X-Empresa-Slug"].FirstOrDefault()?.Trim().ToLowerInvariant();
            if (!string.IsNullOrWhiteSpace(slug))
            {
                var empresa = await empresas.FnObterPorSlugAsync(slug, context.RequestAborted);
                if (empresa is not null && empresa.Status == StatusRegistro.Ativo)
                    tenant.EmpresaId = empresa.Id;
            }
        }

        await _next(context);
    }
}

public static class EmpresaResolverMiddlewareExtensions
{
    public static IApplicationBuilder FnUseEmpresaResolver(this IApplicationBuilder app) =>
        app.UseMiddleware<EmpresaResolverMiddleware>();
}
