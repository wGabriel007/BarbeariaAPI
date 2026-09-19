using Barbearia.Domain.Entidades;

namespace Barbearia.Application.Comum;

/// <summary>
/// Mesma ideia do IPasswordHasher: a Application sabe que precisa gerar
/// um token de acesso pra um Usuario autenticado, mas não sabe (nem
/// deveria saber) o formato do token, o algoritmo de assinatura, ou de
/// onde vem a chave secreta. Isso tudo é montado pela implementação
/// real (Infrastructure/Security/JwtTokenGenerator, usando JWT).
/// </summary>
public interface IJwtTokenGenerator
{
    string FnGerarToken(Usuario usuario);
}
