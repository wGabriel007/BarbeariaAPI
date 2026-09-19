using Barbearia.Domain.Enumeracoes;

namespace Barbearia.Application.Dtos.Agendamentos;

public sealed record CriarAgendamentoRequest(
    long ClienteId,
    long BarbeiroId,
    long ServicoId,
    DateTimeOffset Inicio,
    long? AssinaturaId,
    string? Observacoes);

/// <summary>
/// Usado pelo próprio cliente (Comum) — sem ClienteId (o servidor
/// descobre/cria o Cliente dele a partir do token) e sem AssinaturaId
/// (uso de plano é algo que o staff aplica, não o próprio cliente).
/// </summary>
public sealed record SolicitarAgendamentoRequest(
    long BarbeiroId,
    long ServicoId,
    DateTimeOffset Inicio,
    string? Observacoes);

/// <summary>
/// AtualizadoEm vem direto da coluna atualizado_em (preenchida pelo
/// próprio banco a cada UPDATE — ver AuditableEntity) — usado no front
/// pra saber se algo mudou num agendamento (confirmado, rejeitado,
/// cancelado...) desde a última vez que a pessoa olhou "Meus
/// agendamentos", pra acender a bolinha de notificação (ver
/// Layout.jsx). Não é um campo "de notificação" de verdade, é só o
/// timestamp que já existia sendo reaproveitado pra essa comparação.
/// </summary>
public sealed record AgendamentoResponse(
    long Id,
    long ClienteId,
    long BarbeiroId,
    long ServicoId,
    long? AssinaturaId,
    DateTimeOffset Inicio,
    DateTimeOffset Fim,
    StatusAgendamento Status,
    decimal PrecoCobrado,
    string? Observacoes,
    string? MensagemResposta,
    DateTimeOffset AtualizadoEm);

/// <summary>
/// Um cliente (não necessariamente o logado) dentro da fila de hoje de
/// um barbeiro — ver FilaDoBarbeiroResponse. SouEu marca qual item é o
/// do próprio usuário que pediu a lista, pra o front destacar a própria
/// linha sem precisar comparar Ids.
/// </summary>
public sealed record ItemFilaResponse(
    long AgendamentoId,
    string NomeCliente,
    DateTimeOffset Inicio,
    StatusAgendamento Status,
    bool SouEu);

/// <summary>
/// "Fila de hoje" pra quem pediu — ver AgendamentoService.FnListarMinhaFilaAsync,
/// que devolve isso tanto pro CLIENTE logado quanto pro BARBEIRO logado.
/// Itens conta só quem ainda não foi atendido (Confirmado) ou está sendo
/// atendido AGORA (EmAtendimento) hoje, na ordem do horário. Pro cliente,
/// MinhaPosicao = 1 significa "você é o próximo da vez" (ver Layout.jsx,
/// que dispara um aviso quando isso muda de >1 pra 1, e a aba "Fila de
/// espera", que mostra a lista inteira) e MeuAgendamentoId aponta qual
/// item é o dele (mesma info que ItemFilaResponse.SouEu, redundante de
/// propósito pra o front não precisar procurar). Pro barbeiro, não existe
/// "minha posição" — ele não é um dos clientes esperando — então
/// MeuAgendamentoId e MinhaPosicao voltam 0 e é assim que o front
/// distingue as duas visões. Não existe tabela de fila no banco: é tudo
/// calculado na hora a partir dos mesmos agendamentos que já existem,
/// igual todo o resto de "notificação" deste sistema. Devolvida em lista
/// porque o cliente pode (raramente) ter mais de um agendamento
/// confirmado hoje, com barbeiros diferentes — um item por barbeiro (já
/// o barbeiro só tem a própria fila, então a lista aqui tem no máximo um
/// item).
/// </summary>
public sealed record FilaDoBarbeiroResponse(
    long BarbeiroId,
    string NomeBarbeiro,
    long MeuAgendamentoId,
    int MinhaPosicao,
    int TotalNaFila,
    List<ItemFilaResponse> Itens);
