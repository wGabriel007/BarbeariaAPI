using Barbearia.Domain.Entidades;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Barbearia.Infrastructure.Persistencia.Configuracoes;

public class ConfiguracaoSiteConfiguration : IEntityTypeConfiguration<ConfiguracaoSite>
{
    public void Configure(EntityTypeBuilder<ConfiguracaoSite> builder)
    {
        // Diferente de toda outra entidade (BIGINT GENERATED ALWAYS AS
        // IDENTITY): aqui o Id é sempre o mesmo valor fixo (ConfiguracaoSite.IdUnico),
        // já inserido pela migração — o EF Core nunca deve tentar gerar
        // um novo Id sozinho pra isto.
        builder.Property(c => c.Id).ValueGeneratedNever();

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
