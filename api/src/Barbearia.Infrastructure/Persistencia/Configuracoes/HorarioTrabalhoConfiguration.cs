using Barbearia.Domain.Entidades;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Barbearia.Infrastructure.Persistencia.Configuracoes;

public class HorarioTrabalhoConfiguration : IEntityTypeConfiguration<HorarioTrabalho>
{
    public void Configure(EntityTypeBuilder<HorarioTrabalho> builder)
    {
        builder.Property(h => h.Id).UseIdentityAlwaysColumn();
        // Sem criado_em/atualizado_em de propósito — a tabela horarios_trabalho
        // no 01_schema.sql não tem essas colunas. O relacionamento com
        // Barbeiro (FK, cascade) já é configurado em BarbeiroConfiguration.
    }
}
