namespace Barbearia.Domain.Enumeracoes;

/// <summary>
/// Agrupa os serviços na tela (Serviços) pra ficar organizado — "Corte",
/// "Barba", etc. em vez de uma lista única — sem afetar preço, duração
/// ou qualquer outra regra de negócio; é só apresentação.
/// </summary>
public enum CategoriaServico
{
    Corte = 0,
    Barba = 1,
    ComboCorteEBarba = 2,
    Sobrancelha = 3,
    Coloracao = 4,
    Tratamento = 5,
    Outro = 6,
}
