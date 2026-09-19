using Barbearia.Application.Abstracoes;

namespace Barbearia.Infrastructure.Armazenamento;

/// <summary>
/// Implementação mais simples possível de IArmazenamentoArquivos: grava
/// no disco local, dentro de wwwroot/uploads/{subpasta} — servido depois
/// como arquivo estático pela própria Api (ver app.UseStaticFiles() em
/// Program.cs). Funciona bem pra uma barbearia rodando num servidor só;
/// se um dia isso crescer pra múltiplos servidores atrás de um load
/// balancer, troca-se só esta classe por uma que fale com S3/Azure Blob
/// (a interface não muda).
/// </summary>
public class ArmazenamentoArquivosLocal : IArmazenamentoArquivos
{
    private readonly string _pastaWebRoot;

    public ArmazenamentoArquivosLocal(string pastaWebRoot)
    {
        _pastaWebRoot = pastaWebRoot;
    }

    public async Task<string> FnSalvarAsync(Stream conteudo, string extensao, string subpasta, CancellationToken ct = default)
    {
        var pastaDestino = Path.Combine(_pastaWebRoot, "uploads", subpasta);
        Directory.CreateDirectory(pastaDestino);

        // Nome aleatório (Guid), nunca o nome original enviado pelo
        // navegador — evita colisão entre dois uploads com o mesmo nome
        // de arquivo e também um usuário tentando "adivinhar" o nome do
        // arquivo de outra pessoa.
        var nomeArquivo = $"{Guid.NewGuid():N}{extensao}";
        var caminhoCompleto = Path.Combine(pastaDestino, nomeArquivo);

        await using (var arquivo = File.Create(caminhoCompleto))
        {
            await conteudo.CopyToAsync(arquivo, ct);
        }

        // Barra "/" sempre, mesmo se este processo rodar no Windows —
        // isto vira uma URL http (servida por UseStaticFiles), não um
        // caminho de arquivo do sistema operacional.
        return $"/uploads/{subpasta}/{nomeArquivo}";
    }

    public void FnRemover(string? urlRelativa)
    {
        if (string.IsNullOrWhiteSpace(urlRelativa))
            return;

        var caminhoRelativo = urlRelativa.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
        var caminhoCompleto = Path.Combine(_pastaWebRoot, caminhoRelativo);

        try
        {
            if (File.Exists(caminhoCompleto))
                File.Delete(caminhoCompleto);
        }
        catch (IOException)
        {
            // Melhor esforço: se o arquivo estiver em uso ou sem permissão
            // de exclusão, não é motivo pra falhar a troca da foto/logo —
            // o arquivo velho só fica "órfão" em disco.
        }
    }
}
