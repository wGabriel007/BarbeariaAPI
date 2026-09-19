using Barbearia.Domain.Entidades;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Barbearia.Infrastructure.Persistencia.Configuracoes;

public class AssinaturaConfiguration : IEntityTypeConfiguration<Assinatura>
{
    public void Configure(EntityTypeBuilder<Assinatura> builder)
    {
        builder.Property(a => a.Id).UseIdentityAlwaysColumn();
        builder.Property(a => a.CriadoEm).HasDefaultValueSql("now()").ValueGeneratedOnAdd();
        builder.Property(a => a.AtualizadoEm).HasDefaultValueSql("now()").ValueGeneratedOnAddOrUpdate();

        builder.HasOne<Cliente>()
            .WithMany()
            .HasForeignKey(a => a.ClienteId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<PlanoAssinatura>()
            .WithMany()
            .HasForeignKey(a => a.PlanoId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(a => a.ClienteId);
    }
}
