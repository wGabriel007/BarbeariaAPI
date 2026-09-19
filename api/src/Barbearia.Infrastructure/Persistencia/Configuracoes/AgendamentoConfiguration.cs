using Barbearia.Domain.Entidades;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Barbearia.Infrastructure.Persistencia.Configuracoes;

public class AgendamentoConfiguration : IEntityTypeConfiguration<Agendamento>
{
    public void Configure(EntityTypeBuilder<Agendamento> builder)
    {
        builder.Property(a => a.Id).UseIdentityAlwaysColumn();
        builder.Property(a => a.CriadoEm).HasDefaultValueSql("now()").ValueGeneratedOnAdd();
        builder.Property(a => a.AtualizadoEm).HasDefaultValueSql("now()").ValueGeneratedOnAddOrUpdate();

        builder.Property(a => a.PrecoCobrado).HasPrecision(10, 2);

        builder.HasOne<Cliente>().WithMany().HasForeignKey(a => a.ClienteId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Barbeiro>().WithMany().HasForeignKey(a => a.BarbeiroId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Servico>().WithMany().HasForeignKey(a => a.ServicoId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Assinatura>().WithMany().HasForeignKey(a => a.AssinaturaId).OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(a => a.ClienteId);
        builder.HasIndex(a => new { a.BarbeiroId, a.Inicio });
        builder.HasIndex(a => a.Inicio);

        // O EXCLUDE constraint que impede dois agendamentos conflitantes
        // pro mesmo barbeiro (sem_conflito_horario, em 01_schema.sql)
        // NÃO tem representação em Fluent API — EF Core não sabe gerar
        // EXCLUDE USING gist. Como este projeto não usa Migrations (ver
        // BarbeariaDbContext), isso não é um problema: o constraint já
        // existe no banco, criado pelo SQL. Se algum dia este projeto
        // migrar para EF Core Migrations, essa constraint precisa ser
        // adicionada manualmente na migration via migrationBuilder.Sql(...).
    }
}
