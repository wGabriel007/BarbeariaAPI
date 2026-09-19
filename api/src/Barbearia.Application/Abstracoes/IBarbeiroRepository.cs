using Barbearia.Application.Comum;
using Barbearia.Domain.Entidades;

namespace Barbearia.Application.Abstracoes;

public interface IBarbeiroRepository : IRepository<Barbeiro>
{
    /// <summary>Traz o barbeiro já com os Horarios carregados (join), pra evitar N+1 na tela de agenda.</summary>
    Task<Barbeiro?> FnObterComHorariosAsync(long id, CancellationToken ct = default);

    Task<bool> FnExistePorUsuarioIdAsync(long usuarioId, CancellationToken ct = default);

    /// <summary>Acha o cadastro de Barbeiro do usuário logado — usado pra um Barbeiro (não Admin) ver só as PRÓPRIAS solicitações pendentes.</summary>
    Task<Barbeiro?> FnObterPorUsuarioIdAsync(long usuarioId, CancellationToken ct = default);
}
