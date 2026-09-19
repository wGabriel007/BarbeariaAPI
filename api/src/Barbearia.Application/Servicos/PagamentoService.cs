using Barbearia.Application.Abstracoes;
using Barbearia.Application.Comum;
using Barbearia.Application.Dtos.Pagamentos;
using Barbearia.Domain.Entidades;

namespace Barbearia.Application.Servicos;

/// <summary>
/// Pagamentos nascem sozinhos (ver AgendamentoService.FnConcluirAsync) —
/// este Service não tem "CriarAsync" de propósito, só consulta e as duas
/// ações que o barbeiro toma em cima de um pagamento Pendente: confirmar
/// (cliente pagou) ou cancelar (não pagou / ficou devendo).
/// </summary>
public class PagamentoService
{
    private readonly IPagamentoRepository _repositorio;
    private readonly IUnitOfWork _uow;

    public PagamentoService(IPagamentoRepository repositorio, IUnitOfWork uow)
    {
        _repositorio = repositorio;
        _uow = uow;
    }

    /// <summary>
    /// GET /api/pagamentos?de=&amp;ate= — mesma forma de consulta por
    /// período que já existe em Agendamentos (ver
    /// IAgendamentoRepository.FnListarPorBarbeiroEPeriodoAsync): o
    /// front decide o intervalo (um dia = "hoje", vários dias =
    /// histórico) e manda como DateTimeOffset já no fuso do navegador.
    /// </summary>
    public async Task<List<PagamentoResponse>> FnListarPorPeriodoAsync(DateTimeOffset de, DateTimeOffset ate, CancellationToken ct = default)
    {
        var pagamentos = await _repositorio.FnListarPorPeriodoAsync(de, ate, ct);
        return pagamentos.Select(FnMapear).ToList();
    }

    /// <summary>
    /// GET /api/pagamentos/por-cliente/5 — histórico de pagamentos (qualquer
    /// status) de UM cliente, pro "cartão do cliente" da aba Usuários (ver
    /// ClienteService.FnObterDetalhePorUsuarioIdAsync). Diferente de
    /// FnListarPorPeriodoAsync (que corta por data, pra "hoje"/"histórico"
    /// dos últimos N dias), aqui é sempre TUDO daquele cliente — não tem
    /// "hoje" quando o assunto é a vida inteira de UM cliente só.
    /// </summary>
    public async Task<List<PagamentoResponse>> FnListarPorClienteAsync(long clienteId, CancellationToken ct = default)
    {
        var pagamentos = await _repositorio.FnListarPorClienteAsync(clienteId, ct);
        return pagamentos.Select(FnMapear).ToList();
    }

    public async Task<PagamentoResponse> FnConfirmarAsync(long id, ConfirmarPagamentoRequest request, CancellationToken ct = default)
    {
        var pagamento = await _repositorio.FnObterPorIdAsync(id, ct)
            ?? throw NotFoundException.FnPara("Pagamento", id);

        // UtcNow, não Now: o Npgsql (versão em uso) só aceita gravar
        // DateTimeOffset com offset 0 num "timestamp with time zone" —
        // um DateTimeOffset.Now no fuso local (-03:00 no Brasil) quebra
        // o INSERT/UPDATE com "only offset 0 (UTC) is supported". O valor
        // salvo continua representando o mesmo instante; é só convertido
        // pro fuso do navegador na hora de exibir, como em qualquer outra
        // data do sistema.
        pagamento.FnConfirmarPagamento(DateTimeOffset.UtcNow, request.Forma);
        await _uow.FnSalvarAsync(ct);

        return FnMapear(pagamento);
    }

    public async Task<PagamentoResponse> FnCancelarAsync(long id, CancellationToken ct = default)
    {
        var pagamento = await _repositorio.FnObterPorIdAsync(id, ct)
            ?? throw NotFoundException.FnPara("Pagamento", id);

        pagamento.FnCancelar();
        await _uow.FnSalvarAsync(ct);

        return FnMapear(pagamento);
    }

    private static PagamentoResponse FnMapear(Pagamento p) => new(
        p.Id, p.AgendamentoId, p.AssinaturaId, p.ClienteId, p.Valor, p.Forma, p.Status, p.PagoEm, p.CriadoEm);
}
