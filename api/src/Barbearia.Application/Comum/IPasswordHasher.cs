namespace Barbearia.Application.Comum;

/// <summary>
/// Abstração pro algoritmo de hash de senha. A Application sabe que
/// "precisa transformar uma senha em algo seguro pra guardar" e
/// "precisa conferir se uma senha digitada bate com o hash guardado" —
/// mas não sabe (e não deveria saber) qual biblioteca faz isso. A
/// implementação real (Infrastructure/Security/BCryptPasswordHasher)
/// usa BCrypt.Net-Next.
/// </summary>
public interface IPasswordHasher
{
    string FnHash(string senha);
    bool FnVerificar(string senha, string hash);
}
