using System.Net;
using Barbearia.Application.Comum;
using Barbearia.Domain.Comum;

namespace Barbearia.Api.Middleware;

/// <summary>
/// Middleware "guarda-chuva": fica em volta de TODO o pipeline (é
/// registrado antes de app.MapControllers() em Program.cs) e captura
/// qualquer exceção que os Controllers deixem escapar, traduzindo cada
/// tipo de exceção de negócio para o HTTP status certo, com um corpo de
/// erro em formato consistente — assim nenhum Controller precisa de
/// try/catch repetido, e nenhum erro de negócio vira um HTTP 500 cru.
///
///   DomainException          -> 400 Bad Request  (dados violam regra de negócio)
///   NotFoundException        -> 404 Not Found     (id não existe)
///   ConflitoDeHorarioException -> 409 Conflict    (agendamento sobreposto)
///   qualquer outra exceção   -> 500 Internal Server Error (bug real — logado)
/// </summary>
public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            var (status, titulo) = Classificar(ex);

            if (status == HttpStatusCode.InternalServerError)
            {
                // Erro inesperado de verdade (bug, queda de conexão com o
                // banco, etc.) — este sim precisa ir pro log com stack
                // trace completo, porque ninguém vai conseguir investigar
                // um 500 só pela mensagem que volta pro cliente.
                _logger.LogError(ex, "Erro não tratado ao processar {Metodo} {Caminho}", context.Request.Method, context.Request.Path);
            }

            context.Response.ContentType = "application/problem+json";
            context.Response.StatusCode = (int)status;

            await context.Response.WriteAsJsonAsync(new
            {
                status = (int)status,
                title = titulo,
                detail = status == HttpStatusCode.InternalServerError
                    ? "Ocorreu um erro inesperado. Tente novamente ou contate o suporte."
                    : ex.Message,
                traceId = context.TraceIdentifier,
            });
        }
    }

    private static (HttpStatusCode Status, string Titulo) Classificar(Exception ex) => ex switch
    {
        NotFoundException => (HttpStatusCode.NotFound, "Recurso não encontrado"),
        ConflitoDeHorarioException => (HttpStatusCode.Conflict, "Conflito de horário"),
        DomainException => (HttpStatusCode.BadRequest, "Requisição inválida"),
        _ => (HttpStatusCode.InternalServerError, "Erro interno"),
    };
}

public static class ExceptionHandlingMiddlewareExtensions
{
    public static IApplicationBuilder FnUseExceptionHandling(this IApplicationBuilder app) =>
        app.UseMiddleware<ExceptionHandlingMiddleware>();
}
