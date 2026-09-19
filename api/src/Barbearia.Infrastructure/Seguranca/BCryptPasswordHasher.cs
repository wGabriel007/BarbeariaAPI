using Barbearia.Application.Comum;

namespace Barbearia.Infrastructure.Seguranca;

public class BCryptPasswordHasher : IPasswordHasher
{
    public string FnHash(string senha) => BCrypt.Net.BCrypt.HashPassword(senha);

    public bool FnVerificar(string senha, string hash) => BCrypt.Net.BCrypt.Verify(senha, hash);
}
