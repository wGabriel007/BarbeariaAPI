using Barbearia.Application.Abstracoes;
using Barbearia.Application.Comum;
using Barbearia.Application.Dtos.Agendamentos;
using Barbearia.Domain.Comum;
using Barbearia.Domain.Entidades;
using Barbearia.Domain.Enumeracoes;

namespace Barbearia.Application.Servicos;

public class AgendamentoService
{
    private readonly IAgendamentoRepository _agendamentos;
    private readonly IClienteRepository _clientes;
    private readonly IBarbeiroRepository _barbeiros;
    private readonly IServicoRepository _servicos;
    private readonly IPagamentoRepository _pagamentos;
    private readonly IUsuarioRepository _usuarios;
    private readonly IUnitOfWork _uow;

    public AgendamentoService(
        IAgendamentoRepository agendamentos,
        IClienteRepository clientes,
        IBarbeiroRepository barbeiros,
        IServicoRepository servicos,
        IPagamentoRepository pagamentos,
        IUsuarioRepository usuarios,
        IUnitOfWork uow)
    {
        _agendamentos = agendamentos;
        _clientes = clientes;
        _barbeiros = barbeiros;
        _servicos = servicos;
        _pagamentos = pagamentos;
        _usuarios = usuarios;
        _uow = uow;
    }

    public async Task<AgendamentoResponse> FnCriarAsync(CriarAgendamentoRequest request, CancellationToken ct = default)
    {
        var cliente = await _clientes.FnObterPorIdAsync(request.ClienteId, ct)
            ?? throw NotFoundException.FnPara("Cliente", request.ClienteId);

        _ = await _barbeiros.FnObterPorIdAsync(request.BarbeiroId, ct)
            ?? throw NotFoundException.FnPara("Barbeiro", request.BarbeiroId);

        var servico = await _servicos.FnObterPorIdAsync(request.ServicoId, ct)
            ?? throw NotFoundException.FnPara("Serviço", request.ServicoId);

        // Fim e PrecoCobrado são CALCULADOS pelo servidor a partir do
        // catálogo de serviços — nunca aceitos do cliente. Isso evita
        // dois problemas: (1) o cliente "inventar" um preço diferente
        // do cadastrado, e (2) fim e duração do serviço ficarem
        // inconsistentes entre si.
        var inicio = request.Inicio;
        var fim = inicio.AddMinutes(servico.DuracaoMinutos);
        var precoCobrado = servico.Preco;

        // Primeira camada de defesa (ver comentário em IAgendamentoRepository.FnExisteConflitoAsync).
        if (await _agendamentos.FnExisteConflitoAsync(request.BarbeiroId, inicio, fim, ct: ct))
            throw new ConflitoDeHorarioException("Este barbeiro já tem um agendamento nesse horário.");

        var agendamento = Agendamento.FnCriar(
            request.ClienteId, request.BarbeiroId, request.ServicoId,
            inicio, fim, precoCobrado, request.AssinaturaId, request.Observacoes);
        agendamento.FnAtribuirEmpresa(cliente.EmpresaId);

        await _agendamentos.FnAdicionarAsync(agendamento, ct);

        // Segunda camada de defesa: se, entre a checagem acima e esta
        // linha, outro pedido para o mesmo horário já tiver sido
        // salvo (condição de corrida), o EXCLUDE constraint do Postgres
        // rejeita o INSERT — a Infrastructure traduz isso de volta para
        // ConflitoDeHorarioException (não deixa vazar como erro de banco).
        await _uow.FnSalvarAsync(ct);

        return FnMapear(agendamento);
    }

    /// <summary>
    /// Fluxo do CLIENTE (Comum) pedindo um horário — diferente de
    /// FnCriarAsync (usado pelo staff), aqui: (1) não recebe ClienteId, o
    /// próprio Cliente é resolvido/criado a partir da conta logada; (2)
    /// o horário pedido tem que caber dentro do expediente cadastrado
    /// do barbeiro (HorarioTrabalho); (3) nasce Pendente, não Agendado —
    /// só passa a valer depois que um Admin/Barbeiro confirma (ver
    /// FnConfirmarAsync/FnRejeitarAsync).
    /// </summary>
    public async Task<AgendamentoResponse> FnSolicitarAsync(long usuarioId, SolicitarAgendamentoRequest request, CancellationToken ct = default)
    {
        var cliente = await FnObterOuCriarClienteDoUsuarioAsync(usuarioId, ct);

        var barbeiro = await _barbeiros.FnObterComHorariosAsync(request.BarbeiroId, ct)
            ?? throw NotFoundException.FnPara("Barbeiro", request.BarbeiroId);

        // Segunda camada de defesa (a primeira é o próprio GET /api/barbeiros
        // já não listar barbeiros Ausentes/Inativos pra um Comum/Cliente —
        // ver BarbeirosController.FnListar): mesmo que a lista que o front
        // usou pra montar o <select> esteja desatualizada, o pedido não
        // passa.
        if (barbeiro.Ausente)
            throw new DomainException("Este barbeiro está ausente hoje e não pode receber novas solicitações.");

        if (barbeiro.Status != StatusRegistro.Ativo)
            throw new DomainException("Este barbeiro não está mais ativo.");

        var servico = await _servicos.FnObterPorIdAsync(request.ServicoId, ct)
            ?? throw NotFoundException.FnPara("Serviço", request.ServicoId);

        var inicio = request.Inicio;
        var fim = inicio.AddMinutes(servico.DuracaoMinutos);

        FnGarantirDentroDoExpediente(barbeiro, inicio, fim);

        if (await _agendamentos.FnExisteConflitoAsync(request.BarbeiroId, inicio, fim, ct: ct))
            throw new ConflitoDeHorarioException("Este barbeiro já tem um agendamento (ou uma solicitação pendente) nesse horário.");

        var agendamento = Agendamento.FnSolicitar(
            cliente.Id, request.BarbeiroId, request.ServicoId, inicio, fim, servico.Preco, observacoes: request.Observacoes);
        agendamento.FnAtribuirEmpresa(cliente.EmpresaId);

        await _agendamentos.FnAdicionarAsync(agendamento, ct);
        await _uow.FnSalvarAsync(ct);

        return FnMapear(agendamento);
    }

    /// <summary>
    /// Todo usuário Comum tem, no máximo, UM Cliente ligado a ele (ver
    /// Cliente.UsuarioId) — criado na hora em que ele pede o PRIMEIRO
    /// agendamento (não no cadastro/login, pra não gerar um Cliente
    /// "vazio" pra quem nunca vai usar). Nas vezes seguintes, o mesmo
    /// Cliente é reaproveitado.
    /// </summary>
    private async Task<Cliente> FnObterOuCriarClienteDoUsuarioAsync(long usuarioId, CancellationToken ct)
    {
        var cliente = await _clientes.FnObterPorUsuarioIdAsync(usuarioId, ct);
        if (cliente is not null)
            return cliente;

        var usuario = await _usuarios.FnObterPorIdAsync(usuarioId, ct)
            ?? throw NotFoundException.FnPara("Usuario", usuarioId);

        cliente = Cliente.FnCriar(usuario.NomeCompleto, telefone: null, email: usuario.Email, usuarioId: usuarioId);
        cliente.FnAtribuirEmpresa(usuario.EmpresaId!.Value);
        await _clientes.FnAdicionarAsync(cliente, ct);
        await _uow.FnSalvarAsync(ct); // precisa do Id gerado antes de usar cliente.Id no Agendamento

        return cliente;
    }

    /// <summary>
    /// Fuso fixo da barbearia (Brasil, sem horário de verão desde 2019 —
    /// por isso um offset fixo já resolve, sem precisar de TimeZoneInfo/
    /// tzdata, que nem sempre está disponível igual entre Linux/Windows).
    /// Necessário porque o front sempre manda 'Inicio' em UTC
    /// (Date.toISOString() do JS sempre usa offset zero, "Z"), enquanto
    /// HorarioTrabalho.HoraInicio/HoraFim são cadastrados pensando em
    /// horário LOCAL (ver HorarioTrabalho — são só TimeOnly, sem fuso
    /// nenhum). Sem converter pra este fuso antes de comparar, um pedido
    /// às 18h (horário local) vira 21h em 'inicio.DateTime' e seria
    /// recusado por "estar fora do expediente" mesmo cabendo direitinho
    /// num expediente até as 20h — mesma família do bug do dia trocado na
    /// Agenda (ver comentário em frontend/src/pages/Agenda.jsx), só que
    /// nas HORAS em vez do dia.
    /// </summary>
    private static readonly TimeSpan FusoLoja = TimeSpan.FromHours(-3);

    /// <summary>
    /// DiaSemana do Domain (0=Domingo...6=Sábado) bate exatamente com
    /// DayOfWeek do .NET, então a conversão é só um cast. O agendamento
    /// inteiro (início E fim) precisa caber dentro de um único
    /// HorarioTrabalho — não damos suporte a um corte que atravessa dois
    /// intervalos cadastrados no mesmo dia.
    /// </summary>
    private static void FnGarantirDentroDoExpediente(Barbeiro barbeiro, DateTimeOffset inicio, DateTimeOffset fim)
    {
        var inicioLocal = inicio.ToOffset(FusoLoja);
        var fimLocal = fim.ToOffset(FusoLoja);

        var diaSemana = (DiaSemana)(int)inicioLocal.DayOfWeek;
        var horaInicio = TimeOnly.FromDateTime(inicioLocal.DateTime);
        var horaFim = TimeOnly.FromDateTime(fimLocal.DateTime);

        var cabeNoExpediente = barbeiro.Horarios.Any(h =>
            h.DiaSemana == diaSemana && horaInicio >= h.HoraInicio && horaFim <= h.HoraFim);

        if (!cabeNoExpediente)
            throw new DomainException("Esse horário está fora do expediente cadastrado para este barbeiro.");
    }

    public async Task<AgendamentoResponse> FnObterPorIdAsync(long id, CancellationToken ct = default)
    {
        var agendamento = await _agendamentos.FnObterPorIdAsync(id, ct)
            ?? throw NotFoundException.FnPara("Agendamento", id);

        return FnMapear(agendamento);
    }

    public async Task<List<AgendamentoResponse>> FnListarPorBarbeiroEPeriodoAsync(
        long barbeiroId, DateTimeOffset de, DateTimeOffset ate, CancellationToken ct = default)
    {
        var lista = await _agendamentos.FnListarPorBarbeiroEPeriodoAsync(barbeiroId, de, ate, ct);
        return lista.Select(FnMapear).ToList();
    }

    public async Task<List<AgendamentoResponse>> FnListarPorClienteAsync(long clienteId, CancellationToken ct = default)
    {
        var lista = await _agendamentos.FnListarPorClienteAsync(clienteId, ct);
        return lista.Select(FnMapear).ToList();
    }

    /// <summary>"Meus agendamentos" do cliente logado — vazio se ele ainda não pediu nenhum (nem tem Cliente provisionado ainda).</summary>
    public async Task<List<AgendamentoResponse>> FnListarMeusAsync(long usuarioId, CancellationToken ct = default)
    {
        var cliente = await _clientes.FnObterPorUsuarioIdAsync(usuarioId, ct);
        if (cliente is null)
            return new List<AgendamentoResponse>();

        return await FnListarPorClienteAsync(cliente.Id, ct);
    }

    /// <summary>
    /// "Fila de hoje" de quem está logado — JUNTA todas as pontas que se
    /// aplicarem a essa conta (antes era "só a primeira que bater", um
    /// BUG real: "Meus agendamentos" é aberta pra todo mundo, inclusive
    /// staff — ver comentário em Layout.jsx sobre "um Barbeiro que também
    /// seja cliente" — então um Barbeiro que pediu um corte pra si mesmo
    /// (virando também Cliente) parava de ver a PRÓPRIA fila de
    /// atendimento pra sempre, mesmo sem ter mais nenhum agendamento como
    /// cliente hoje):
    ///   - CLIENTE (se houver cadastro): sua própria posição em cada fila
    ///     onde está esperando, ver FnListarFilaComoClienteAsync.
    ///   - BARBEIRO (se houver cadastro, mesmo sendo também Admin): a
    ///     fila de quem ele vai atender hoje, ver FnListarFilaComoBarbeiroAsync.
    ///   - ADMIN sem cadastro de Barbeiro próprio: a fila de HOJE de
    ///     TODOS os barbeiros, uma seção por barbeiro — visão gerencial
    ///     do dia inteiro, ver FnListarFilaDeTodosOsBarbeirosAsync.
    /// Uma conta comum (Comum, sem Cliente ainda) cai só no primeiro
    /// caso, com lista vazia se não tiver Cliente — mesmo resultado de
    /// antes. Uma conta staff sem NENHUM agendamento como cliente hoje
    /// nem sequer entra no primeiro bloco (FnObterPorUsuarioIdAsync
    /// devolve null), então o comportamento de sempre (só a própria fila
    /// profissional) continua idêntico pra quem nunca usou "Meus
    /// agendamentos" como cliente.
    /// </summary>
    public async Task<List<FilaDoBarbeiroResponse>> FnListarMinhaFilaAsync(long usuarioId, bool ehAdmin, CancellationToken ct = default)
    {
        var resultado = new List<FilaDoBarbeiroResponse>();

        var cliente = await _clientes.FnObterPorUsuarioIdAsync(usuarioId, ct);
        if (cliente is not null)
            resultado.AddRange(await FnListarFilaComoClienteAsync(cliente, ct));

        var barbeiro = await _barbeiros.FnObterPorUsuarioIdAsync(usuarioId, ct);
        if (barbeiro is not null)
            resultado.AddRange(await FnListarFilaComoBarbeiroAsync(barbeiro, ct));
        else if (ehAdmin)
            resultado.AddRange(await FnListarFilaDeTodosOsBarbeirosAsync(ct));

        return resultado;
    }

    /// <summary>
    /// Visão do CLIENTE (ver FnListarMinhaFilaAsync): pra cada agendamento
    /// HOJE já Confirmado, monta a lista ordenada de quem ainda não foi
    /// atendido (Confirmado) ou está sendo atendido agora (EmAtendimento)
    /// com o MESMO barbeiro — a posição do próprio cliente nessa lista É
    /// a posição na fila, sem guardar posição nenhuma: se alguém antes
    /// for atendido/cancelado, a próxima consulta já reflete a fila nova
    /// sozinha. Um agendamento ainda Pendente ou Agendado (staff nem
    /// confirmou) não entra na fila — só passa a contar depois de
    /// Confirmado. Inclui o NOME dos outros clientes na frente — igual
    /// quem espera numa barbearia de verdade vê fisicamente quem está
    /// antes na fila (ver pages/FilaDeEspera.jsx).
    /// </summary>
    private async Task<List<FilaDoBarbeiroResponse>> FnListarFilaComoClienteAsync(Cliente cliente, CancellationToken ct)
    {
        var agora = DateTimeOffset.UtcNow.ToOffset(FusoLoja);
        var inicioDoDia = new DateTimeOffset(agora.Year, agora.Month, agora.Day, 0, 0, 0, FusoLoja);
        var fimDoDia = inicioDoDia.AddDays(1);

        var meus = await _agendamentos.FnListarPorClienteAsync(cliente.Id, ct);
        var meusNaFilaHoje = meus
            .Where(a => a.Status == StatusAgendamento.Confirmado && a.Inicio >= inicioDoDia && a.Inicio < fimDoDia)
            .ToList();

        var resultado = new List<FilaDoBarbeiroResponse>();
        foreach (var meu in meusNaFilaHoje)
        {
            // Um por barbeiro seria o ideal pra evitar repetir a consulta se o
            // cliente tiver mais de um horário com o mesmo barbeiro hoje, mas
            // isso é raríssimo (o EXCLUDE constraint já impede sobreposição) —
            // não vale a complexidade de cachear aqui.
            var doBarbeiroHoje = await _agendamentos.FnListarPorBarbeiroEPeriodoAsync(meu.BarbeiroId, inicioDoDia, fimDoDia, ct);
            var fila = doBarbeiroHoje
                .Where(a => a.Status == StatusAgendamento.Confirmado || a.Status == StatusAgendamento.EmAtendimento)
                .OrderBy(a => a.Inicio)
                .ToList();

            var posicao = fila.FindIndex(a => a.Id == meu.Id) + 1;
            if (posicao <= 0)
                continue; // defensivo: não devia acontecer (meu já é Confirmado, então está na própria lista)

            // Barbeiro não guarda o próprio nome (isso vive no Usuario
            // ligado a ele — mesmo padrão de BarbeiroService.FnMapear):
            // precisa de uma segunda busca pelo UsuarioId.
            var barbeiro = await _barbeiros.FnObterPorIdAsync(meu.BarbeiroId, ct);
            var usuarioBarbeiro = barbeiro is null ? null : await _usuarios.FnObterPorIdAsync(barbeiro.UsuarioId, ct);

            // A fila de uma barbearia real nunca é grande (é literalmente
            // quem está esperando AGORA, num único dia) — N consultas
            // extras pro nome de cada cliente não pesa, mesma lógica já
            // usada em SolicitacaoPlanoService.FnMapearAsync.
            var itens = new List<ItemFilaResponse>();
            foreach (var a in fila)
            {
                var clienteDoItem = await _clientes.FnObterPorIdAsync(a.ClienteId, ct);
                itens.Add(new ItemFilaResponse(
                    a.Id,
                    clienteDoItem?.NomeCompleto ?? $"Cliente #{a.ClienteId}",
                    a.Inicio,
                    a.Status,
                    a.Id == meu.Id));
            }

            resultado.Add(new FilaDoBarbeiroResponse(
                meu.BarbeiroId,
                usuarioBarbeiro?.NomeCompleto ?? $"Barbeiro #{meu.BarbeiroId}",
                meu.Id,
                posicao,
                fila.Count,
                itens));
        }

        return resultado;
    }

    /// <summary>
    /// Visão do BARBEIRO (ver FnListarMinhaFilaAsync): a fila de HOJE que
    /// ele mesmo vai atender — todo mundo Confirmado ou EmAtendimento com
    /// ele, na ordem do horário. Diferente da visão do cliente, aqui não
    /// existe "minha posição" (quem pediu é o barbeiro, não um dos
    /// clientes esperando) — por isso MeuAgendamentoId e MinhaPosicao
    /// voltam zerados; é assim que o front (FilaDeEspera.jsx) distingue
    /// as duas visões e decide o que mostrar. Sem ninguém confirmado
    /// hoje, devolve lista vazia (mesmo formato do "não tenho fila" do
    /// cliente).
    /// </summary>
    private async Task<List<FilaDoBarbeiroResponse>> FnListarFilaComoBarbeiroAsync(Barbeiro barbeiro, CancellationToken ct)
    {
        var agora = DateTimeOffset.UtcNow.ToOffset(FusoLoja);
        var inicioDoDia = new DateTimeOffset(agora.Year, agora.Month, agora.Day, 0, 0, 0, FusoLoja);
        var fimDoDia = inicioDoDia.AddDays(1);

        var doBarbeiroHoje = await _agendamentos.FnListarPorBarbeiroEPeriodoAsync(barbeiro.Id, inicioDoDia, fimDoDia, ct);
        var fila = doBarbeiroHoje
            .Where(a => a.Status == StatusAgendamento.Confirmado || a.Status == StatusAgendamento.EmAtendimento)
            .OrderBy(a => a.Inicio)
            .ToList();

        if (fila.Count == 0)
            return new List<FilaDoBarbeiroResponse>();

        var usuarioBarbeiro = await _usuarios.FnObterPorIdAsync(barbeiro.UsuarioId, ct);

        var itens = new List<ItemFilaResponse>();
        foreach (var a in fila)
        {
            var clienteDoItem = await _clientes.FnObterPorIdAsync(a.ClienteId, ct);
            itens.Add(new ItemFilaResponse(
                a.Id,
                clienteDoItem?.NomeCompleto ?? $"Cliente #{a.ClienteId}",
                a.Inicio,
                a.Status,
                false));
        }

        return new List<FilaDoBarbeiroResponse>
        {
            new(barbeiro.Id, usuarioBarbeiro?.NomeCompleto ?? $"Barbeiro #{barbeiro.Id}", 0, 0, fila.Count, itens),
        };
    }

    /// <summary>
    /// Visão do ADMIN (ver FnListarMinhaFilaAsync): junta a fila de HOJE
    /// de TODOS os barbeiros Ativos, uma seção por barbeiro — reaproveita
    /// FnListarFilaComoBarbeiroAsync pra cada um (mesmo formato que o
    /// front já sabe desenhar, ver FilaDeEspera.jsx) e simplesmente pula
    /// quem não tem ninguém esperando hoje, em vez de mostrar uma seção
    /// vazia pra cada barbeiro sem fila.
    /// </summary>
    private async Task<List<FilaDoBarbeiroResponse>> FnListarFilaDeTodosOsBarbeirosAsync(CancellationToken ct)
    {
        var barbeiros = await _barbeiros.FnListarAsync(ct);

        var resultado = new List<FilaDoBarbeiroResponse>();
        foreach (var barbeiro in barbeiros.Where(b => b.Status == StatusRegistro.Ativo))
        {
            var filaDoBarbeiro = await FnListarFilaComoBarbeiroAsync(barbeiro, ct);
            resultado.AddRange(filaDoBarbeiro); // vazia (sem ninguém esperando) não adiciona nada
        }

        return resultado;
    }

    /// <summary>
    /// "Aba de solicitações" do staff. Um Admin ('ehAdmin' true) vê as
    /// solicitações de TODOS os barbeiros; um Barbeiro vê só as PRÓPRIAS
    /// (resolvidas aqui a partir do usuarioId do token, nunca de um Id
    /// que o próprio cliente da API poderia informar) — se esse usuário
    /// não tiver nenhum cadastro de Barbeiro (não devia acontecer, mas
    /// não custa ser defensivo), devolve vazio em vez de vazar tudo.
    /// </summary>
    public async Task<List<AgendamentoResponse>> FnListarPendentesAsync(long usuarioId, bool ehAdmin, CancellationToken ct = default)
    {
        long? barbeiroId = null;
        if (!ehAdmin)
        {
            var barbeiro = await _barbeiros.FnObterPorUsuarioIdAsync(usuarioId, ct);
            barbeiroId = barbeiro?.Id ?? -1;
        }

        var lista = await _agendamentos.FnListarPendentesAsync(barbeiroId, ct);
        return lista.Select(FnMapear).ToList();
    }

    public async Task FnConfirmarAsync(long id, string? mensagem = null, CancellationToken ct = default) =>
        await FnAplicarTransicaoAsync(id, a => a.FnConfirmar(mensagem), ct);

    /// <summary>Só se aplica a uma solicitação do cliente (Status Pendente) — ver Agendamento.FnRejeitar.</summary>
    public async Task FnRejeitarAsync(long id, string? mensagem = null, CancellationToken ct = default) =>
        await FnAplicarTransicaoAsync(id, a => a.FnRejeitar(mensagem), ct);

    public async Task FnIniciarAtendimentoAsync(long id, CancellationToken ct = default) =>
        await FnAplicarTransicaoAsync(id, a => a.FnIniciarAtendimento(), ct);

    /// <summary>
    /// FnConcluir tem uma regra a mais que as outras transições: junto com
    /// marcar o agendamento como Concluido, já nasce um Pagamento
    /// Pendente pro valor cobrado — é o que faz o cliente aparecer na aba
    /// de Pagamentos como "aguardando pagamento" pro barbeiro conferir
    /// (ver PagamentoService). A forma de pagamento (Dinheiro/Pix/...)
    /// ainda não é conhecida aqui — só quando o pagamento for confirmado
    /// de verdade (Pagamento.FnConfirmarPagamento).
    /// </summary>
    public async Task FnConcluirAsync(long id, CancellationToken ct = default)
    {
        var agendamento = await _agendamentos.FnObterPorIdAsync(id, ct)
            ?? throw NotFoundException.FnPara("Agendamento", id);

        agendamento.FnConcluir();

        var pagamento = Pagamento.FnCriar(
            agendamento.ClienteId,
            agendamento.PrecoCobrado,
            FormaPagamento.Dinheiro,
            agendamentoId: agendamento.Id);
        pagamento.FnAtribuirEmpresa(agendamento.EmpresaId);

        await _pagamentos.FnAdicionarAsync(pagamento, ct);
        await _uow.FnSalvarAsync(ct);
    }

    public async Task FnCancelarAsync(long id, string? mensagem = null, CancellationToken ct = default) =>
        await FnAplicarTransicaoAsync(id, a => a.FnCancelar(mensagem), ct);

    public async Task FnMarcarNaoCompareceuAsync(long id, string? mensagem = null, CancellationToken ct = default) =>
        await FnAplicarTransicaoAsync(id, a => a.FnMarcarNaoCompareceu(mensagem), ct);

    private async Task FnAplicarTransicaoAsync(long id, Action<Agendamento> transicao, CancellationToken ct)
    {
        var agendamento = await _agendamentos.FnObterPorIdAsync(id, ct)
            ?? throw NotFoundException.FnPara("Agendamento", id);

        transicao(agendamento);
        await _uow.FnSalvarAsync(ct);
    }

    private static AgendamentoResponse FnMapear(Agendamento a) => new(
        a.Id, a.ClienteId, a.BarbeiroId, a.ServicoId, a.AssinaturaId, a.Inicio, a.Fim, a.Status, a.PrecoCobrado, a.Observacoes, a.MensagemResposta, a.AtualizadoEm);
}
