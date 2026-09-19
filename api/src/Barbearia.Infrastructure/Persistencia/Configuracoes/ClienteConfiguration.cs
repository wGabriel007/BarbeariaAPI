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

        builder.HasIndex(c => c.Cpf).IsUnique();
        builder.HasIndex(c => c.Telefone); // não único (ver ix_clientes_telefone no 01_schema.sql)

        // 1:1 opcional com Usuario — diferente de Barbeiro (que sempre
        // tem Usuario), a maioria dos Clientes não tem nenhum (cadastro
        // manual pelo staff); só quem foi auto-provisionado a partir de
        // um Comum tem (ver Cliente.UsuarioId). Postgres permite vários
        // NULL numa coluna UNIQUE, então isso não trava os demais.
        builder.HasIndex(c => c.UsuarioId).IsUnique();
    }
}
