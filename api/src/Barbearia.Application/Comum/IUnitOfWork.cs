namespace Barbearia.Application.Comum;

/// <summary>
/// Representa "salvar as mudanças feitas nesta requisição, como uma
/// transação só". Os repositórios (IRepository&lt;T&gt;) só manipulam
/// objetos em memória (Adicionar/FnRemover) — nada é gravado no banco até
/// alguém chamar FnSalvarAsync(). Isso permite que um Service altere
/// várias entidades diferentes (ex.: criar um Agendamento E marcar uma
/// Assinatura como usada) e tudo seja gravado atomicamente em um único
/// SaveChanges/commit, em vez de uma transação por entidade.
/// </summary>
public interface IUnitOfWork
{
    Task FnSalvarAsync(CancellationToken ct = default);
}
