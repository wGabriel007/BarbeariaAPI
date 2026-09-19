using System.Security.Claims;
using Barbearia.Application.Abstracoes;
using Barbearia.Domain.Enumeracoes;

namespace Barbearia.Api.Middleware;

/// <summary>
/// Fecha uma brecha do JWT: o token é "stateless" (a Api não guarda
/// sessão nenhuma) e continua válido até expirar (Jwt:ExpiraMinutos, 8h
/// por padrão) mesmo que um Admin bloqueie/inative o usuário DEPOIS que
/// o token foi emitido — sem este middleware, quem já estava logado
/// continuaria acessando tudo normalmente até o token vencer sozinho.
///
/// Este middleware faz uma checagem no banco a cada requisição
/// autenticada: se o usuário do token não existe mais, ou não está mais
/// com status Ativo, a requisição é cortada aqui com 401, antes de
/// chegar em qualquer Controller — inclusive marcar horário, que era
/// exatamente o acesso que devia se perder.
///
/// Fica registrado DEPOIS de app.UseAuthentication() (precisa que
/// HttpContext.User já esteja preenchido) e ANTES de
/// app.UseAuthorization().
/// </summary>
public class StatusUsuarioMiddleware
{
    private readonly RequestDelegate _next;

    public StatusUsuarioMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, IUsuarioRepository usuarios)
    {
        if (context.User.Identity?.IsAuthenticated == true)
        {
            // ClaimTypes.NameIdentifier é o nome que o .NET costuma usar
            // depois de mapear automaticamente o claim "sub" do JWT, mas
            // checamos os dois de propósito — assim este middleware
            // continua funcionando mesmo se esse mapeamento automático
            // for desligado em algum momento (evita precisar depender do
            // pacote System.IdentityModel.Tokens.Jwt só por causa da
            // constante JwtRegisteredClaimNames.Sub).
            var idClaim = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                ?? context.User.FindFirst("sub")?.Value;

            if (long.TryParse(idClaim, out var usuarioId))
            {
                var usuario = await usuarios.FnObterPorIdAsync(usuarioId, context.RequestAborted);

                if (usuario is null || usuario.Status != StatusRegistro.Ativo)
                {
                    context.Response.ContentType = "application/problem+json";
                    context.Response.StatusCode = StatusCodes.Status401Unauthorized;

                    await context.Response.WriteAsJsonAsync(new
                    {
                        status = StatusCodes.Status401Unauthorized,
                        title = "Sessão inválida",
                        detail = "Sua conta está inativa ou bloqueada. Fale com o administrador.",
                        traceId = context.TraceIdentifier,
                    });
                    return;
                }
            }
        }

        await _next(context);
    }
}

public static class StatusUsuarioMiddlewareExtensions
{
    public static IApplicationBuilder FnUseStatusUsuario(this IApplicationBuilder app) =>
        app.UseMiddleware<StatusUsuarioMiddleware>();
}
