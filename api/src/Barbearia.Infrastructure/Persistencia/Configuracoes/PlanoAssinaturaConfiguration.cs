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

        // Nome único DENTRO de cada barbearia — mesmo raciocínio do
        // ServicoConfiguration ("Plano Mensal" vai se repetir entre
        // barbearias; ver ux_planos_assinatura_empresa_nome em
        // 13_migracao_multi_barbearia.sql).
        builder.HasIndex(p => new { p.EmpresaId, p.Nome })
            .IsUnique()
            .HasDatabaseName("ux_planos_assinatura_empresa_nome");

        builder.Navigation(p => p.ServicosInclusos).UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasMany(p => p.ServicosInclusos)
            .WithOne(ps => ps.Plano!)
            .HasForeignKey(ps => ps.PlanoId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
