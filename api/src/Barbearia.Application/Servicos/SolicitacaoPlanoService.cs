using Barbearia.Application.Abstracoes;
using Barbearia.Application.Comum;
using Barbearia.Application.Dtos.Planos;
using Barbearia.Domain.Comum;
using Barbearia.Domain.Entidades;
using Barbearia.Domain.Enumeracoes;

namespace Barbearia.Application.Servicos;

/// <summary>
/// Fluxo do CLIENTE (Comum) pedindo pra assinar um plano — pedido nasce
/// Pendente (ver SolicitacaoPlano.FnCriar) e espera um Admin/Barbeiro
/// aceitar ou rejeitar na aba "Solicitações", igual ao fluxo de
/// agendamento (ver AgendamentoService). FnAceitar aqui é mais envolvido
/// que aceitar um agendamento: além de mudar o status do pedido, precisa
/// (1) garantir que o usuário já tem um Cliente (auto-provisiona se
/// ainda não tiver — mesma ideia de
/// AgendamentoService.FnObterOuCriarClienteDoUsuarioAsync), (2) atualizar
/// o contato desse Cliente com o e-mail/telefone digitados no pedido, e
/// (3) criar a Assinatura de verdade com as datas que o Admin informou
/// ao aceitar.
/// </summary>
public class SolicitacaoPlanoService
{
    private readonly ISolicitacaoPlanoRepository _solicitacoes;
    private readonly IPlanoAssinaturaRepository _planos;
    private readonly IUsuarioRepository _usuarios;
    private readonly IClienteRepository _clientes;
    private readonly IAssinaturaRepository _assinaturas;
    private readonly IUnitOfWork _uow;

    public SolicitacaoPlanoService(
        ISolicitacaoPlanoRepository solicitacoes,
        IPlanoAssinaturaRepository planos,
        IUsuarioRepository usuarios,
        IClienteRepository clientes,
        IAssinaturaRepository assinaturas,
        IUnitOfWork uow)
    {
        _solicitacoes = solicitacoes;
        _planos = planos;
        _usuarios = usuarios;
        _clientes = clientes;
        _assinaturas = assinaturas;
        _uow = uow;
    }

    public async Task<SolicitacaoPlanoResponse> FnSolicitarAsync(long usuarioId, SolicitarPlanoRequest request, CancellationToken ct = default)
    {
        // Só um pedido em aberto por vez — enquanto o de agora não for
        // Aceito/Rejeitado pelo staff, não dá pra pedir outro (nem outro
        // plano, nem o mesmo de novo).
        if (await _solicitacoes.FnExistePendentePorUsuarioIdAsync(usuarioId, ct))
            throw new DomainException("Você já tem uma solicitação de plano pendente. Aguarde a resposta da equipe de suporte antes de pedir outro.");

        var plano = await _planos.FnObterPorIdAsync(request.PlanoId, ct)
            ?? throw NotFoundException.FnPara("Plano", request.PlanoId);

        if (plano.Status != StatusRegistro.Ativo)
            throw new DomainException("Este plano não está mais disponível.");

        var solicitacao = SolicitacaoPlano.FnCriar(usuarioId, request.PlanoId, request.Email, request.Telefone);

        await _solicitacoes.FnAdicionarAsync(solicitacao, ct);
        await _uow.FnSalvarAsync(ct);

        return await FnMapearAsync(solicitacao, ct);
    }

    /// <summary>"Aba de solicitações" do staff.</summary>
    public async Task<List<SolicitacaoPlanoResponse>> FnListarPendentesAsync(CancellationToken ct = default)
    {
        var lista = await _solicitacoes.FnListarPendentesAsync(ct);
        return await FnMapearTodasAsync(lista, ct);
    }

    /// <summary>"Meus planos" do usuário logado — todos os pedidos que já fez, qualquer status.</summary>
    public async Task<List<SolicitacaoPlanoResponse>> FnListarMeusAsync(long usuarioId, CancellationToken ct = default)
    {
        var lista = await _solicitacoes.FnListarPorUsuarioIdAsync(usuarioId, ct);
        return await FnMapearTodasAsync(lista, ct);
    }

    public async Task<SolicitacaoPlanoResponse> FnAceitarAsync(long id, AceitarSolicitacaoPlanoRequest request, CancellationToken ct = default)
    {
        var solicitacao = await _solicitacoes.FnObterPorIdAsync(id, ct)
            ?? throw NotFoundException.FnPara("Solicitação de plano", id);

        var cliente = await FnObterOuCriarClienteDoUsuarioAsync(solicitacao.UsuarioId, ct);

        // O e-mail/telefone do PEDIDO (não os que já estavam na conta, se
        // é que tinha algum) passam a valer pro Cliente — é o único jeito
        // de um Cliente auto-provisionado ganhar telefone sem passar pela
        // tela Clientes (ver comentário em SolicitarPlanoRequest).
        cliente.FnAtualizarDados(cliente.NomeCompleto, solicitacao.Telefone, solicitacao.Email, cliente.Observacoes);

        var assinatura = Assinatura.FnCriar(cliente.Id, solicitacao.PlanoId, request.DataInicio, request.DataVencimento);
        await _assinaturas.FnAdicionarAsync(assinatura, ct);
        await _uow.FnSalvarAsync(ct); // precisa do Id gerado da assinatura antes de usar em FnAceitar

        solicitacao.FnAceitar(assinatura.Id);
        await _uow.FnSalvarAsync(ct);

        return await FnMapearAsync(solicitacao, ct);
    }

    public async Task<SolicitacaoPlanoResponse> FnRejeitarAsync(long id, RejeitarSolicitacaoPlanoRequest request, CancellationToken ct = default)
    {
        var solicitacao = await _solicitacoes.FnObterPorIdAsync(id, ct)
            ?? throw NotFoundException.FnPara("Solicitação de plano", id);

        solicitacao.FnRejeitar(request.Mensagem);
        await _uow.FnSalvarAsync(ct);

        return await FnMapearAsync(solicitacao, ct);
    }

    /// <summary>
    /// Mesmo papel de AgendamentoService.FnObterOuCriarClienteDoUsuarioAsync
    /// (duplicado aqui de propósito — os dois Services não dependem um do
    /// outro, cada camada de Application só conhece Abstractions): um
    /// usuário Comum tem no máximo UM Cliente ligado a ele.
    /// </summary>
    private async Task<Cliente> FnObterOuCriarClienteDoUsuarioAsync(long usuarioId, CancellationToken ct)
    {
        var cliente = await _clientes.FnObterPorUsuarioIdAsync(usuarioId, ct);
        if (cliente is not null)
            return cliente;

        var usuario = await _usuarios.FnObterPorIdAsync(usuarioId, ct)
            ?? throw NotFoundException.FnPara("Usuario", usuarioId);

        cliente = Cliente.FnCriar(usuario.NomeCompleto, telefone: null, email: usuario.Email, usuarioId: usuarioId);
        await _clientes.FnAdicionarAsync(cliente, ct);
        await _uow.FnSalvarAsync(ct);

        return cliente;
    }

    // Sem navegação Usuario/Plano na entidade (ver comentário em
    // SolicitacaoPlano) — busca os dois só pra exibir nome, direto aqui.
    // A lista de pedidos nunca é grande (é sempre "os pendentes" ou "os
    // meus"), então N consultas extras não pesa.
    private async Task<SolicitacaoPlanoResponse> FnMapearAsync(SolicitacaoPlano s, CancellationToken ct)
    {
        var usuario = await _usuarios.FnObterPorIdAsync(s.UsuarioId, ct);
        var plano = await _planos.FnObterPorIdAsync(s.PlanoId, ct);

        return new SolicitacaoPlanoResponse(
            s.Id,
            s.UsuarioId,
            usuario?.NomeCompleto ?? $"Usuário #{s.UsuarioId}",
            s.PlanoId,
            plano?.Nome ?? $"Plano #{s.PlanoId}",
            s.Email,
            s.Telefone,
            s.Status,
            s.MensagemResposta,
            s.AssinaturaId,
            s.CriadoEm,
            s.AtualizadoEm);
    }

    private async Task<List<SolicitacaoPlanoResponse>> FnMapearTodasAsync(List<SolicitacaoPlano> lista, CancellationToken ct)
    {
        var resultado = new List<SolicitacaoPlanoResponse>();
        foreach (var s in lista)
            resultado.Add(await FnMapearAsync(s, ct));

        return resultado;
    }
}
