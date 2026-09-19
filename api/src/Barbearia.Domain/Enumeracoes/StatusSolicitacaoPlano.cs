namespace Barbearia.Domain.Enumeracoes;

/// <summary>
/// Espelha o CHECK de solicitacoes_plano.status no 06_migracao_solicitacao_plano.sql.
/// Valores explícitos de propósito, nunca reordenar (ver mesmo aviso em StatusAgendamento).
/// </summary>
public enum StatusSolicitacaoPlano
{
    Pendente = 0,
    Aceita = 1,
    Rejeitada = 2
}
