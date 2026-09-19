using Barbearia.Application.Abstracoes;
using Barbearia.Domain.Entidades;
using Barbearia.Domain.Enumeracoes;
using Microsoft.EntityFrameworkCore;

namespace Barbearia.Infrastructure.Persistencia.Repositorios;

public class AgendamentoRepository : EfRepository<Agendamento>, IAgendamentoRepository
{
    public AgendamentoRepository(BarbeariaDbContext context) : base(context)
    {
    }

    public async Task<bool> FnExisteConflitoAsync(
        long barbeiroId,
        DateTimeOffset inicio,
        DateTimeOffset fim,
        long? ignorarAgendamentoId = null,
        CancellationToken ct = default)
    {
        // Mesma lógica do EXCLUDE constraint do banco (ver
        // 01_schema.sql), só que em LINQ: dois intervalos [a,b) e [c,d)
        // se sobrepõem quando a < d E c < b. Cancelado/NaoCompareceu/
        // Rejeitado não contam como conflito, porque liberam o horário —
        // Pendente CONTA (reserva o horário até o barbeiro decidir).
        return await Context.Agendamentos.AnyAsync(a =>
                a.BarbeiroId == barbeiroId &&
                a.Status != StatusAgendamento.Cancelado &&
                a.Status != StatusAgendamento.NaoCompareceu &&
                a.Status != StatusAgendamento.Rejeitado &&
                a.Inicio < fim &&
                inicio < a.Fim &&
                (ignorarAgendamentoId == null || a.Id != ignorarAgendamentoId),
            ct);
    }

    public async Task<List<Agendamento>> FnListarPorBarbeiroEPeriodoAsync(
        long barbeiroId, DateTimeOffset de, DateTimeOffset ate, CancellationToken ct = default) =>
        await Context.Agendamentos
            .Where(a => a.BarbeiroId == barbeiroId && a.Inicio >= de && a.Inicio < ate)
            .OrderBy(a => a.Inicio)
            .AsNoTracking()
            .ToListAsync(ct);

    public async Task<List<Agendamento>> FnListarPorClienteAsync(long clienteId, CancellationToken ct = default) =>
        await Context.Agendamentos
            .Where(a => a.ClienteId == clienteId)
            .OrderByDescending(a => a.Inicio)
            .AsNoTracking()
            .ToListAsync(ct);

    public async Task<List<Agendamento>> FnListarConcluidosPorPeriodoAsync(
        DateTimeOffset de, DateTimeOffset ate, CancellationToken ct = default) =>
        await Context.Agendamentos
            .Where(a => a.Status == StatusAgendamento.Concluido && a.Inicio >= de && a.Inicio < ate)
            .AsNoTracking()
            .ToListAsync(ct);

    public async Task<List<Agendamento>> FnListarPendentesAsync(long? barbeiroId, CancellationToken ct = default) =>
        await Context.Agendamentos
            .Where(a => a.Status == StatusAgendamento.Pendente && (barbeiroId == null || a.BarbeiroId == barbeiroId))
            .OrderBy(a => a.Inicio)
            .AsNoTracking()
            .ToListAsync(ct);
}
