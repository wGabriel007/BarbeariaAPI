using Barbearia.Domain.Comum;
using Barbearia.Domain.Entidades;
using Barbearia.Domain.Enumeracoes;
using Xunit;

namespace Barbearia.Tests.Dominio;

/// <summary>
/// Testa a máquina de estados de Agendamento isoladamente — sem banco,
/// sem repositório, sem EF Core. É exatamente o tipo de teste que uma
/// Entity de Domain permite escrever: rápido (roda em milissegundos) e
/// não quebra se o banco de dados estiver fora do ar.
/// </summary>
public class AgendamentoTests
{
    private static Agendamento FnCriarAgendamentoValido() => Agendamento.FnCriar(
        clienteId: 1,
        barbeiroId: 1,
        servicoId: 1,
        inicio: new DateTimeOffset(2026, 9, 20, 14, 0, 0, TimeSpan.FromHours(-3)),
        fim: new DateTimeOffset(2026, 9, 20, 14, 30, 0, TimeSpan.FromHours(-3)),
        precoCobrado: 40m);

    [Fact]
    public void FnCriar_ComDadosValidos_ComecaComStatusAgendado()
    {
        var agendamento = FnCriarAgendamentoValido();

        Assert.Equal(StatusAgendamento.Agendado, agendamento.Status);
    }

    [Fact]
    public void FnCriar_ComFimAntesDoInicio_LancaDomainException()
    {
        var inicio = new DateTimeOffset(2026, 9, 20, 14, 0, 0, TimeSpan.FromHours(-3));
        var fimAntesDoInicio = inicio.AddMinutes(-10);

        Assert.Throws<DomainException>(() =>
            Agendamento.FnCriar(1, 1, 1, inicio, fimAntesDoInicio, precoCobrado: 40m));
    }

    [Fact]
    public void FnCriar_ComPrecoNegativo_LancaDomainException()
    {
        var inicio = new DateTimeOffset(2026, 9, 20, 14, 0, 0, TimeSpan.FromHours(-3));

        Assert.Throws<DomainException>(() =>
            Agendamento.FnCriar(1, 1, 1, inicio, inicio.AddMinutes(30), precoCobrado: -1m));
    }

    [Fact]
    public void FnFluxoFeliz_AgendadoAteConcluido_FuncionaNaOrdemCerta()
    {
        var agendamento = FnCriarAgendamentoValido();

        agendamento.FnConfirmar();
        Assert.Equal(StatusAgendamento.Confirmado, agendamento.Status);

        agendamento.FnIniciarAtendimento();
        Assert.Equal(StatusAgendamento.EmAtendimento, agendamento.Status);

        agendamento.FnConcluir();
        Assert.Equal(StatusAgendamento.Concluido, agendamento.Status);
    }

    [Fact]
    public void FnConcluir_SemPassarPorEmAtendimento_LancaDomainException()
    {
        var agendamento = FnCriarAgendamentoValido();
        agendamento.FnConfirmar();

        // Ainda está "Confirmado", não "EmAtendimento" — FnConcluir direto
        // não é permitido, tem que passar por FnIniciarAtendimento() antes.
        Assert.Throws<DomainException>(agendamento.FnConcluir);
    }

    [Fact]
    public void FnCancelar_UmAgendamentoJaConcluido_LancaDomainException()
    {
        var agendamento = FnCriarAgendamentoValido();
        agendamento.FnConfirmar();
        agendamento.FnIniciarAtendimento();
        agendamento.FnConcluir();

        // 'agendamento.FnCancelar' (método com um parâmetro opcional) não
        // converte mais direto pra Action desde que FnCancelar passou a
        // aceitar 'mensagem' — por isso o lambda em vez de passar o
        // método group direto (mesmo motivo no teste de NaoCompareceu).
        Assert.Throws<DomainException>(() => agendamento.FnCancelar());
    }

    [Fact]
    public void FnCancelar_UmAgendamentoJaCancelado_EhIdempotente()
    {
        var agendamento = FnCriarAgendamentoValido();
        agendamento.FnCancelar();

        // Chamar de novo não deve lançar exceção — é o comportamento
        // documentado em Agendamento.FnCancelar().
        agendamento.FnCancelar();

        Assert.Equal(StatusAgendamento.Cancelado, agendamento.Status);
    }

    [Theory]
    [InlineData(StatusAgendamento.Agendado)]
    [InlineData(StatusAgendamento.Confirmado)]
    public void FnMarcarNaoCompareceu_APartirDeAgendadoOuConfirmado_Funciona(StatusAgendamento statusInicial)
    {
        var agendamento = FnCriarAgendamentoValido();
        if (statusInicial == StatusAgendamento.Confirmado)
            agendamento.FnConfirmar();

        agendamento.FnMarcarNaoCompareceu();

        Assert.Equal(StatusAgendamento.NaoCompareceu, agendamento.Status);
    }

    [Fact]
    public void FnMarcarNaoCompareceu_ApartirDeEmAtendimento_LancaDomainException()
    {
        var agendamento = FnCriarAgendamentoValido();
        agendamento.FnConfirmar();
        agendamento.FnIniciarAtendimento();

        Assert.Throws<DomainException>(() => agendamento.FnMarcarNaoCompareceu());
    }

    // --- Fluxo de SOLICITAÇÃO (cliente Comum pedindo horário) ----------
    // Diferente de FnCriar (usado pelo staff, nasce Agendado), FnSolicitar
    // nasce Pendente e só chega a Confirmado depois que um Admin/Barbeiro
    // aprova — ou vira Rejeitado, se recusarem.

    private static Agendamento FnCriarSolicitacaoValida() => Agendamento.FnSolicitar(
        clienteId: 1,
        barbeiroId: 1,
        servicoId: 1,
        inicio: new DateTimeOffset(2026, 9, 20, 14, 0, 0, TimeSpan.FromHours(-3)),
        fim: new DateTimeOffset(2026, 9, 20, 14, 30, 0, TimeSpan.FromHours(-3)),
        precoCobrado: 40m);

    [Fact]
    public void FnSolicitar_ComDadosValidos_ComecaComStatusPendente()
    {
        var solicitacao = FnCriarSolicitacaoValida();

        Assert.Equal(StatusAgendamento.Pendente, solicitacao.Status);
    }

    [Fact]
    public void FnConfirmar_UmaSolicitacaoPendente_VaiParaConfirmadoEGuardaMensagem()
    {
        var solicitacao = FnCriarSolicitacaoValida();

        solicitacao.FnConfirmar("Combinado! Te espero lá.");

        Assert.Equal(StatusAgendamento.Confirmado, solicitacao.Status);
        Assert.Equal("Combinado! Te espero lá.", solicitacao.MensagemResposta);
    }

    [Fact]
    public void FnRejeitar_UmaSolicitacaoPendente_VaiParaRejeitadoEGuardaMensagem()
    {
        var solicitacao = FnCriarSolicitacaoValida();

        solicitacao.FnRejeitar("Não vou poder nesse horário.");

        Assert.Equal(StatusAgendamento.Rejeitado, solicitacao.Status);
        Assert.Equal("Não vou poder nesse horário.", solicitacao.MensagemResposta);
    }

    [Fact]
    public void FnRejeitar_UmAgendamentoQueNaoEhPendente_LancaDomainException()
    {
        // Um agendamento criado direto pelo staff (Agendado) não tem o
        // que "rejeitar" — só uma solicitação (Pendente) do cliente.
        var agendamento = FnCriarAgendamentoValido();

        Assert.Throws<DomainException>(() => agendamento.FnRejeitar());
    }

    [Fact]
    public void FnConfirmar_UmAgendadoDireto_TambemFunciona()
    {
        // FnConfirmar aceita tanto Agendado (fluxo do staff) quanto
        // Pendente (fluxo do cliente) — ver Agendamento.FnConfirmar.
        var agendamento = FnCriarAgendamentoValido();

        agendamento.FnConfirmar();

        Assert.Equal(StatusAgendamento.Confirmado, agendamento.Status);
    }

    [Fact]
    public void FnCancelar_UmaSolicitacaoPendente_Funciona()
    {
        // O próprio cliente desistindo do pedido antes de ser confirmado.
        var solicitacao = FnCriarSolicitacaoValida();

        solicitacao.FnCancelar();

        Assert.Equal(StatusAgendamento.Cancelado, solicitacao.Status);
    }
}
