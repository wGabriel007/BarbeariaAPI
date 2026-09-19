using Barbearia.Domain.Comum;
using Barbearia.Domain.Enumeracoes;

namespace Barbearia.Domain.Entidades;

public class PlanoAssinatura : AuditableEntity
{
    public string Nome { get; private set; } = string.Empty;
    public string? Descricao { get; private set; }
    public decimal PrecoMensal { get; private set; }
    public StatusRegistro Status { get; private set; }

    // Ver nota equivalente em Barbeiro.cs sobre não usar .AsReadOnly() aqui.
    private readonly List<PlanoServico> _servicosInclusos = new();
    public IReadOnlyCollection<PlanoServico> ServicosInclusos => _servicosInclusos;

    private PlanoAssinatura()
    {
    }

    public static PlanoAssinatura FnCriar(string nome, decimal precoMensal, string? descricao = null)
    {
        if (string.IsNullOrWhiteSpace(nome))
            throw new DomainException("Nome do plano é obrigatório.");

        if (precoMensal < 0)
            throw new DomainException("Preço mensal não pode ser negativo.");

        return new PlanoAssinatura
        {
            Nome = nome.Trim(),
            Descricao = descricao,
            PrecoMensal = precoMensal,
            Status = StatusRegistro.Ativo
        };
    }

    /// <summary>
    /// Inclui um serviço no plano com um limite de uso mensal (ex.: 2
    /// cortes por mês). Nota sobre o Id=0 aqui: enquanto este
    /// PlanoAssinatura ainda não foi salvo, Id é 0 — tudo bem, o EF Core
    /// substitui esse valor automaticamente pelo Id real gerado pelo
    /// banco no momento do SaveChanges, porque o PlanoServico foi
    /// adicionado através da navegação (_servicosInclusos), não por um
    /// número solto.
    /// </summary>
    public void FnIncluirServico(long servicoId, short limiteMensal)
    {
        if (_servicosInclusos.Any(ps => ps.ServicoId == servicoId))
            throw new DomainException("Esse serviço já está incluído neste plano.");

        _servicosInclusos.Add(PlanoServico.FnCriar(Id, servicoId, limiteMensal));
    }

    public void FnRemoverServico(long servicoId)
    {
        var item = _servicosInclusos.FirstOrDefault(ps => ps.ServicoId == servicoId);
        if (item is not null)
            _servicosInclusos.Remove(item);
    }

    public void FnAtivar() => Status = StatusRegistro.Ativo;
    public void FnInativar() => Status = StatusRegistro.Inativo;
    public void FnBloquear() => Status = StatusRegistro.Bloqueado;
}
