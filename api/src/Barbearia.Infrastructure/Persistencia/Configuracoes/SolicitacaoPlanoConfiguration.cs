using Barbearia.Domain.Entidades;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Barbearia.Infrastructure.Persistencia.Configuracoes;

public class SolicitacaoPlanoConfiguration : IEntityTypeConfiguration<SolicitacaoPlano>
{
    public void Configure(EntityTypeBuilder<SolicitacaoPlano> builder)
    {
        // Sem ToTable explícito: o nome vem do DbSet
        // "SolicitacoesPlano" em BarbeariaDbContext, convertido pra
        // snake_case ("solicitacoes_plano") pela EFCore.NamingConventions
        // — mesmo padrão de toda outra entidade neste projeto.
        builder.Property(s => s.Id).UseIdentityAlwaysColumn();
        builder.Property(s => s.CriadoEm).HasDefaultValueSql("now()").ValueGeneratedOnAdd();
        builder.Property(s => s.AtualizadoEm).HasDefaultValueSql("now()").ValueGeneratedOnAddOrUpdate();

        // Sem navegação pra Usuario/Plano/Assinatura de propósito (ver
        // comentário em SolicitacaoPlano) — são só FKs simples, o Service
        // busca os nomes separadamente quando precisa exibir.
        builder.HasIndex(s => s.UsuarioId);
    }
}
