using Barbearia.Domain.Enumeracoes;

namespace Barbearia.Application.Dtos.Servicos;

public sealed record CriarServicoRequest(
    string Nome,
    string? Descricao,
    short DuracaoMinutos,
    decimal Preco,
    CategoriaServico Categoria);

public sealed record AtualizarPrecoServicoRequest(decimal NovoPreco);

public sealed record AtualizarCategoriaServicoRequest(CategoriaServico NovaCategoria);

public sealed record ServicoResponse(
    long Id,
    string Nome,
    string? Descricao,
    short DuracaoMinutos,
    decimal Preco,
    CategoriaServico Categoria,
    StatusRegistro Status);
