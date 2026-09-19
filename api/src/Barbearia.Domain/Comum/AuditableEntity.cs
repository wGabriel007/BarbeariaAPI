namespace Barbearia.Domain.Comum;

/// <summary>
/// Entidade cuja tabela tem criado_em/atualizado_em (a maioria — só
/// horarios_trabalho e bloqueios_agenda não têm, no schema).
///
/// Importante: esses dois campos são preenchidos pelo PRÓPRIO BANCO
/// (DEFAULT now() na criação, e a trigger atualizar_timestamp() em todo
/// UPDATE — ver 01_schema.sql). O C# não escreve esses valores; ele só
/// os LÊ de volta depois do INSERT/UPDATE. Por isso os setters aqui são
/// 'protected' e nunca chamados manualmente — o EF Core é configurado
/// (na Fase de Infrastructure) para tratar essas colunas como geradas
/// pelo banco (ValueGeneratedOnAddOrUpdate).
/// </summary>
public abstract class AuditableEntity : Entity
{
    public DateTimeOffset CriadoEm { get; protected set; }
    public DateTimeOffset AtualizadoEm { get; protected set; }
}
