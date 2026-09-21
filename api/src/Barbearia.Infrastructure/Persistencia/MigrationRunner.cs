using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;

namespace Barbearia.Infrastructure.Persistencia;

/// <summary>
/// Aplica sozinho, quando a Api sobe, qualquer migração pendente da
/// pasta /database que ainda não rodou neste banco — nada de copiar SQL
/// na mão de novo. NÃO é EF Core Migrations (o projeto continua sem
/// usar isso — ver o comentário grande em BarbeariaDbContext): é um
/// runner bem mais simples, feito só pra tocar os arquivos
/// "NN_migracao_*.sql" que já existiam manualmente, na ordem certa,
/// lembrando o que já foi aplicado numa tabelinha de controle
/// (migracoes_aplicadas).
///
/// 01_schema.sql e 02_seed.sql ficam DE FORA de propósito: são pra criar
/// um banco do ZERO (rodados manualmente uma única vez, na primeira
/// vez), não migrações incrementais — rodar um CREATE TABLE ou um INSERT
/// de dados de exemplo de novo, por engano, quebraria ou duplicaria
/// dados. Só arquivos "NN_migracao_*.sql" entram aqui.
///
/// Cada arquivo de migração precisa ser escrito de um jeito seguro pra
/// rodar mais de uma vez (idempotente: "ADD COLUMN IF NOT EXISTS", um
/// bloco DO verificando pg_constraint antes de um ADD CONSTRAINT, etc.)
/// — é o que permite este runner simplesmente tentar aplicar tudo que
/// ainda não está marcado como aplicado, sem se preocupar em adivinhar
/// se um banco já tinha recebido aquela mudança manualmente antes desta
/// automação existir.
/// </summary>
public class MigrationRunner
{
    private const string PrefixoRecurso = "Barbearia.Infrastructure.Migracoes.";

    private readonly BarbeariaDbContext _context;
    private readonly ILogger<MigrationRunner> _logger;

    public MigrationRunner(BarbeariaDbContext context, ILogger<MigrationRunner> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task FnAplicarPendentesAsync(CancellationToken ct = default)
    {
        await _context.Database.ExecuteSqlRawAsync(
            """
            CREATE TABLE IF NOT EXISTS migracoes_aplicadas (
              nome_arquivo VARCHAR(255) PRIMARY KEY,
              aplicado_em  TIMESTAMPTZ NOT NULL DEFAULT now()
            )
            """, ct);

        var jaAplicadas = (await _context.Database
                .SqlQueryRaw<string>("SELECT nome_arquivo FROM migracoes_aplicadas")
                .ToListAsync(ct))
            .ToHashSet();

        // Os arquivos .sql são embutidos no próprio assembly (ver
        // Barbearia.Infrastructure.csproj) — assim a Api não depende de
        // achar a pasta /database no disco em tempo de execução,
        // continua funcionando igual publicada em outra máquina.
        var assembly = typeof(MigrationRunner).Assembly;
        var recursos = assembly.GetManifestResourceNames()
            .Where(nome => nome.StartsWith(PrefixoRecurso, StringComparison.Ordinal) && nome.EndsWith(".sql", StringComparison.Ordinal))
            .OrderBy(nome => nome, StringComparer.Ordinal) // nomes começam com "01_", "02_"... a ordem alfabética já é a ordem certa
            .ToList();

        foreach (var recurso in recursos)
        {
            var nomeArquivo = recurso[PrefixoRecurso.Length..];
            if (jaAplicadas.Contains(nomeArquivo))
                continue;

            _logger.LogInformation("Aplicando migração pendente: {Arquivo}", nomeArquivo);

            string sql;
            using (var stream = assembly.GetManifestResourceStream(recurso)!)
            using (var reader = new StreamReader(stream))
            {
                sql = await reader.ReadToEndAsync(ct);
            }

            await using var transacao = await _context.Database.BeginTransactionAsync(ct);
            try
            {
                // NÃO usar ExecuteSqlRawAsync aqui (mesmo com "cancellationToken:"
                // nomeado) — toda sobrecarga dele, mesmo sem nenhum parâmetro real,
                // passa o texto pelo parser de composite-format do EF (igual
                // string.Format), procurando "{0}", "{1}" etc. Qualquer "{" solto no
                // arquivo (até dentro de um comentário, como "{id}" na migração 07)
                // já quebra com FormatException "Expected an ASCII digit". Como o
                // texto da migração não tem NENHUM parâmetro de verdade pra
                // substituir, roda ele direto via ADO.NET (DbCommand), que executa o
                // SQL tal como está, sem interpretar nada — e ainda participa da
                // mesma transação (transacao.GetDbTransaction()).
                var conexao = _context.Database.GetDbConnection();
                using (var comando = conexao.CreateCommand())
                {
                    comando.CommandText = sql;
                    comando.Transaction = transacao.GetDbTransaction();
                    await comando.ExecuteNonQueryAsync(ct);
                }

                // Esta chamada aqui embaixo continua com ExecuteSqlRawAsync de
                // propósito: ela TEM um parâmetro real (nomeArquivo), então o "{0}"
                // é intencional e seguro — o EF substitui certo por um parâmetro do
                // Postgres (não por concatenação de string, sem risco de SQL
                // injection).
                await _context.Database.ExecuteSqlRawAsync(
                    "INSERT INTO migracoes_aplicadas (nome_arquivo) VALUES ({0})", new object[] { nomeArquivo }, ct);
                await transacao.CommitAsync(ct);

                _logger.LogInformation("Migração {Arquivo} aplicada com sucesso.", nomeArquivo);
            }
            catch (Exception ex)
            {
                await transacao.RollbackAsync(ct);

                // Deixa a exceção subir de propósito: Program.cs chama
                // isto ANTES de app.Run(), então um erro aqui impede a
                // Api de subir com um schema pela metade — melhor um
                // crash claro no console na hora de ligar a Api do que
                // um 500 misterioso depois, na primeira tela que tocar
                // a tabela afetada.
                _logger.LogError(ex, "Falha ao aplicar a migração {Arquivo}. A Api não vai subir até isso ser corrigido.", nomeArquivo);
                throw;
            }
        }
    }
}
