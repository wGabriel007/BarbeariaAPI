using Barbearia.Domain.Comum;
using Barbearia.Domain.Enumeracoes;

namespace Barbearia.Domain.Entidades;

public class Barbeiro : AuditableEntity
{
    public long UsuarioId { get; private set; }
    public string? Telefone { get; private set; }
    public StatusRegistro Status { get; private set; }

    /// <summary>
    /// "De folga hoje" — bem diferente de Status=Inativo/Bloqueado: o
    /// cadastro continua ativo e o barbeiro continua podendo usar o
    /// sistema normalmente (login, ver as próprias solicitações etc.),
    /// só não aparece pra Comum/Cliente escolherem na hora de pedir um
    /// agendamento (ver filtro em BarbeirosController.FnListar). Por isso
    /// é um campo à parte de Status, não um terceiro valor dele — o
    /// próprio barbeiro liga/desliga isso sozinho quando quiser (ver
    /// BarbeiroService.FnDefinirAusenciaAsync), sem precisar de um Admin.
    /// </summary>
    public bool Ausente { get; private set; }

    /// <summary>Barbearia (Empresa) dona deste cadastro — sempre igual ao EmpresaId do Usuario ligado (ver FnAtribuirEmpresa).</summary>
    public long EmpresaId { get; private set; }

    /// <summary>
    /// Apresentação profissional self-service (ver FnAtualizarPerfil) —
    /// exibida na aba "Sobre a barbearia" (ver paginas/SobreABarbearia.jsx).
    /// Bio é o texto livre ("sobre mim"); Especialidade é o rótulo curto
    /// ao lado do nome (ex.: "Corte degradê, barba clássica").
    /// </summary>
    public string? Bio { get; private set; }
    public string? Especialidade { get; private set; }

    public Usuario? Usuario { get; private set; }

    // NOTA sobre EF Core: a propriedade pública devolve o campo _horarios
    // diretamente (não .AsReadOnly()) — List<T> já implementa
    // IReadOnlyCollection<T>, então isso continua impedindo quem usa a
    // classe de dar .Add()/.Remove() por fora (a coleção só muda através
    // de FnAdicionarHorario()). É importante NÃO envolver em .AsReadOnly()
    // aqui: isso criaria um objeto novo a cada leitura, e o EF Core
    // (configurado com PropertyAccessMode.Field na Fase de
    // Infrastructure) precisa que a mesma instância de List<T> seja
    // devolvida sempre, pra conseguir materializar/rastrear a coleção
    // vinda do banco direto no campo privado.
    private readonly List<HorarioTrabalho> _horarios = new();
    public IReadOnlyCollection<HorarioTrabalho> Horarios => _horarios;

    private readonly List<BloqueioAgenda> _bloqueios = new();
    public IReadOnlyCollection<BloqueioAgenda> Bloqueios => _bloqueios;

    private Barbeiro()
    {
    }

    public static Barbeiro FnCriar(long usuarioId, string? telefone = null)
    {
        if (usuarioId <= 0)
            throw new DomainException("UsuarioId inválido.");

        return new Barbeiro
        {
            UsuarioId = usuarioId,
            Telefone = telefone,
            Status = StatusRegistro.Ativo,
            Ausente = false
        };
    }

    public void FnAtualizarTelefone(string? telefone) => Telefone = telefone;

    public void FnMarcarAusente() => Ausente = true;
    public void FnMarcarPresente() => Ausente = false;

    /// <summary>
    /// Adiciona um horário de trabalho fixo (ex.: terça, 09h-18h).
    /// A checagem de "não sobrepor com outro horário do mesmo dia" fica
    /// para a Application (que consegue consultar o banco); aqui no
    /// Domain só validamos o que a própria entidade sabe sozinha.
    /// </summary>
    public void FnAdicionarHorario(DiaSemana diaSemana, TimeOnly horaInicio, TimeOnly horaFim)
    {
        var horario = HorarioTrabalho.FnCriar(Id, diaSemana, horaInicio, horaFim);
        _horarios.Add(horario);
    }

    /// <summary>
    /// Remove um horário de trabalho já cadastrado (ex.: o barbeiro parou
    /// de atender às terças). Não mexe em nenhum agendamento já marcado
    /// nesse horário — só tira o barbeiro da disponibilidade DAQUI PRA
    /// FRENTE (ver FnGarantirDentroDoExpediente em AgendamentoService, que
    /// só olha os horários atuais na hora de aceitar uma NOVA solicitação).
    /// </summary>
    public void FnRemoverHorario(long horarioId)
    {
        var horario = _horarios.FirstOrDefault(h => h.Id == horarioId)
            ?? throw new DomainException("Este horário não pertence a este barbeiro.");

        _horarios.Remove(horario);
    }

    public void FnAtivar() => Status = StatusRegistro.Ativo;
    public void FnInativar() => Status = StatusRegistro.Inativo;
    public void FnBloquear() => Status = StatusRegistro.Bloqueado;

    /// <summary>Chamado uma única vez, logo após FnCriar, sempre com o EmpresaId do Usuario promovido — ver BarbeiroService.FnPromoverAsync.</summary>
    public void FnAtribuirEmpresa(long empresaId)
    {
        if (EmpresaId != 0)
            throw new DomainException("Este barbeiro já pertence a uma barbearia.");

        if (empresaId <= 0)
            throw new DomainException("EmpresaId inválido.");

        EmpresaId = empresaId;
    }

    /// <summary>Bio/Especialidade são opcionais e livres — só cortamos o que passar do tamanho da coluna no banco, nunca lançamos por "campo vazio".</summary>
    public void FnAtualizarPerfil(string? bio, string? especialidade)
    {
        Bio = string.IsNullOrWhiteSpace(bio) ? null : bio.Trim();

        if (string.IsNullOrWhiteSpace(especialidade))
        {
            Especialidade = null;
        }
        else
        {
            var limpa = especialidade.Trim();
            Especialidade = limpa.Length > 150 ? limpa[..150] : limpa;
        }
    }
}
