using Barbearia.Application.Comum;
using Barbearia.Domain.Entidades;

namespace Barbearia.Application.Abstracoes;

public interface IClienteRepository : IRepository<Cliente>
{
    Task<Cliente?> FnObterPorCpfAsync(string cpf, CancellationToken ct = default);

    /// <summary>Acha o Cliente auto-provisionado de um usuário Comum (ver Cliente.UsuarioId) — null se ele ainda não pediu nenhum agendamento.</summary>
    Task<Cliente?> FnObterPorUsuarioIdAsync(long usuarioId, CancellationToken ct = default);
}
