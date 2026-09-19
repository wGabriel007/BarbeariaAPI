namespace Barbearia.Application.Comum;

/// <summary>
/// "Pedi um recurso que não existe" (ex.: GET /clientes/999 e não tem
/// cliente 999). Fica separado de DomainException (que é "os dados que
/// você mandou violam uma regra de negócio") porque a Api trata as duas
/// de formas diferentes: NotFoundException -> HTTP 404,
/// DomainException -> HTTP 400. Ver Barbearia.Api mais adiante.
/// </summary>
public sealed class NotFoundException : Exception
{
    public NotFoundException(string mensagem) : base(mensagem)
    {
    }

    public static NotFoundException FnPara(string entidade, long id) =>
        new($"{entidade} com id {id} não foi encontrado(a).");
}

/// <summary>
/// O barbeiro já tem outro agendamento que conflita com o horário
/// pedido. É lançada em dois pontos, de propósito:
///
///   1. Pela Application, ANTES de tentar inserir — consultando o
///      banco (AgendamentoRepository.FnExisteConflitoAsync) e dando um
///      erro rápido e amigável na maioria dos casos.
///   2. Pela Infrastructure, se AINDA ASSIM o INSERT falhar por causa
///      do EXCLUDE constraint do Postgres (ver 01_schema.sql) — o que
///      só acontece numa condição de corrida genuína (dois pedidos pro
///      mesmo horário processados ao mesmo tempo, entre a checagem #1
///      e o INSERT). Sem esse segundo ponto, essa corrida rara vira um
///      HTTP 500 feio em vez de uma mensagem de negócio normal.
/// </summary>
public sealed class ConflitoDeHorarioException : Exception
{
    public ConflitoDeHorarioException(string mensagem) : base(mensagem)
    {
    }

    public ConflitoDeHorarioException(string mensagem, Exception innerException) : base(mensagem, innerException)
    {
    }
}
