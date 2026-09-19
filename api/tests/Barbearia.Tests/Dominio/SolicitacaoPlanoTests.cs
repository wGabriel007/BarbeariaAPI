using Barbearia.Domain.Comum;
using Barbearia.Domain.Entidades;
using Barbearia.Domain.Enumeracoes;
using Xunit;

namespace Barbearia.Tests.Dominio;

/// <summary>
/// Testa a máquina de estados de SolicitacaoPlano isoladamente — sem
/// banco, sem repositório, sem EF Core. Mesmo espírito de
/// AgendamentoTests: o pedido nasce Pendente e só sai desse estado por
/// FnAceitar ou FnRejeitar (decisão do Admin/Barbeiro), nunca dos dois.
/// </summary>
public class SolicitacaoPlanoTests
{
    private static SolicitacaoPlano FnCriarSolicitacaoValida() => SolicitacaoPlano.FnCriar(
        usuarioId: 1,
        planoId: 1,
        email: "cliente@exemplo.com",
        telefone: "11999998888");

    [Fact]
    public void FnCriar_ComDadosValidos_ComecaComStatusPendente()
    {
        var solicitacao = FnCriarSolicitacaoValida();

        Assert.Equal(StatusSolicitacaoPlano.Pendente, solicitacao.Status);
        Assert.Null(solicitacao.AssinaturaId);
        Assert.Null(solicitacao.MensagemResposta);
    }

    [Fact]
    public void FnCriar_ComUsuarioIdInvalido_LancaDomainException()
    {
        Assert.Throws<DomainException>(() =>
            SolicitacaoPlano.FnCriar(usuarioId: 0, planoId: 1, email: "a@a.com", telefone: "11999998888"));
    }

    [Fact]
    public void FnCriar_ComPlanoIdInvalido_LancaDomainException()
    {
        Assert.Throws<DomainException>(() =>
            SolicitacaoPlano.FnCriar(usuarioId: 1, planoId: 0, email: "a@a.com", telefone: "11999998888"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void FnCriar_ComEmailEmBranco_LancaDomainException(string? email)
    {
        Assert.Throws<DomainException>(() =>
            SolicitacaoPlano.FnCriar(usuarioId: 1, planoId: 1, email: email!, telefone: "11999998888"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void FnCriar_ComTelefoneEmBranco_LancaDomainException(string? telefone)
    {
        Assert.Throws<DomainException>(() =>
            SolicitacaoPlano.FnCriar(usuarioId: 1, planoId: 1, email: "a@a.com", telefone: telefone!));
    }

    [Fact]
    public void FnAceitar_UmaSolicitacaoPendente_VaiParaAceitaEGuardaAssinaturaEMensagem()
    {
        var solicitacao = FnCriarSolicitacaoValida();

        solicitacao.FnAceitar(assinaturaId: 42, mensagem: "Bem-vindo!");

        Assert.Equal(StatusSolicitacaoPlano.Aceita, solicitacao.Status);
        Assert.Equal(42, solicitacao.AssinaturaId);
        Assert.Equal("Bem-vindo!", solicitacao.MensagemResposta);
    }

    [Fact]
    public void FnAceitar_SemMensagem_FuncionaComMensagemNula()
    {
        var solicitacao = FnCriarSolicitacaoValida();

        solicitacao.FnAceitar(assinaturaId: 42);

        Assert.Equal(StatusSolicitacaoPlano.Aceita, solicitacao.Status);
        Assert.Null(solicitacao.MensagemResposta);
    }

    [Fact]
    public void FnRejeitar_UmaSolicitacaoPendente_VaiParaRejeitadaEGuardaMensagem()
    {
        var solicitacao = FnCriarSolicitacaoValida();

        solicitacao.FnRejeitar("No momento não temos vaga nesse plano.");

        Assert.Equal(StatusSolicitacaoPlano.Rejeitada, solicitacao.Status);
        Assert.Null(solicitacao.AssinaturaId);
        Assert.Equal("No momento não temos vaga nesse plano.", solicitacao.MensagemResposta);
    }

    [Fact]
    public void FnAceitar_UmaSolicitacaoJaAceita_LancaDomainException()
    {
        var solicitacao = FnCriarSolicitacaoValida();
        solicitacao.FnAceitar(assinaturaId: 42);

        Assert.Throws<DomainException>(() => solicitacao.FnAceitar(assinaturaId: 99));
    }

    [Fact]
    public void FnRejeitar_UmaSolicitacaoJaRejeitada_LancaDomainException()
    {
        var solicitacao = FnCriarSolicitacaoValida();
        solicitacao.FnRejeitar();

        Assert.Throws<DomainException>(() => solicitacao.FnRejeitar());
    }

    [Fact]
    public void FnRejeitar_UmaSolicitacaoJaAceita_LancaDomainException()
    {
        // Uma vez decidida, a decisão é definitiva — não dá pra "voltar
        // atrás" trocando Aceita por Rejeitada nem vice-versa.
        var solicitacao = FnCriarSolicitacaoValida();
        solicitacao.FnAceitar(assinaturaId: 42);

        Assert.Throws<DomainException>(() => solicitacao.FnRejeitar());
    }

    [Fact]
    public void FnAceitar_UmaSolicitacaoJaRejeitada_LancaDomainException()
    {
        var solicitacao = FnCriarSolicitacaoValida();
        solicitacao.FnRejeitar();

        Assert.Throws<DomainException>(() => solicitacao.FnAceitar(assinaturaId: 42));
    }
}
