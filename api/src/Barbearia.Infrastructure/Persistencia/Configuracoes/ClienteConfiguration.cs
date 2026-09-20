using Barbearia.Domain.Entidades;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Barbearia.Infrastructure.Persistencia.Configuracoes;

public class ClienteConfiguration : IEntityTypeConfiguration<Cliente>
{
    public void Configure(EntityTypeBuilder<Cliente> builder)
    {
        builder.Property(c => c.Id).UseIdentityAlwaysColumn();
        builder.Property(c => c.CriadoEm).HasDefaultValueSql("now()").ValueGeneratedOnAdd();
        builder.Property(c => c.AtualizadoEm).HasDefaultValueSql("now()").ValueGeneratedOnAddOrUpdate();

        // CPF único DENTRO de cada barbearia — a mesma pessoa pode ser
        // cliente de duas barbearias diferentes na plataforma (ver
        // ux_clientes_empresa_cpf em 13_migracao_multi_barbearia.sql).
        builder.HasIndex(c => new { c.EmpresaId, c.Cpf })
            .IsUnique()
            .HasDatabaseName("ux_clientes_empresa_cpf");
        builder.HasIndex(c => c.Telefone); // não único (ver ix_clientes_telefone no 01_schema.sql)

        // 1:1 opcional com Usuario — diferente de Barbeiro (que sempre
        // tem Usuario), a maioria dos Clientes não tem nenhum (cadastro
        // manual pelo staff); só quem foi auto-provisionado a partir de
        // um Comum tem (ver Cliente.UsuarioId). Postgres permite vários
        // NULL numa coluna UNIQUE, então isso não trava os demais.
        builder.HasIndex(c => c.UsuarioId).IsUnique();
    }
}
