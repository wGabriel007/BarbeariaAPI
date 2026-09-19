using Barbearia.Domain.Comum;

namespace Barbearia.Application.Comum;

/// <summary>
/// Operações de acesso a dados comuns a toda entidade. Só o essencial —
/// consultas específicas de cada entidade (ex.: "buscar cliente pelo
/// CPF") vão em uma interface própria por recurso, em Abstractions/,
/// que herda desta.
///
/// Por que essa interface mora na Application e não na Infrastructure:
/// é a Application que DEFINE o que ela precisa do banco (linguagem de
/// negócio: "adicionar", "remover", "buscar por id"); a Infrastructure
/// só IMPLEMENTA isso com EF Core. Se um dia trocar Postgres por outro
/// banco, ou EF Core por Dapper, a Application não muda uma linha — só
/// a implementação em Infrastructure muda. Isso é a Inversão de
/// Dependência (o "D" do SOLID) que dá nome à Clean Architecture.
/// </summary>
public interface IRepository<T> where T : Entity
{
    Task<T?> FnObterPorIdAsync(long id, CancellationToken ct = default);
    Task<List<T>> FnListarAsync(CancellationToken ct = default);
    Task FnAdicionarAsync(T entidade, CancellationToken ct = default);
    void FnRemover(T entidade);
}
