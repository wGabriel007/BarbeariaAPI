using Barbearia.Domain.Entidades;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Barbearia.Infrastructure.Persistencia.Configuracoes;

public class PlanoAssinaturaConfiguration : IEntityTypeConfiguration<PlanoAssinatura>
{
    public void Configure(EntityTypeBuilder<PlanoAssinatura> builder)
    {
        builder.Property(p => p.Id).UseIdentityAlwaysColumn();
        builder.Property(p => p.CriadoEm).HasDefaultValueSql("now()").ValueGeneratedOnAdd();
        builder.Property(p => p.AtualizadoEm).HasDefaultValueSql("now()").ValueGeneratedOnAddOrUpdate();

        builder.Property(p => p.PrecoMensal).HasPrecision(10, 2);

        builder.HasIndex(p => p.Nome).IsUnique();

        builder.Navigation(p => p.ServicosInclusos).UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasMany(p => p.ServicosInclusos)
            .WithOne(ps => ps.Plano!)
            .HasForeignKey(ps => ps.PlanoId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
