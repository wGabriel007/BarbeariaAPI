namespace Barbearia.Domain.Enumeracoes;

/// <summary>
/// Espelha o CHECK (tipo IN (0, 1, 2)) de usuarios.tipo no 01_schema.sql.
/// Os valores são atribuídos EXPLICITAMENTE de propósito — nunca deixe
/// o compilador escolher (ex.: "enum TipoUsuario { Admin, Barbeiro }"
/// sem os '= 0, = 1, = 2'), e nunca reordene ou remova um valor depois
/// que já existirem dados gravados: o número é o que fica salvo no
/// banco, o nome é só um rótulo pro código.
/// </summary>
public enum TipoUsuario
{
    Admin = 0,
    Barbeiro = 1,

    /// <summary>
    /// Papel padrão de quem se autocadastra pelo /cadastro — só a
    /// PRIMEIRA conta do sistema vira Admin automaticamente (ver
    /// AutenticacaoService.FnRegistrarAsync); todo autocadastro seguinte
    /// nasce Comum. Um Admin promove alguém depois, se precisar (não
    /// há tela pra isso ainda — troca de tipo continua manual no banco
    /// por enquanto).
    /// </summary>
    Comum = 2
}
