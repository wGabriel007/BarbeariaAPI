using Barbearia.Domain.Entidades;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Barbearia.Infrastructure.Persistencia.Configuracoes;

public class PremioRankingConfiguration : IEntityTypeConfiguration<PremioRanking>
{
    public void Configure(EntityTypeBuilder<PremioRanking> builder)
    {
        builder.Property(p => p.Id).UseIdentityAlwaysColumn();
        builder.Property(p => p.CriadoEm).HasDefaultValueSql("now()").ValueGeneratedOnAdd();
        builder.Property(p => p.AtualizadoEm).HasDefaultValueSql("now()").ValueGeneratedOnAddOrUpdate();

        // Um prêmio só por posição, por mês — é isso que faz
        // FnDefinirPremiosAsync conseguir decidir "atualizar ou criar" sem
        // ambiguidade (ver 10_migracao_ranking.sql).
        builder.HasIndex(p => new { p.Mes, p.Ano, p.Posicao }).IsUnique();
    }
}
