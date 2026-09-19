using Barbearia.Domain.Comum;
using Barbearia.Domain.Enumeracoes;

namespace Barbearia.Domain.Entidades;

/// <summary>
/// Conta de acesso ao painel (dono/admin ou barbeiro). Clientes não têm
/// login nesta v1 — ver comentário em 01_schema.sql.
/// </summary>
public class Usuario : AuditableEntity
{
    public string NomeCompleto { get; private set; } = string.Empty;
    public string Email { get; private set; } = string.Empty;

    /// <summary>
    /// FnHash da senha (BCrypt/Argon2) — NUNCA a senha em texto puro.
    /// Gerar o hash é responsabilidade da Application/Infrastructure
    /// (que tem acesso a uma lib de hashing); o Domain só armazena o
    /// resultado e não sabe como ele foi calculado.
    /// </summary>
    public string SenhaHash { get; private set; } = string.Empty;

    public TipoUsuario Tipo { get; private set; }
    public StatusRegistro Status { get; private set; }

    /// <summary>Contato pessoal (aba "Meu perfil") — separado do telefone de Cliente/Barbeiro, que são cadastros à parte.</summary>
    public string? Telefone { get; private set; }

    /// <summary>URL relativa do arquivo de foto (ver IArmazenamentoArquivos) — null usa o avatar padrão (iniciais do nome).</summary>
    public string? FotoUrl { get; private set; }

    /// <summary>Navegação 1:1 — nem todo usuário é barbeiro (o admin pode não ser).</summary>
    public Barbeiro? Barbeiro { get; private set; }

    private Usuario()
    {
        // Construtor privado sem parâmetros: só o EF Core usa, via
        // reflexão, para materializar a entidade lendo do banco.
        // Código de aplicação nunca deveria chamar isso — por isso é
        // privado; use Usuario.FnCriar(...) para criar um novo usuário.
    }

    /// <summary>
    /// 'telefone' é opcional AQUI de propósito — quem exige (ou não)
    /// telefone na hora de criar a conta é decisão de CADA fluxo que
    /// chama este método, não do Domain: o autocadastro público
    /// (AutenticacaoService.FnRegistrarAsync) passa a exigir a partir de
    /// agora, enquanto o cadastro manual de conta pelo Admin
    /// (UsuarioService.FnCriarAsync, tela "Usuários") continua sem pedir —
    /// o Admin normalmente ainda nem tem esse dado na hora de criar um
    /// segundo Admin/Comum na mão.
    /// </summary>
    public static Usuario FnCriar(string nomeCompleto, string email, string senhaHash, TipoUsuario tipo, string? telefone = null)
    {
        if (string.IsNullOrWhiteSpace(nomeCompleto))
            throw new DomainException("Nome completo é obrigatório.");

        if (string.IsNullOrWhiteSpace(email) || !email.Contains('@'))
            throw new DomainException("E-mail inválido.");

        if (string.IsNullOrWhiteSpace(senhaHash))
            throw new DomainException("Hash de senha é obrigatório.");

        return new Usuario
        {
            NomeCompleto = nomeCompleto.Trim(),
            Email = email.Trim().ToLowerInvariant(),
            SenhaHash = senhaHash,
            Tipo = tipo,
            Telefone = string.IsNullOrWhiteSpace(telefone) ? null : telefone.Trim(),
            Status = StatusRegistro.Ativo
        };
    }

    public void FnAtualizarSenha(string novoHash)
    {
        if (string.IsNullOrWhiteSpace(novoHash))
            throw new DomainException("Hash de senha inválido.");

        SenhaHash = novoHash;
    }

    /// <summary>Aba "Meu perfil": o próprio usuário edita nome/e-mail/telefone. FnVerificar unicidade do e-mail é responsabilidade da Application (só ela consegue consultar o banco).</summary>
    public void FnAtualizarDados(string nomeCompleto, string email, string? telefone)
    {
        if (string.IsNullOrWhiteSpace(nomeCompleto))
            throw new DomainException("Nome completo é obrigatório.");

        if (string.IsNullOrWhiteSpace(email) || !email.Contains('@'))
            throw new DomainException("E-mail inválido.");

        NomeCompleto = nomeCompleto.Trim();
        Email = email.Trim().ToLowerInvariant();
        Telefone = string.IsNullOrWhiteSpace(telefone) ? null : telefone.Trim();
    }

    public void FnDefinirFoto(string fotoUrl) => FotoUrl = fotoUrl;
    public void FnRemoverFoto() => FotoUrl = null;

    public void FnAtivar() => Status = StatusRegistro.Ativo;
    public void FnInativar() => Status = StatusRegistro.Inativo;
    public void FnBloquear() => Status = StatusRegistro.Bloqueado;

    /// <summary>
    /// Promove um usuário Comum a Barbeiro — chamado pelo Admin ao
    /// "setar" um usuário comum (ou o Comum por trás de um Cliente já
    /// existente) como barbeiro (ver BarbeiroService.FnPromoverAsync).
    /// Só parte de Comum: não existe caminho pra "rebaixar" um Admin, e
    /// promover quem já é Barbeiro não faz sentido (BarbeiroService já
    /// impede duplicar o cadastro de Barbeiro em si).
    /// </summary>
    public void FnPromoverParaBarbeiro()
    {
        if (Tipo != TipoUsuario.Comum)
            throw new DomainException("Só é possível promover um usuário do tipo Comum a Barbeiro.");

        Tipo = TipoUsuario.Barbeiro;
    }
}
