using Barbearia.Domain.Entidades;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Barbearia.Infrastructure.Persistencia.Configuracoes;

public class FotoBarbeariaConfiguration : IEntityTypeConfiguration<FotoBarbearia>
{
    public void Configure(EntityTypeBuilder<FotoBarbearia> builder)
    {
        builder.Property(f => f.Id).UseIdentityAlwaysColumn();
        // Sem criado_em/atualizado_em de propósito — a tabela fotos_barbearia
        // no 01_schema.sql não tem essas colunas (mesmo caso de
        // HorarioTrabalho). O relacionamento com ConfiguracaoSite (FK,
        // cascade) já é configurado em ConfiguracaoSiteConfiguration.
    }
}
