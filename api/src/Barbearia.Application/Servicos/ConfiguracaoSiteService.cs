using Barbearia.Application.Abstracoes;
using Barbearia.Application.Comum;
using Barbearia.Application.Dtos.ConfiguracoesSite;
using Barbearia.Domain.Comum;
using Barbearia.Domain.Entidades;

namespace Barbearia.Application.Servicos;

/// <summary>
/// Marca/aparência do site (nome exibido, logo, cor de destaque) e as
/// informações públicas do negócio (descrição, endereço, telefone,
/// Instagram, horário de funcionamento, galeria de fotos). Uma linha só
/// no banco (ver ConfiguracaoSite.IdUnico) — não tem "CriarAsync" nem
/// "ListarAsync" de propósito, só leitura (qualquer um, inclusive
/// deslogado) e alteração — nome/logo/cor é só Admin, as informações/
/// fotos são Admin OU Barbeiro (ver ConfiguracaoSiteController).
/// </summary>
public class ConfiguracaoSiteService
{
    private readonly IConfiguracaoSiteRepository _repositorio;
    private readonly IArmazenamentoArquivos _armazenamento;
    private readonly IUnitOfWork _uow;

    public ConfiguracaoSiteService(IConfiguracaoSiteRepository repositorio, IArmazenamentoArquivos armazenamento, IUnitOfWork uow)
    {
        _repositorio = repositorio;
        _armazenamento = armazenamento;
        _uow = uow;
    }

    public async Task<ConfiguracaoSiteResponse> FnObterAsync(CancellationToken ct = default) =>
        FnMapear(await _repositorio.FnObterAsync(ct));

    public async Task<ConfiguracaoSiteResponse> FnAtualizarAsync(AtualizarConfiguracaoSiteRequest request, CancellationToken ct = default)
    {
        var config = await _repositorio.FnObterAsync(ct);
        config.FnAtualizar(request.NomeBarbearia, request.CorPrimaria);
        await _uow.FnSalvarAsync(ct);

        return FnMapear(config);
    }

    /// <summary>
    /// Descrição/endereço/telefone/Instagram/horário de funcionamento —
    /// diferente de FnAtualizarAsync (nome/cor, só Admin), este aqui é
    /// Admin OU Barbeiro (ver ConfiguracaoSiteController).
    /// </summary>
    public async Task<ConfiguracaoSiteResponse> FnAtualizarInformacoesAsync(AtualizarInformacoesBarbeariaRequest request, CancellationToken ct = default)
    {
        var config = await _repositorio.FnObterAsync(ct);
        config.FnAtualizarInformacoes(request.Descricao, request.Endereco, request.Telefone, request.Instagram, request.HorarioFuncionamento);
        await _uow.FnSalvarAsync(ct);

        return FnMapear(config);
    }

    /// <summary>
    /// Adiciona mais uma foto à galeria — confere o limite ANTES de gastar
    /// o upload (ver comentário em ConfiguracaoSite.MaximoFotos): sem
    /// isso, uma tentativa rejeitada ainda teria salvo o arquivo físico
    /// no armazenamento, órfão, sem nenhuma FotoBarbearia apontando pra ele.
    /// </summary>
    public async Task<ConfiguracaoSiteResponse> FnAdicionarFotoAsync(Stream conteudo, string extensao, CancellationToken ct = default)
    {
        var config = await _repositorio.FnObterAsync(ct);

        if (config.Fotos.Count >= ConfiguracaoSite.MaximoFotos)
            throw new DomainException($"Máximo de {ConfiguracaoSite.MaximoFotos} fotos na galeria — remova alguma antes de adicionar outra.");

        var url = await _armazenamento.FnSalvarAsync(conteudo, extensao, "fotos-barbearia", ct);
        config.FnAdicionarFoto(url);
        await _uow.FnSalvarAsync(ct);

        return FnMapear(config);
    }

    public async Task<ConfiguracaoSiteResponse> FnRemoverFotoAsync(long fotoId, CancellationToken ct = default)
    {
        var config = await _repositorio.FnObterAsync(ct);
        var foto = config.Fotos.FirstOrDefault(f => f.Id == fotoId);

        config.FnRemoverFoto(fotoId); // lança DomainException se não existir — antes de apagar o arquivo físico
        await _uow.FnSalvarAsync(ct);

        _armazenamento.FnRemover(foto!.Url);

        return FnMapear(config);
    }

    public async Task<ConfiguracaoSiteResponse> FnAtualizarLogoAsync(Stream conteudo, string extensao, CancellationToken ct = default)
    {
        var config = await _repositorio.FnObterAsync(ct);
        var urlAntiga = config.LogoUrl;

        var novaUrl = await _armazenamento.FnSalvarAsync(conteudo, extensao, "logo", ct);
        config.FnDefinirLogo(novaUrl);
        await _uow.FnSalvarAsync(ct);

        // Só apaga a antiga DEPOIS de salvar a nova com sucesso — se o
        // upload novo falhar no meio do caminho, a logo antiga continua
        // valendo em vez de o site ficar sem nenhuma.
        _armazenamento.FnRemover(urlAntiga);

        return FnMapear(config);
    }

    public async Task<ConfiguracaoSiteResponse> FnRemoverLogoAsync(CancellationToken ct = default)
    {
        var config = await _repositorio.FnObterAsync(ct);

        _armazenamento.FnRemover(config.LogoUrl);
        config.FnRemoverLogo();
        await _uow.FnSalvarAsync(ct);

        return FnMapear(config);
    }

    private static ConfiguracaoSiteResponse FnMapear(ConfiguracaoSite c) => new(
        c.NomeBarbearia,
        c.LogoUrl,
        c.CorPrimaria,
        c.Descricao,
        c.Endereco,
        c.Telefone,
        c.Instagram,
        c.HorarioFuncionamento,
        c.Fotos.Select(f => new FotoBarbeariaResponse(f.Id, f.Url)).ToList());
}
