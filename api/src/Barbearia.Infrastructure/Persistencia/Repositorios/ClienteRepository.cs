using Barbearia.Application.Abstracoes;
using Barbearia.Domain.Entidades;
using Microsoft.EntityFrameworkCore;

namespace Barbearia.Infrastructure.Persistencia.Repositorios;

public class ClienteRepository : EfRepository<Cliente>, IClienteRepository
{
    public ClienteRepository(BarbeariaDbContext context) : base(context)
    {
    }

    public async Task<Cliente?> FnObterPorCpfAsync(string cpf, CancellationToken ct = default) =>
        await Context.Clientes.FirstOrDefaultAsync(c => c.Cpf == cpf, ct);

    public async Task<Cliente?> FnObterPorUsuarioIdAsync(long usuarioId, CancellationToken ct = default) =>
        await Context.Clientes.FirstOrDefaultAsync(c => c.UsuarioId == usuarioId, ct);
}
