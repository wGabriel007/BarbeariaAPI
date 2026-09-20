using Barbearia.Application.Abstracoes;
using Barbearia.Application.Comum;
using Barbearia.Application.Dtos.Servicos;
using Barbearia.Domain.Comum;
using Barbearia.Domain.Entidades;

namespace Barbearia.Application.Servicos;

public class ServicoService
{
    private readonly IServicoRepository _repositorio;
    private readonly ICurrentTenantService _tenant;
    private readonly IUnitOfWork _uow;

    public ServicoService(IServicoRepository repositorio, ICurrentTenantService tenant, IUnitOfWork uow)
    {
        _repositorio = repositorio;
        _tenant = tenant;
        _uow = uow;
    }

    public async Task<ServicoResponse> FnCriarAsync(CriarServicoRequest request, CancellationToken ct = default)
    {
        if (await _repositorio.FnExisteComNomeAsync(request.Nome, ct))
            throw new DomainException($"Já existe um serviço chamado '{request.Nome}'.");

        var servico = Servico.FnCriar(request.Nome, request.DuracaoMinutos, request.Preco, request.Categoria, request.Descricao);
        servico.FnAtribuirEmpresa(_tenant.EmpresaId!.Value);

        await _repositorio.FnAdicionarAsync(servico, ct);
        await _uow.FnSalvarAsync(ct);

        return FnMapear(servico);
    }

    public async Task<ServicoResponse> FnObterPorIdAsync(long id, CancellationToken ct = default)
    {
        var servico = await _repositorio.FnObterPorIdAsync(id, ct)
            ?? throw NotFoundException.FnPara("Serviço", id);

        return FnMapear(servico);
    }

    public async Task<List<ServicoResponse>> FnListarAsync(CancellationToken ct = default)
    {
        var servicos = await _repositorio.FnListarAsync(ct);
        return servicos.Select(FnMapear).ToList();
    }

    public async Task<ServicoResponse> FnAtualizarPrecoAsync(long id, AtualizarPrecoServicoRequest request, CancellationToken ct = default)
    {
        var servico = await _repositorio.FnObterPorIdAsync(id, ct)
            ?? throw NotFoundException.FnPara("Serviço", id);

        servico.FnAtualizarPreco(request.NovoPreco);
        await _uow.FnSalvarAsync(ct);

        return FnMapear(servico);
    }

    public async Task<ServicoResponse> FnAtualizarCategoriaAsync(long id, AtualizarCategoriaServicoRequest request, CancellationToken ct = default)
    {
        var servico = await _repositorio.FnObterPorIdAsync(id, ct)
            ?? throw NotFoundException.FnPara("Serviço", id);

        servico.FnAtualizarCategoria(request.NovaCategoria);
        await _uow.FnSalvarAsync(ct);

        return FnMapear(servico);
    }

    public async Task FnInativarAsync(long id, CancellationToken ct = default)
    {
        var servico = await _repositorio.FnObterPorIdAsync(id, ct)
            ?? throw NotFoundException.FnPara("Serviço", id);

        servico.FnInativar();
        await _uow.FnSalvarAsync(ct);
    }

    public async Task FnAtivarAsync(long id, CancellationToken ct = default)
    {
        var servico = await _repositorio.FnObterPorIdAsync(id, ct)
            ?? throw NotFoundException.FnPara("Serviço", id);

        servico.FnAtivar();
        await _uow.FnSalvarAsync(ct);
    }

    private static ServicoResponse FnMapear(Servico s) => new(s.Id, s.Nome, s.Descricao, s.DuracaoMinutos, s.Preco, s.Categoria, s.Status);
}
