using Barbearia.Domain.Entidades;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Barbearia.Infrastructure.Persistencia.Configuracoes;

public class BarbeiroConfiguration : IEntityTypeConfiguration<Barbeiro>
{
    public void Configure(EntityTypeBuilder<Barbeiro> builder)
    {
        builder.Property(b => b.Id).UseIdentityAlwaysColumn();
        builder.Property(b => b.CriadoEm).HasDefaultValueSql("now()").ValueGeneratedOnAdd();
        builder.Property(b => b.AtualizadoEm).HasDefaultValueSql("now()").ValueGeneratedOnAddOrUpdate();

        // 1:1 com Usuario — o lado dependente (quem tem a FK) é Barbeiro.
        builder.HasOne(b => b.Usuario)
            .WithOne(u => u.Barbeiro)
            .HasForeignKey<Barbeiro>(b => b.UsuarioId)
            .OnDelete(DeleteBehavior.Cascade); // espelha "ON DELETE CASCADE" de usuario_id no 01_schema.sql

        builder.HasIndex(b => b.UsuarioId).IsUnique();

        // Horarios/Bloqueios são expostos como IReadOnlyCollection<T> por
        // fora, mas o campo de verdade é List<T> (_horarios/_bloqueios).
        // Isto aqui diz ao EF Core: "materialize e rastreie a coleção
        // direto no campo privado, ignorando a propriedade pública" —
        // sem isso, o EF não consegue popular uma coleção só de getter.
        builder.Navigation(b => b.Horarios).UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.Navigation(b => b.Bloqueios).UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasMany(b => b.Horarios)
            .WithOne()
            .HasForeignKey(h => h.BarbeiroId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(b => b.Bloqueios)
            .WithOne()
            .HasForeignKey(bl => bl.BarbeiroId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
