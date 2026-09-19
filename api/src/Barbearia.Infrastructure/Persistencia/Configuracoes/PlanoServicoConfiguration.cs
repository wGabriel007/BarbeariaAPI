using Barbearia.Domain.Entidades;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Barbearia.Infrastructure.Persistencia.Configuracoes;

public class PlanoServicoConfiguration : IEntityTypeConfiguration<PlanoServico>
{
    public void Configure(EntityTypeBuilder<PlanoServico> builder)
    {
        // Chave primária composta — espelha PRIMARY KEY (plano_id, servico_id) no 01_schema.sql.
        builder.HasKey(ps => new { ps.PlanoId, ps.ServicoId });

        builder.HasOne(ps => ps.Servico)
            .WithMany()
            .HasForeignKey(ps => ps.ServicoId)
            .OnDelete(DeleteBehavior.Restrict); // ON DELETE RESTRICT no 01_schema.sql
    }
}
