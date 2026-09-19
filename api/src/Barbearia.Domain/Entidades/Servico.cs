using Barbearia.Domain.Comum;
using Barbearia.Domain.Enumeracoes;

namespace Barbearia.Domain.Entidades;

public class Servico : AuditableEntity
{
    public string Nome { get; private set; } = string.Empty;
    public string? Descricao { get; private set; }
    public short DuracaoMinutos { get; private set; }
    public decimal Preco { get; private set; }
    public CategoriaServico Categoria { get; private set; }
    public StatusRegistro Status { get; private set; }

    private Servico()
    {
    }

    public static Servico FnCriar(
        string nome,
        short duracaoMinutos,
        decimal preco,
        CategoriaServico categoria,
        string? descricao = null)
    {
        if (string.IsNullOrWhiteSpace(nome))
            throw new DomainException("Nome do serviço é obrigatório.");

        if (duracaoMinutos <= 0)
            throw new DomainException("Duração precisa ser maior que zero.");

        if (preco < 0)
            throw new DomainException("Preço não pode ser negativo.");

        return new Servico
        {
            Nome = nome.Trim(),
            Descricao = descricao,
            DuracaoMinutos = duracaoMinutos,
            Preco = preco,
            Categoria = categoria,
            Status = StatusRegistro.Ativo
        };
    }

    public void FnAtualizarPreco(decimal novoPreco)
    {
        if (novoPreco < 0)
            throw new DomainException("Preço não pode ser negativo.");

        Preco = novoPreco;
    }

    /// <summary>Reclassificar um serviço já cadastrado (ex.: criou como "Outro" e depois decidiu que é "Barba") — não mexe em preço/duração/status.</summary>
    public void FnAtualizarCategoria(CategoriaServico novaCategoria) => Categoria = novaCategoria;

    public void FnAtivar() => Status = StatusRegistro.Ativo;
    public void FnInativar() => Status = StatusRegistro.Inativo;
    public void FnBloquear() => Status = StatusRegistro.Bloqueado;
}
