namespace Barbearia.Domain.Comum;

/// <summary>
/// Lançada quando uma regra de negócio do Domain é violada (ex.: tentar
/// criar um agendamento com fim antes do início). A camada Api (Fase 2,
/// mais adiante) converte isso num HTTP 400 com a mensagem — o
/// controller nunca precisa saber QUAIS regras existem, só que essa
/// exceção específica significa "entrada inválida, mostra a mensagem
/// pro usuário".
/// </summary>
public sealed class DomainException : Exception
{
    public DomainException(string message) : base(message)
    {
    }
}
