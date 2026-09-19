using Barbearia.Domain.Entidades;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Barbearia.Infrastructure.Persistencia.Configuracoes;

public class BloqueioAgendaConfiguration : IEntityTypeConfiguration<BloqueioAgenda>
{
    public void Configure(EntityTypeBuilder<BloqueioAgenda> builder)
    {
        builder.Property(b => b.Id).UseIdentityAlwaysColumn();
        // Sem criado_em/atualizado_em — mesmo caso de horarios_trabalho.
    }
}
