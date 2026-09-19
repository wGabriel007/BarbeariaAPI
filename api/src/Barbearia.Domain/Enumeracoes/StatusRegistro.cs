namespace Barbearia.Domain.Enumeracoes;

/// <summary>
/// Enum ÚNICO, reaproveitado em Usuario.Status, Barbeiro.Status,
/// Cliente.Status, Servico.Status e PlanoAssinatura.Status — todos com
/// exatamente o mesmo significado (ver o "MAPA CANÔNICO" no topo do
/// 01_schema.sql). Não crie um StatusUsuario/StatusCliente/StatusServico
/// separado para cada tabela: seria o mesmo enum repetido 5 vezes.
/// </summary>
public enum StatusRegistro
{
    Inativo = 0,
    Ativo = 1,
    Bloqueado = 2
}
