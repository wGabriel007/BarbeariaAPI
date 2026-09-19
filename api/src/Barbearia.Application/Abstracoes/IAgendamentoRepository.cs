using Barbearia.Application.Comum;
using Barbearia.Domain.Entidades;

namespace Barbearia.Application.Abstracoes;

public interface IAgendamentoRepository : IRepository<Agendamento>
{
    /// <summary>
    /// Primeira camada de defesa contra conflito de horário: consulta o
    /// banco ANTES de tentar inserir. Retorna true se o barbeiro já tem
    /// algum agendamento (não cancelado / não-comparecido) que se
    /// sobrepõe ao intervalo [inicio, fim). 'ignorarAgendamentoId' serve
    /// pra quando estamos REAGENDANDO um agendamento existente — nesse
    /// caso ele não deve conflitar com ele mesmo.
    ///
    /// A segunda (e definitiva) camada de defesa é o EXCLUDE constraint
    /// do Postgres em 01_schema.sql — essa consulta aqui é só uma
    /// checagem otimista pra dar erro rápido e amigável no caso comum.
    /// </summary>
    Task<bool> FnExisteConflitoAsync(
        long barbeiroId,
        DateTimeOffset inicio,
        DateTimeOffset fim,
        long? ignorarAgendamentoId = null,
        CancellationToken ct = default);

    Task<List<Agendamento>> FnListarPorBarbeiroEPeriodoAsync(
        long barbeiroId,
        DateTimeOffset de,
        DateTimeOffset ate,
        CancellationToken ct = default);

    Task<List<Agendamento>> FnListarPorClienteAsync(long clienteId, CancellationToken ct = default);

    /// <summary>
    /// Todos os agendamentos CONCLUÍDOS cujo início cai dentro de
    /// [de, ate) — base do Ranking de Clientes (ver RankingService): qual
    /// CLIENTE mais voltou pra cortar no mês. Conta pelo INÍCIO do
    /// atendimento (mesmo campo já usado em FnListarPorBarbeiroEPeriodoAsync),
    /// não por quando foi marcado como concluído, então um corte de fim de
    /// mês concluído só dias depois ainda conta pro mês em que aconteceu.
    /// </summary>
    Task<List<Agendamento>> FnListarConcluidosPorPeriodoAsync(
        DateTimeOffset de,
        DateTimeOffset ate,
        CancellationToken ct = default);

    /// <summary>
    /// Solicitações (Status = Pendente) aguardando confirmação do
    /// barbeiro — a "aba de solicitações" do staff. 'barbeiroId' nulo
    /// devolve de TODOS os barbeiros (visão do Admin); informado,
    /// devolve só as desse barbeiro (visão de um Barbeiro comum, que só
    /// decide sobre a própria agenda).
    /// </summary>
    Task<List<Agendamento>> FnListarPendentesAsync(long? barbeiroId, CancellationToken ct = default);
}
