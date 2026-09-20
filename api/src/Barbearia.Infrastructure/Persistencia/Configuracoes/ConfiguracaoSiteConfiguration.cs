using Barbearia.Domain.Entidades;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Barbearia.Infrastructure.Persistencia.Configuracoes;

public class ConfiguracaoSiteConfiguration : IEntityTypeConfiguration<ConfiguracaoSite>
{
    public void Configure(EntityTypeBuilder<ConfiguracaoSite> builder)
    {
        // Desde o multi-tenant (ver 14_migracao_multi_barbearia.sql), esta
        // tabela passou a ter uma linha POR BARBEARIA — igual toda outra
        // entidade, com Id gerado normalmente (antes, com uma linha global
        // só, o Id era sempre fixo em 1 e ValueGeneratedNever — não é mais
        // o caso).
        builder.Property(c => c.Id).UseIdentityAlwaysColumn();

        builder.HasIndex(c => c.EmpresaId).IsUnique();

        // Fotos é exposta como IReadOnlyCollection<T> por fora, mas o
        // campo de verdade é List<T> (_fotos) — mesma nota de
        // BarbeiroConfiguration sobre Horarios/Bloqueios: sem isto o EF
        // Core não consegue popular uma coleção só de getter.
        builder.Navigation(c => c.Fotos).UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasMany(c => c.Fotos)
            .WithOne()
            .HasForeignKey(f => f.ConfiguracaoSiteId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
