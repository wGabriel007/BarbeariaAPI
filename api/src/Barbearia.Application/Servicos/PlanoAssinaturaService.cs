using Barbearia.Application.Abstracoes;
using Barbearia.Application.Comum;
using Barbearia.Application.Dtos.Planos;
using Barbearia.Domain.Entidades;

namespace Barbearia.Application.Servicos;

public class PlanoAssinaturaService
{
    private readonly IPlanoAssinaturaRepository _planos;
    private readonly IServicoRepository _servicos;
    private readonly IUnitOfWork _uow;

    public PlanoAssinaturaService(IPlanoAssinaturaRepository planos, IServicoRepository servicos, IUnitOfWork uow)
    {
        _planos = planos;
        _servicos = servicos;
        _uow = uow;
    }

    public async Task<PlanoResponse> FnCriarAsync(CriarPlanoRequest request, CancellationToken ct = default)
    {
        var plano = PlanoAssinatura.FnCriar(request.Nome, request.PrecoMensal, request.Descricao);

        await _planos.FnAdicionarAsync(plano, ct);
        await _uow.FnSalvarAsync(ct);

        return FnMapear(plano);
    }

    public async Task<PlanoResponse> FnIncluirServicoAsync(long planoId, IncluirServicoNoPlanoRequest request, CancellationToken ct = default)
    {
        var plano = await _planos.FnObterComServicosAsync(planoId, ct)
            ?? throw NotFoundException.FnPara("Plano", planoId);

        _ = await _servicos.FnObterPorIdAsync(request.ServicoId, ct)
            ?? throw NotFoundException.FnPara("Serviço", request.ServicoId);

        plano.FnIncluirServico(request.ServicoId, request.LimiteMensal);
        await _uow.FnSalvarAsync(ct);

        return FnMapear(plano);
    }

    public async Task<PlanoResponse> FnObterPorIdAsync(long id, CancellationToken ct = default)
    {
        var plano = await _planos.FnObterComServicosAsync(id, ct)
            ?? throw NotFoundException.FnPara("Plano", id);

        return FnMapear(plano);
    }

    public async Task<List<PlanoResponse>> FnListarAsync(CancellationToken ct = default)
    {
        var planos = await _planos.FnListarAsync(ct);
        return planos.Select(FnMapear).ToList();
    }

    public async Task FnInativarAsync(long id, CancellationToken ct = default)
    {
        var plano = await _planos.FnObterPorIdAsync(id, ct)
            ?? throw NotFoundException.FnPara("Plano", id);

        plano.FnInativar();
        await _uow.FnSalvarAsync(ct);
    }

    public async Task FnAtivarAsync(long id, CancellationToken ct = default)
    {
        var plano = await _planos.FnObterPorIdAsync(id, ct)
            ?? throw NotFoundException.FnPara("Plano", id);

        plano.FnAtivar();
        await _uow.FnSalvarAsync(ct);
    }

    private static PlanoResponse FnMapear(PlanoAssinatura p) => new(
        p.Id,
        p.Nome,
        p.Descricao,
        p.PrecoMensal,
        p.Status,
        p.ServicosInclusos.Select(ps => new ServicoIncluidoResponse(ps.ServicoId, ps.Servico!.Nome, ps.Servico!.Status, ps.LimiteMensal)).ToList());
}
