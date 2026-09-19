using Barbearia.Domain.Comum;
using Barbearia.Domain.Enumeracoes;

namespace Barbearia.Domain.Entidades;

public class Cliente : AuditableEntity
{
    public string NomeCompleto { get; private set; } = string.Empty;

    // Antes era obrigatório (todo cliente era cadastrado manualmente pelo
    // staff, que sempre pedia um telefone de contato). Ficou opcional
    // porque agora um Cliente também pode nascer sozinho, auto-provisionado
    // pra um usuário Comum que solicitou um agendamento (ver UsuarioId
    // abaixo e AgendamentoService.FnSolicitarAsync) — nesse caso não temos
    // telefone nenhum ainda, só nome/e-mail da conta.
    public string? Telefone { get; private set; }
    public string? Email { get; private set; }
    public string? Cpf { get; private set; }
    public DateOnly? DataNascimento { get; private set; }
    public string? Observacoes { get; private set; }
    public StatusRegistro Status { get; private set; }

    /// <summary>
    /// Liga este Cliente à conta de login (Usuario) de quem ele é, SE
    /// existir uma — mesma ideia do Barbeiro.UsuarioId, só que aqui é
    /// opcional: um cliente cadastrado manualmente pelo staff (a
    /// maioria, hoje) não tem conta nenhuma. Só fica preenchido quando o
    /// Cliente nasceu a partir de um usuário Comum pedindo o próprio
    /// agendamento pela primeira vez.
    /// </summary>
    public long? UsuarioId { get; private set; }

    private Cliente()
    {
    }

    public static Cliente FnCriar(
        string nomeCompleto,
        string? telefone,
        string? email = null,
        string? cpf = null,
        DateOnly? dataNascimento = null,
        string? observacoes = null,
        long? usuarioId = null)
    {
        if (string.IsNullOrWhiteSpace(nomeCompleto))
            throw new DomainException("Nome completo é obrigatório.");

        return new Cliente
        {
            NomeCompleto = nomeCompleto.Trim(),
            Telefone = string.IsNullOrWhiteSpace(telefone) ? null : telefone.Trim(),
            Email = string.IsNullOrWhiteSpace(email) ? null : email.Trim(),
            Cpf = string.IsNullOrWhiteSpace(cpf) ? null : cpf.Trim(),
            DataNascimento = dataNascimento,
            Observacoes = observacoes,
            UsuarioId = usuarioId,
            Status = StatusRegistro.Ativo
        };
    }

    public void FnAtualizarDados(string nomeCompleto, string? telefone, string? email, string? observacoes)
    {
        if (string.IsNullOrWhiteSpace(nomeCompleto))
            throw new DomainException("Nome completo é obrigatório.");

        NomeCompleto = nomeCompleto.Trim();
        Telefone = string.IsNullOrWhiteSpace(telefone) ? null : telefone.Trim();
        Email = string.IsNullOrWhiteSpace(email) ? null : email.Trim();
        Observacoes = observacoes;
    }

    public void FnAtivar() => Status = StatusRegistro.Ativo;
    public void FnInativar() => Status = StatusRegistro.Inativo;
    public void FnBloquear() => Status = StatusRegistro.Bloqueado;
}
