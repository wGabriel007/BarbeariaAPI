using Barbearia.Application.Comum;
using Barbearia.Domain.Entidades;

namespace Barbearia.Application.Abstracoes;

public interface IUsuarioRepository : IRepository<Usuario>
{
    Task<Usuario?> FnObterPorEmailAsync(string email, CancellationToken ct = default);

    /// <summary>
    /// Existe pelo menos um usuário cadastrado? Usado só pelo
    /// autocadastro (AutenticacaoService.FnRegistrarAsync) pra decidir se
    /// a conta que está sendo criada agora é a primeira do sistema (e
    /// por isso vira Admin automaticamente) ou não (e por isso vira
    /// Comum).
    /// </summary>
    Task<bool> FnExisteAlgumAsync(CancellationToken ct = default);
}
