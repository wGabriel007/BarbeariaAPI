using Barbearia.Application.Abstracoes;
using Barbearia.Application.Comum;
using Barbearia.Application.Dtos.Assinaturas;
using Barbearia.Domain.Comum;
using Barbearia.Domain.Entidades;

namespace Barbearia.Application.Servicos;

public class AssinaturaService
{
    private readonly IAssinaturaRepository _assinaturas;
    private readonly IClienteRepository _clientes;
    private readonly IPlanoAssinaturaRepository _planos;
    private readonly IUnitOfWork _uow;

    public AssinaturaService(
        IAssinaturaRepository assinaturas,
        IClienteRepository clientes,
        IPlanoAssinaturaRepository planos,
        IUnitOfWork uow)
    {
        _assinaturas = assinaturas;
        _clientes = clientes;
        _planos = planos;
        _uow = uow;
    }

    public async Task<AssinaturaResponse> FnCriarAsync(CriarAssinaturaRequest request, CancellationToken ct = default)
    {
        var cliente = await _clientes.FnObterPorIdAsync(request.ClienteId, ct)
            ?? throw NotFoundException.FnPara("Cliente", request.ClienteId);

        _ = await _planos.FnObterPorIdAsync(request.PlanoId, ct)
            ?? throw NotFoundException.FnPara("Plano", request.PlanoId);

        var assinatura = Assinatura.FnCriar(request.ClienteId, request.PlanoId, request.DataInicio, request.DataVencimento);
        assinatura.FnAtribuirEmpresa(cliente.EmpresaId);

        await _assinaturas.FnAdicionarAsync(assinatura, ct);
        await _uow.FnSalvarAsync(ct);

        return FnMapear(assinatura);
    }

    public async Task<List<AssinaturaResponse>> FnListarPorClienteAsync(long clienteId, CancellationToken ct = default)
    {
        var lista = await _assinaturas.FnListarPorClienteAsync(clienteId, ct);
        return lista.Select(FnMapear).ToList();
    }

    /// <summary>
    /// GET /api/assinaturas/minhas — a aba "Meu Plano" do usuário Comum
    /// logado. Não existe Cliente ainda (nunca pediu agendamento nem
    /// plano) -&gt; devolve lista vazia em vez de 404, porque "não ter
    /// nenhuma assinatura" é um estado normal, não um erro.
    /// </summary>
    public async Task<List<AssinaturaResponse>> FnListarMinhasAsync(long usuarioId, CancellationToken ct = default)
    {
        var cliente = await _clientes.FnObterPorUsuarioIdAsync(usuarioId, ct);
        if (cliente is null)
            return [];

        return await FnListarPorClienteAsync(cliente.Id, ct);
    }

    public async Task FnSuspenderAsync(long id, CancellationToken ct = default)
    {
        var assinatura = await _assinaturas.FnObterPorIdAsync(id, ct)
            ?? throw NotFoundException.FnPara("Assinatura", id);

        assinatura.FnSuspender();
        await _uow.FnSalvarAsync(ct);
    }

    public async Task FnReativarAsync(long id, CancellationToken ct = default)
    {
        var assinatura = await _assinaturas.FnObterPorIdAsync(id, ct)
            ?? throw NotFoundException.FnPara("Assinatura", id);

        assinatura.FnReativar();
        await _uow.FnSalvarAsync(ct);
    }

    public async Task FnCancelarAsync(long id, DateOnly dataCancelamento, CancellationToken ct = default)
    {
        var assinatura = await _assinaturas.FnObterPorIdAsync(id, ct)
            ?? throw NotFoundException.FnPara("Assinatura", id);

        assinatura.FnCancelar(dataCancelamento);
        await _uow.FnSalvarAsync(ct);
    }

    /// <summary>
    /// POST /api/assinaturas/5/cancelar-minha — self-service: o próprio
    /// cliente cancela a PRÓPRIA assinatura (mesmo espírito de
    /// BarbeiroService.FnDefinirAusenciaAsync / Agendamento.FnCancelar), sem
    /// precisar de um Admin/Barbeiro pra aprovar. A data de cancelamento
    /// é sempre "agora" — diferente da rota de staff (que aceita
    /// qualquer data, útil pra acerto retroativo), aqui não faz sentido
    /// o cliente escolher uma data no request.
    /// </summary>
    public async Task FnCancelarMinhaAsync(long id, long usuarioId, CancellationToken ct = default)
    {
        var cliente = await _clientes.FnObterPorUsuarioIdAsync(usuarioId, ct);
        var assinatura = await _assinaturas.FnObterPorIdAsync(id, ct)
            ?? throw NotFoundException.FnPara("Assinatura", id);

        if (cliente is null || assinatura.ClienteId != cliente.Id)
            throw new DomainException("Essa assinatura não pertence a você.");

        assinatura.FnCancelar(DateOnly.FromDateTime(DateTime.UtcNow));
        await _uow.FnSalvarAsync(ct);
    }

    private static AssinaturaResponse FnMapear(Assinatura a) => new(
        a.Id, a.ClienteId, a.PlanoId, a.DataInicio, a.DataFim, a.DataVencimento, a.Status, a.AtualizadoEm);
}
