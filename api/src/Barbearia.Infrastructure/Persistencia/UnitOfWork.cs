using Barbearia.Application.Comum;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Barbearia.Infrastructure.Persistencia;

public class UnitOfWork : IUnitOfWork
{
    private readonly BarbeariaDbContext _context;

    public UnitOfWork(BarbeariaDbContext context)
    {
        _context = context;
    }

    public async Task FnSalvarAsync(CancellationToken ct = default)
    {
        try
        {
            await _context.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (FnEhViolacaoDeExclusaoDeHorario(ex))
        {
            // Segunda camada de defesa contra conflito de horário (ver
            // comentário em IAgendamentoRepository.FnExisteConflitoAsync
            // e AgendamentoService.FnCriarAsync): o Postgres recusou o
            // INSERT/UPDATE por causa do EXCLUDE constraint
            // 'sem_conflito_horario' (01_schema.sql). Em vez de deixar
            // essa DbUpdateException/PostgresException vazar pra cima
            // (o que a Api transformaria num HTTP 500 genérico), ela é
            // traduzida aqui pra uma exceção de negócio que a Api já
            // sabe converter em HTTP 409 com mensagem clara.
            throw new ConflitoDeHorarioException(
                "Este barbeiro já tem um agendamento nesse horário (conflito detectado pelo banco de dados).",
                ex);
        }
    }

    private static bool FnEhViolacaoDeExclusaoDeHorario(DbUpdateException ex) =>
        ex.InnerException is PostgresException pg
        && pg.SqlState == PostgresErrorCodes.ExclusionViolation
        && pg.ConstraintName == "sem_conflito_horario";
}
