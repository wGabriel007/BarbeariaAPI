using Barbearia.Domain.Comum;

namespace Barbearia.Domain.Entidades;

/// <summary>
/// Uma foto da galeria de "Sobre a barbearia" (ver
/// ConfiguracaoSite.Fotos) — sempre pertence à ÚNICA linha de
/// ConfiguracaoSite, nunca criada solta (por isso o construtor é
/// 'internal', só a própria ConfiguracaoSite.FnAdicionarFoto cria uma).
/// Sem CriadoEm/AtualizadoEm de propósito — a tabela fotos_barbearia não
/// tem essas colunas (mesmo motivo de HorarioTrabalho): a ordem de
/// exibição já é a ordem de inserção (Id crescente).
/// </summary>
public class FotoBarbearia : Entity
{
    public long ConfiguracaoSiteId { get; private set; }
    public string Url { get; private set; } = null!;

    private FotoBarbearia()
    {
    }

    internal static FotoBarbearia FnCriar(long configuracaoSiteId, string url)
    {
        if (string.IsNullOrWhiteSpace(url))
            throw new DomainException("URL da foto é obrigatória.");

        return new FotoBarbearia
        {
            ConfiguracaoSiteId = configuracaoSiteId,
            Url = url,
        };
    }
}
