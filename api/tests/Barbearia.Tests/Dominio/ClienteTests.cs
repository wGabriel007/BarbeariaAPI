using Barbearia.Domain.Comum;
using Barbearia.Domain.Entidades;
using Barbearia.Domain.Enumeracoes;
using Xunit;

namespace Barbearia.Tests.Dominio;

public class ClienteTests
{
    [Fact]
    public void FnCriar_ComDadosValidos_ComecaAtivoETrimado()
    {
        var cliente = Cliente.FnCriar("  João da Silva  ", " 11999990000 ", "joao@example.com");

        Assert.Equal("João da Silva", cliente.NomeCompleto);
        Assert.Equal("11999990000", cliente.Telefone);
        Assert.Equal(StatusRegistro.Ativo, cliente.Status);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void FnCriar_SemNome_LancaDomainException(string nome)
    {
        Assert.Throws<DomainException>(() => Cliente.FnCriar(nome, "11999990000"));
    }

    // Telefone deixou de ser obrigatório — um Cliente auto-provisionado a
    // partir de um usuário Comum (ver Cliente.UsuarioId /
    // AgendamentoService.FnSolicitarAsync) nasce sem telefone nenhum.
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void FnCriar_SemTelefone_NaoLancaExcecaoEGuardaComoNulo(string? telefone)
    {
        var cliente = Cliente.FnCriar("João", telefone);

        Assert.Null(cliente.Telefone);
    }

    [Fact]
    public void FnCriar_LigadoAUmUsuario_GuardaUsuarioId()
    {
        var cliente = Cliente.FnCriar("João", telefone: null, usuarioId: 42);

        Assert.Equal(42, cliente.UsuarioId);
    }

    [Fact]
    public void FnCriar_ComEmailEmBranco_GuardaComoNulo()
    {
        var cliente = Cliente.FnCriar("João", "11999990000", email: "   ");

        Assert.Null(cliente.Email);
    }

    [Fact]
    public void FnBloquear_DepoisAtivar_VoltaParaAtivo()
    {
        var cliente = Cliente.FnCriar("João", "11999990000");

        cliente.FnBloquear();
        Assert.Equal(StatusRegistro.Bloqueado, cliente.Status);

        cliente.FnAtivar();
        Assert.Equal(StatusRegistro.Ativo, cliente.Status);
    }
}
