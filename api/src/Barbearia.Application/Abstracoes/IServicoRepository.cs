using Barbearia.Application.Comum;
using Barbearia.Domain.Entidades;

namespace Barbearia.Application.Abstracoes;

public interface IServicoRepository : IRepository<Servico>
{
    Task<bool> FnExisteComNomeAsync(string nome, CancellationToken ct = default);
}
