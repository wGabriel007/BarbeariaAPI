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

        // Um prêmio só por posição, por mês, DENTRO de cada barbearia — é
        // isso que faz FnDefinirPremiosAsync conseguir decidir "atualizar
        // ou criar" sem ambiguidade (ver 10_migracao_ranking.sql). Ganhou
        // EmpresaId na chave (ver ux_premios_ranking_empresa_mes_ano_posicao
        // em 13_migracao_multi_barbearia.sql) — sem isso, só UMA barbearia
        // no sistema inteiro conseguiria configurar o prêmio de um mês.
        builder.HasIndex(p => new { p.EmpresaId, p.Mes, p.Ano, p.Posicao })
            .IsUnique()
            .HasDatabaseName("ux_premios_ranking_empresa_mes_ano_posicao");
    }
}
