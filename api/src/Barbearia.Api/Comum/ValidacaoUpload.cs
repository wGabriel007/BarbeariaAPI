using Barbearia.Domain.Comum;
using Microsoft.AspNetCore.Http;

namespace Barbearia.Api.Comum;

/// <summary>
/// Validação de imagem enviada por upload (foto de perfil, logo da
/// barbearia) — compartilhada entre PerfilController e
/// ConfiguracaoSiteController pra não duplicar os mesmos limites em dois
/// lugares. Fica na Api (não na Application) de propósito: IFormFile é um
/// tipo do ASP.NET Core, e a Application/Domain não podem depender disso
/// (ver comentário em BarbeariaDbContext/DependencyInjection sobre manter
/// as camadas de baixo sem depender de frameworks de fora).
/// </summary>
public static class ValidacaoUpload
{
    private const long TamanhoMaximoBytes = 3 * 1024 * 1024; // 3 MB

    private static readonly HashSet<string> TiposDeImagemPermitidos = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/jpeg", "image/png", "image/webp",
    };

    /// <summary>Lança DomainException (vira 400 — ver ExceptionHandlingMiddleware) se o arquivo não passar; devolve a extensão a usar ao salvar.</summary>
    public static string FnValidarImagem(IFormFile? arquivo)
    {
        if (arquivo is null || arquivo.Length == 0)
            throw new DomainException("Selecione uma imagem.");

        if (arquivo.Length > TamanhoMaximoBytes)
            throw new DomainException("A imagem precisa ter no máximo 3 MB.");

        if (!TiposDeImagemPermitidos.Contains(arquivo.ContentType))
            throw new DomainException("Envie uma imagem JPEG, PNG ou WEBP.");

        var extensao = Path.GetExtension(arquivo.FileName);
        return string.IsNullOrWhiteSpace(extensao) ? ".jpg" : extensao;
    }
}
