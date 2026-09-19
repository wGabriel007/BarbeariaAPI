using Barbearia.Domain.Entidades;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Barbearia.Infrastructure.Persistencia.Configuracoes;

public class PagamentoConfiguration : IEntityTypeConfiguration<Pagamento>
{
    public void Configure(EntityTypeBuilder<Pagamento> builder)
    {
        builder.Property(p => p.Id).UseIdentityAlwaysColumn();
        builder.Property(p => p.CriadoEm).HasDefaultValueSql("now()").ValueGeneratedOnAdd();
        builder.Property(p => p.AtualizadoEm).HasDefaultValueSql("now()").ValueGeneratedOnAddOrUpdate();

        builder.Property(p => p.Valor).HasPrecision(10, 2);

        builder.HasOne<Agendamento>().WithMany().HasForeignKey(p => p.AgendamentoId).OnDelete(DeleteBehavior.SetNull);
        builder.HasOne<Assinatura>().WithMany().HasForeignKey(p => p.AssinaturaId).OnDelete(DeleteBehavior.SetNull);
        builder.HasOne<Cliente>().WithMany().HasForeignKey(p => p.ClienteId).OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(p => p.ClienteId);
        builder.HasIndex(p => p.Status);
    }
}
