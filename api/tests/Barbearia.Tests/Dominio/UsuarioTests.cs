using Barbearia.Domain.Comum;
using Barbearia.Domain.Entidades;
using Barbearia.Domain.Enumeracoes;
using Xunit;

namespace Barbearia.Tests.Dominio;

public class UsuarioTests
{
    private static Usuario FnCriarComum() =>
        Usuario.FnCriar("Maria Comum", "maria@example.com", "hash-qualquer", TipoUsuario.Comum);

    [Fact]
    public void FnPromoverParaBarbeiro_UsuarioComum_MudaTipo()
    {
        var usuario = FnCriarComum();

        usuario.FnPromoverParaBarbeiro();

        Assert.Equal(TipoUsuario.Barbeiro, usuario.Tipo);
    }

    [Fact]
    public void FnPromoverParaBarbeiro_UsuarioJaBarbeiro_LancaDomainException()
    {
        var usuario = FnCriarComum();
        usuario.FnPromoverParaBarbeiro();

        Assert.Throws<DomainException>(usuario.FnPromoverParaBarbeiro);
    }

    [Fact]
    public void FnPromoverParaBarbeiro_Admin_LancaDomainException()
    {
        var admin = Usuario.FnCriar("Dono", "dono@example.com", "hash-qualquer", TipoUsuario.Admin);

        Assert.Throws<DomainException>(admin.FnPromoverParaBarbeiro);
    }
}
