using Barbearia.Domain.Enumeracoes;

namespace Barbearia.Application.Dtos.Planos;

/// <summary>
/// Email/Telefone digitados na hora do pedido — não os que já estavam
/// na conta (o Usuario nem tem campo de telefone). Vira o contato do
/// Cliente auto-provisionado se/quando aceito (ver
/// SolicitacaoPlanoService.FnAceitarAsync).
/// </summary>
public sealed record SolicitarPlanoRequest(long PlanoId, string Email, string Telefone);

/// <summary>DataInicio/DataVencimento completam o que o pedido não tinha — vão direto pra Assinatura criada (ver Assinatura.FnCriar).</summary>
public sealed record AceitarSolicitacaoPlanoRequest(DateOnly DataInicio, DateOnly DataVencimento);

public sealed record RejeitarSolicitacaoPlanoRequest(string? Mensagem);

/// <summary>
/// AtualizadoEm (igual AgendamentoResponse) — reaproveitado no front pra
/// saber se um pedido de plano foi Aceito/Rejeitado desde a última vez
/// que a pessoa olhou "Meu plano", e acender a bolinha de notificação
/// (ver Layout.jsx).
/// </summary>
public sealed record SolicitacaoPlanoResponse(
    long Id,
    long UsuarioId,
    string NomeUsuario,
    long PlanoId,
    string NomePlano,
    string Email,
    string Telefone,
    StatusSolicitacaoPlano Status,
    string? MensagemResposta,
    long? AssinaturaId,
    DateTimeOffset CriadoEm,
    DateTimeOffset AtualizadoEm);
