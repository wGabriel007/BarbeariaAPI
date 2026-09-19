using Microsoft.Extensions.Configuration;

namespace Barbearia.Infrastructure.Seguranca;

/// <summary>
/// Espelha a seção "Jwt" do appsettings (ver appsettings.Development.json
/// e o LEIA-ME sobre configurar a chave via dotnet user-secrets).
/// </summary>
public class JwtSettings
{
    /// <summary>
    /// Chave secreta usada para ASSINAR o token (HMAC-SHA256). Precisa
    /// ter pelo menos 32 caracteres — é literalmente a senha-mestra da
    /// Api: quem tiver essa chave consegue forjar um token válido pra
    /// QUALQUER usuário. Nunca comite ela no Git (ver LEIA-ME.md).
    /// </summary>
    public string Key { get; set; } = string.Empty;

    public string Issuer { get; set; } = "Barbearia.Api";
    public string Audience { get; set; } = "Barbearia.Frontend";
    public int ExpiraMinutos { get; set; } = 480; // 8h — um "dia de trabalho" do barbeiro

    /// <summary>
    /// Lida à mão com o indexador de IConfiguration (em vez de
    /// configuration.GetSection("Jwt").Get&lt;JwtSettings&gt;()) de propósito:
    /// esse método de "bind automático" exige o pacote
    /// Microsoft.Extensions.Configuration.Binder, mais uma dependência
    /// NuGet pra algo que dá pra fazer só com o indexador, que já vem de
    /// carona com os pacotes que a Infrastructure já usa.
    ///
    /// Usado em DOIS lugares que precisam do MESMO valor: aqui na
    /// Infrastructure (pra gerar o token) e em Barbearia.Api/Program.cs
    /// (pra configurar como a Api VALIDA o token recebido) — daí ser um
    /// método estático em vez de só ler direto onde é usado.
    /// </summary>
    public static JwtSettings FnLerDaConfiguracao(IConfiguration configuration)
    {
        var chave = configuration["Jwt:Key"]
            ?? throw new InvalidOperationException(
                "Jwt:Key não configurado. Veja appsettings.Development.json ou dotnet user-secrets — " +
                "precisa ser uma string aleatória de pelo menos 32 caracteres.");

        if (chave.Length < 32)
            throw new InvalidOperationException("Jwt:Key precisa ter pelo menos 32 caracteres.");

        var settings = new JwtSettings { Key = chave };

        if (configuration["Jwt:Issuer"] is { } issuer) settings.Issuer = issuer;
        if (configuration["Jwt:Audience"] is { } audience) settings.Audience = audience;
        if (int.TryParse(configuration["Jwt:ExpiraMinutos"], out var minutos)) settings.ExpiraMinutos = minutos;

        return settings;
    }
}
