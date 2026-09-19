using Barbearia.Domain.Entidades;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Barbearia.Infrastructure.Persistencia.Configuracoes;

public class ServicoConfiguration : IEntityTypeConfiguration<Servico>
{
    public void Configure(EntityTypeBuilder<Servico> builder)
    {
        builder.Property(s => s.Id).UseIdentityAlwaysColumn();
        builder.Property(s => s.CriadoEm).HasDefaultValueSql("now()").ValueGeneratedOnAdd();
        builder.Property(s => s.AtualizadoEm).HasDefaultValueSql("now()").ValueGeneratedOnAddOrUpdate();

        // EF Core EXIGE precisão/escala explícitas para 'decimal', senão
        // gera um warning e usa um default que pode truncar valores.
        // NUMERIC(10,2) no 01_schema.sql -> HasPrecision(10, 2) aqui.
        builder.Property(s => s.Preco).HasPrecision(10, 2);

        builder.HasIndex(s => s.Nome).IsUnique();
    }
}
