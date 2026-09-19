using Barbearia.Application.Abstracoes;
using Barbearia.Domain.Entidades;
using Microsoft.EntityFrameworkCore;

namespace Barbearia.Infrastructure.Persistencia.Repositorios;

public class ServicoRepository : EfRepository<Servico>, IServicoRepository
{
    public ServicoRepository(BarbeariaDbContext context) : base(context)
    {
    }

    public async Task<bool> FnExisteComNomeAsync(string nome, CancellationToken ct = default) =>
        await Context.Servicos.AnyAsync(s => s.Nome == nome, ct);
}
