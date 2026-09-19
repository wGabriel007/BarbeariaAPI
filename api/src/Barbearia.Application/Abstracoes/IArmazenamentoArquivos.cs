namespace Barbearia.Application.Abstracoes;

/// <summary>
/// Guarda arquivos enviados pelo usuário (foto de perfil, logo da
/// barbearia) — o Domain/Application só conhece esta interface, nunca o
/// "como" (pasta em disco, nome do arquivo, bucket de nuvem, etc.).
/// Pensado pra trocar a implementação local em disco (ver Infrastructure/
/// Storage/ArmazenamentoArquivosLocal) por um provedor de nuvem (S3, Azure
/// Blob) no futuro sem tocar em nenhum Service.
/// </summary>
public interface IArmazenamentoArquivos
{
    /// <summary>
    /// Salva o conteúdo numa subpasta (ex.: "fotos-usuarios", "logo") com
    /// um nome de arquivo único, e devolve a URL RELATIVA pra acessá-lo
    /// depois (ex.: "/uploads/fotos-usuarios/3f2a...-c1.jpg"). O front
    /// completa isso com a origem da própria Api pra montar a URL final.
    /// </summary>
    Task<string> FnSalvarAsync(Stream conteudo, string extensao, string subpasta, CancellationToken ct = default);

    /// <summary>
    /// Remove um arquivo salvo anteriormente (ex.: a foto/logo antiga, ao
    /// trocar por uma nova) — melhor esforço: nunca lança, mesmo se o
    /// arquivo já não existir mais.
    /// </summary>
    void FnRemover(string? urlRelativa);
}
