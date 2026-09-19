namespace Barbearia.Domain.Comum;

/// <summary>
/// Classe base de toda entidade do Domain. Toda entidade tem identidade
/// (Id) — duas entidades são "iguais" se forem do mesmo tipo e tiverem o
/// mesmo Id, mesmo que os outros campos sejam diferentes (Id é gerado
/// pelo banco via BIGINT GENERATED ALWAYS AS IDENTITY, então o setter é
/// 'protected': só a própria hierarquia de classes e o EF Core, via
/// reflexão, conseguem definir esse valor).
/// </summary>
public abstract class Entity
{
    public long Id { get; protected set; }

    public override bool Equals(object? obj)
    {
        if (obj is not Entity other) return false;
        if (ReferenceEquals(this, other)) return true;
        if (GetType() != other.GetType()) return false;

        // Enquanto o Id ainda é 0 (entidade criada em memória, ainda não
        // salva), duas instâncias nunca são consideradas iguais — mesmo
        // que sejam a "mesma" entidade conceitualmente, elas só passam a
        // ter identidade de verdade depois que o banco gera o Id.
        if (Id == 0 || other.Id == 0) return false;

        return Id == other.Id;
    }

    public override int GetHashCode() => HashCode.Combine(GetType(), Id);
}
