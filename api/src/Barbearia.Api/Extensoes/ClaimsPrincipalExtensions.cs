using System.Security.Claims;

namespace Barbearia.Api.Extensoes;

/// <summary>
/// "Staff" = Admin ou Barbeiro — os dois têm o mesmo nível de acesso de
/// gestão no sistema (ver comentário equivalente em
/// AuthContext.jsx/ehStaff no front). Usado nos Controllers que
/// precisam decidir, DENTRO da própria ação, se escondem um dado de um
/// usuário Comum (ex.: um registro Inativo) — diferente de
/// [Authorize(Roles = "Admin,Barbeiro")], que bloqueia a ação inteira.
/// </summary>
public static class ClaimsPrincipalExtensions
{
    public static bool FnEhStaff(this ClaimsPrincipal user) =>
        user.IsInRole("Admin") || user.IsInRole("Barbeiro");

    /// <summary>
    /// Id (Usuario.Id) de quem está logado, extraído do claim "sub" do
    /// token (ver JwtTokenGenerator) — o ASP.NET Core mapeia esse claim
    /// automaticamente para ClaimTypes.NameIdentifier ao validar o JWT.
    /// Usado nas rotas em que o próprio usuário logado é o "dono" da
    /// requisição (solicitar/ver os próprios agendamentos), sem precisar
    /// que o cliente mande o próprio Id no corpo (que daria pra falsear).
    /// </summary>
    public static long FnObterUsuarioId(this ClaimsPrincipal user)
    {
        var valor = user.FindFirstValue(ClaimTypes.NameIdentifier);
        return long.TryParse(valor, out var id)
            ? id
            : throw new InvalidOperationException("Token não contém um Id de usuário válido.");
    }
}
