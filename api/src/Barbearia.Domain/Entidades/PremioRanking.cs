using Barbearia.Domain.Comum;

namespace Barbearia.Domain.Entidades;

/// <summary>
/// Prêmio configurado pelo Admin/Barbeiro para UMA posição do pódio do
/// Ranking de Cortes de um mês específico (ex.: mes=9, ano=2026,
/// posicao=1, descricao="R$150 de bônus"). Uma linha por (mes, ano,
/// posicao) — ver índice único em PremioRankingConfiguration.
///
/// Não existe "prêmio padrão" nem cópia automática do mês anterior: é
/// assim que o pedido do Gabriel de "reinicia a cada mês, o barbeiro/adm
/// seta de novo" fica garantido — se ninguém configurar nada para o mês
/// novo, o ranking desse mês simplesmente não tem prêmio até alguém
/// definir (ver RankingService.FnDefinirPremiosAsync).
///
/// Prêmio de um mês que já FECHOU nunca muda — não existe método pra
/// alterar mes/ano/posicao depois de criado, só a descrição (ex.: corrigir
/// um erro de digitação no mesmo mês, antes dele fechar).
/// </summary>
public class PremioRanking : AuditableEntity
{
    public int Mes { get; private set; }
    public int Ano { get; private set; }

    /// <summary>1 = campeão do mês, 2 = vice, e assim por diante — sem limite fixo de posições premiadas.</summary>
    public int Posicao { get; private set; }

    public string Descricao { get; private set; } = string.Empty;

    private PremioRanking()
    {
    }

    public static PremioRanking FnCriar(int mes, int ano, int posicao, string descricao)
    {
        if (mes is < 1 or > 12)
            throw new DomainException("Mês precisa estar entre 1 e 12.");

        if (ano < 2000)
            throw new DomainException("Ano inválido.");

        if (posicao < 1)
            throw new DomainException("Posição precisa ser 1 (campeão) ou maior.");

        if (string.IsNullOrWhiteSpace(descricao))
            throw new DomainException("Descrição do prêmio é obrigatória.");

        return new PremioRanking
        {
            Mes = mes,
            Ano = ano,
            Posicao = posicao,
            Descricao = descricao.Trim(),
        };
    }

    public void FnAtualizarDescricao(string novaDescricao)
    {
        if (string.IsNullOrWhiteSpace(novaDescricao))
            throw new DomainException("Descrição do prêmio é obrigatória.");

        Descricao = novaDescricao.Trim();
    }
}
