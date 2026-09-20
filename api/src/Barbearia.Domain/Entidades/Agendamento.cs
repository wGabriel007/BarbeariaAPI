using Barbearia.Domain.Comum;
using Barbearia.Domain.Enumeracoes;

namespace Barbearia.Domain.Entidades;

public class Agendamento : AuditableEntity
{
    public long ClienteId { get; private set; }
    public long BarbeiroId { get; private set; }
    public long ServicoId { get; private set; }
    public long? AssinaturaId { get; private set; }
    public DateTimeOffset Inicio { get; private set; }
    public DateTimeOffset Fim { get; private set; }
    public StatusAgendamento Status { get; private set; }
    public decimal PrecoCobrado { get; private set; }
    public string? Observacoes { get; private set; }

    /// <summary>
    /// Recado opcional que o Admin/Barbeiro deixa pro cliente ao
    /// confirmar, rejeitar, cancelar ou marcar não-compareceu (ex.: "Só
    /// consigo te atender às 14h, tudo bem?"). Sempre reflete a ÚLTIMA
    /// transição que carregou uma mensagem — não é um histórico.
    /// </summary>
    public string? MensagemResposta { get; private set; }

    /// <summary>Barbearia (Empresa) dona deste agendamento — sempre igual ao EmpresaId do Cliente (ver FnAtribuirEmpresa).</summary>
    public long EmpresaId { get; private set; }

    private Agendamento()
    {
    }

    /// <summary>
    /// Usado quando quem cria o agendamento é o próprio STAFF (walk-in,
    /// telefone) — nasce direto como Agendado, sem passar por
    /// aprovação (é a própria pessoa que atende decidindo). Ver
    /// FnSolicitar() para o fluxo do cliente (Comum) pedindo um horário.
    /// </summary>
    public static Agendamento FnCriar(
        long clienteId,
        long barbeiroId,
        long servicoId,
        DateTimeOffset inicio,
        DateTimeOffset fim,
        decimal precoCobrado,
        long? assinaturaId = null,
        string? observacoes = null) =>
        FnCriarInterno(clienteId, barbeiroId, servicoId, inicio, fim, precoCobrado, StatusAgendamento.Agendado, assinaturaId, observacoes);

    /// <summary>
    /// Usado quando quem cria é o CLIENTE (Comum) solicitando um
    /// horário — nasce Pendente, e só passa a valer de fato depois que
    /// um Admin/Barbeiro confirma (ver FnConfirmar/FnRejeitar). A validação
    /// de que o horário pedido cabe dentro do expediente do barbeiro
    /// não é feita aqui (o Domain não conhece HorarioTrabalho de outro
    /// agregado) — isso é responsabilidade da Application
    /// (AgendamentoService.FnSolicitarAsync).
    /// </summary>
    public static Agendamento FnSolicitar(
        long clienteId,
        long barbeiroId,
        long servicoId,
        DateTimeOffset inicio,
        DateTimeOffset fim,
        decimal precoCobrado,
        long? assinaturaId = null,
        string? observacoes = null) =>
        FnCriarInterno(clienteId, barbeiroId, servicoId, inicio, fim, precoCobrado, StatusAgendamento.Pendente, assinaturaId, observacoes);

    private static Agendamento FnCriarInterno(
        long clienteId,
        long barbeiroId,
        long servicoId,
        DateTimeOffset inicio,
        DateTimeOffset fim,
        decimal precoCobrado,
        StatusAgendamento statusInicial,
        long? assinaturaId,
        string? observacoes)
    {
        if (clienteId <= 0)
            throw new DomainException("ClienteId inválido.");

        if (barbeiroId <= 0)
            throw new DomainException("BarbeiroId inválido.");

        if (servicoId <= 0)
            throw new DomainException("ServicoId inválido.");

        if (fim <= inicio)
            throw new DomainException("Fim do agendamento precisa ser depois do início.");

        if (precoCobrado < 0)
            throw new DomainException("Preço cobrado não pode ser negativo.");

        // Nota importante: esta classe NÃO checa se o barbeiro já tem
        // outro agendamento nesse horário — isso exige consultar o
        // banco, o que o Domain não faz (ele não conhece repositórios
        // nem o EF Core). Essa checagem entra na Application (Fase 2,
        // próximo passo), e o banco garante o mesmo com o EXCLUDE
        // constraint em agendamentos (01_schema.sql) como segunda
        // camada de defesa, inclusive contra condição de corrida.

        return new Agendamento
        {
            ClienteId = clienteId,
            BarbeiroId = barbeiroId,
            ServicoId = servicoId,
            AssinaturaId = assinaturaId,
            Inicio = inicio,
            Fim = fim,
            PrecoCobrado = precoCobrado,
            Observacoes = observacoes,
            Status = statusInicial
        };
    }

    // --- Máquina de estados -------------------------------------------
    // Fluxo do staff:  Agendado  -> Confirmado -> EmAtendimento -> Concluido
    // Fluxo do cliente: Pendente -> Confirmado -> EmAtendimento -> Concluido
    //                            -> Rejeitado (em vez de Confirmado)
    // Desvios possíveis a partir de Agendado/Confirmado/Pendente:
    // Cancelado ou NaoCompareceu. Uma vez Concluido/Cancelado/
    // NaoCompareceu/Rejeitado, o agendamento é definitivo — nenhum
    // método de transição aceita sair desses estados.

    public void FnConfirmar(string? mensagem = null)
    {
        if (Status is not (StatusAgendamento.Agendado or StatusAgendamento.Pendente))
            throw new DomainException(
                $"Não é possível confirmar: o agendamento está com status {Status}, esperado Agendado ou Pendente.");

        Status = StatusAgendamento.Confirmado;
        MensagemResposta = mensagem;
    }

    /// <summary>Só faz sentido para uma solicitação do cliente (Pendente) — um agendamento que o staff já criou direto (Agendado) não tem o que "rejeitar".</summary>
    public void FnRejeitar(string? mensagem = null)
    {
        FnGarantirEstadoAtual(StatusAgendamento.Pendente, nameof(FnRejeitar));
        Status = StatusAgendamento.Rejeitado;
        MensagemResposta = mensagem;
    }

    public void FnIniciarAtendimento()
    {
        FnGarantirEstadoAtual(StatusAgendamento.Confirmado, nameof(FnIniciarAtendimento));
        Status = StatusAgendamento.EmAtendimento;
    }

    public void FnConcluir()
    {
        FnGarantirEstadoAtual(StatusAgendamento.EmAtendimento, nameof(FnConcluir));
        Status = StatusAgendamento.Concluido;
    }

    public void FnCancelar(string? mensagem = null)
    {
        if (Status is StatusAgendamento.Concluido)
            throw new DomainException("Não é possível cancelar um agendamento já concluído.");

        if (Status is StatusAgendamento.Cancelado or StatusAgendamento.NaoCompareceu or StatusAgendamento.Rejeitado)
            return; // idempotente: cancelar de novo algo já cancelado/rejeitado não é erro

        Status = StatusAgendamento.Cancelado;
        if (mensagem is not null)
            MensagemResposta = mensagem;
    }

    public void FnMarcarNaoCompareceu(string? mensagem = null)
    {
        if (Status is not (StatusAgendamento.Agendado or StatusAgendamento.Confirmado))
            throw new DomainException($"Não é possível marcar 'não compareceu' a partir do status {Status}.");

        Status = StatusAgendamento.NaoCompareceu;
        if (mensagem is not null)
            MensagemResposta = mensagem;
    }

    private void FnGarantirEstadoAtual(StatusAgendamento esperado, string operacao)
    {
        if (Status != esperado)
            throw new DomainException(
                $"Não é possível executar '{operacao}': o agendamento está com status {Status}, esperado {esperado}.");
    }

    /// <summary>Chamado uma única vez, logo após FnCriar/FnSolicitar, sempre com o EmpresaId do Cliente que pede o agendamento.</summary>
    public void FnAtribuirEmpresa(long empresaId)
    {
        if (EmpresaId != 0)
            throw new DomainException("Este agendamento já pertence a uma barbearia.");

        if (empresaId <= 0)
            throw new DomainException("EmpresaId inválido.");

        EmpresaId = empresaId;
    }
}
