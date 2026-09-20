using Barbearia.Application.Abstracoes;
using Barbearia.Application.Comum;
using Barbearia.Application.Dtos.Ranking;
using Barbearia.Domain.Comum;
using Barbearia.Domain.Entidades;

namespace Barbearia.Application.Servicos;

/// <summary>
/// Ranking de Clientes: qual CLIENTE mais voltou pra cortar no mês — pra
/// incentivar o público a frequentar a barbearia (quanto mais vezes vier,
/// mais perto fica de ganhar o prêmio do mês). A contagem NUNCA é
/// guardada — é sempre recalculada na hora, direto de Agendamento
/// (Status=Concluido), pra qualquer mês, passado ou presente. A única
/// coisa que precisa ficar salva é o PRÊMIO (PremioRanking), porque esse
/// sim é reconfigurado do zero a cada mês pelo Admin/Barbeiro e se
/// perderia se não fosse persistido (ver FnDefinirPremiosAsync).
/// </summary>
public class RankingService
{
    private readonly IAgendamentoRepository _agendamentos;
    private readonly IClienteRepository _clientes;
    private readonly IUsuarioRepository _usuarios;
    private readonly IPremioRankingRepository _premios;
    private readonly ICurrentTenantService _tenant;
    private readonly IUnitOfWork _uow;

    public RankingService(
        IAgendamentoRepository agendamentos,
        IClienteRepository clientes,
        IUsuarioRepository usuarios,
        IPremioRankingRepository premios,
        ICurrentTenantService tenant,
        IUnitOfWork uow)
    {
        _agendamentos = agendamentos;
        _clientes = clientes;
        _usuarios = usuarios;
        _premios = premios;
        _tenant = tenant;
        _uow = uow;
    }

    /// <summary>GET /api/ranking — pódio de um mês (mes/ano nulos = mês corrente).</summary>
    public async Task<RankingMensalResponse> FnObterRankingAsync(int? mes, int? ano, CancellationToken ct = default)
    {
        var agora = DateTimeOffset.UtcNow;
        return await FnMontarRankingAsync(mes ?? agora.Month, ano ?? agora.Year, ct);
    }

    /// <summary>
    /// "Controle de quem foi campeão nos últimos meses": varre pra trás a
    /// partir do mês ANTERIOR ao atual (o corrente ainda está em
    /// andamento — não tem campeão definitivo até fechar), pulando meses
    /// sem nenhum cliente com corte concluído. Devolve só a 1ª posição de
    /// cada mês; o pódio completo daquele mês dá pra ver chamando
    /// FnObterRankingAsync com aquele mes/ano.
    /// </summary>
    public async Task<List<CampeaoHistoricoResponse>> FnObterHistoricoAsync(int quantidadeMeses = 12, CancellationToken ct = default)
    {
        var historico = new List<CampeaoHistoricoResponse>();
        var agora = DateTimeOffset.UtcNow;

        for (var i = 1; i <= quantidadeMeses; i++)
        {
            var referencia = agora.AddMonths(-i);
            var ranking = await FnMontarRankingAsync(referencia.Month, referencia.Year, ct);
            var campeao = ranking.Posicoes.FirstOrDefault(p => p.Posicao == 1);

            if (campeao is not null)
            {
                historico.Add(new CampeaoHistoricoResponse(
                    ranking.Mes, ranking.Ano, campeao.ClienteId, campeao.NomeCliente,
                    campeao.FotoUrl, campeao.TotalCortes, campeao.Premio));
            }
        }

        return historico;
    }

    /// <summary>
    /// Reconfigura de uma vez todos os prêmios do mês ATUAL — nunca de um
    /// mês passado, já fechado. É o que garante o "reinicia todo mês": se
    /// ninguém chamar isto no mês novo, o ranking segue funcionando (a
    /// contagem de cortes não depende de prêmio nenhum), só que sem
    /// prêmio nenhuma posição, até alguém configurar.
    /// </summary>
    public async Task<RankingMensalResponse> FnDefinirPremiosAsync(DefinirPremiosRequest request, CancellationToken ct = default)
    {
        if (request.Premios.Select(p => p.Posicao).Distinct().Count() != request.Premios.Count)
            throw new DomainException("Não é possível repetir a mesma posição duas vezes.");

        var agora = DateTimeOffset.UtcNow;
        var mes = agora.Month;
        var ano = agora.Year;

        var existentes = await _premios.FnListarPorMesAsync(mes, ano, ct);

        foreach (var item in request.Premios)
        {
            var existente = existentes.FirstOrDefault(p => p.Posicao == item.Posicao);
            if (existente is not null)
                existente.FnAtualizarDescricao(item.Descricao);
            else
            {
                var novoPremio = PremioRanking.FnCriar(mes, ano, item.Posicao, item.Descricao);
                novoPremio.FnAtribuirEmpresa(_tenant.EmpresaId!.Value);
                await _premios.FnAdicionarAsync(novoPremio, ct);
            }
        }

        // Posição que estava configurada mas não veio mais no request:
        // o Admin/Barbeiro decidiu tirá-la do pódio deste mês.
        var posicoesEnviadas = request.Premios.Select(p => p.Posicao).ToHashSet();
        foreach (var removido in existentes.Where(p => !posicoesEnviadas.Contains(p.Posicao)))
            _premios.FnRemover(removido);

        await _uow.FnSalvarAsync(ct);

        return await FnMontarRankingAsync(mes, ano, ct);
    }

    private async Task<RankingMensalResponse> FnMontarRankingAsync(int mes, int ano, CancellationToken ct)
    {
        var inicioMes = new DateTimeOffset(ano, mes, 1, 0, 0, 0, TimeSpan.Zero);
        var inicioProximoMes = inicioMes.AddMonths(1);

        var concluidos = await _agendamentos.FnListarConcluidosPorPeriodoAsync(inicioMes, inicioProximoMes, ct);
        var totalPorCliente = concluidos.GroupBy(a => a.ClienteId).ToDictionary(g => g.Key, g => g.Count());

        var premios = await _premios.FnListarPorMesAsync(mes, ano, ct);
        var premioPorPosicao = premios.ToDictionary(p => p.Posicao, p => p.Descricao);

        // Só entra no pódio quem realmente cortou pelo menos 1 vez neste
        // mês — diferente do Ranking de Barbeiros (poucos, sempre listados
        // todos), aqui listar TODO cliente cadastrado encheria a tela de
        // gente com 0 cortes.
        var idsComCorte = totalPorCliente.Keys.ToList();
        var todosClientes = await _clientes.FnListarAsync(ct);
        var participantes = todosClientes.Where(c => idsComCorte.Contains(c.Id)).ToList();

        // Nem todo cliente tem conta de login vinculada (a maioria é
        // cadastrada manualmente pelo staff, sem Usuario) — só busca a
        // foto de quem tem, e só entre os que realmente aparecem no
        // pódio deste mês (nunca todo o cadastro de clientes).
        var fotoPorUsuarioId = new Dictionary<long, string?>();
        foreach (var usuarioId in participantes.Where(c => c.UsuarioId.HasValue).Select(c => c.UsuarioId!.Value).Distinct())
        {
            var usuario = await _usuarios.FnObterPorIdAsync(usuarioId, ct);
            fotoPorUsuarioId[usuarioId] = usuario?.FotoUrl;
        }

        var posicoes = participantes
            .Select(c => new { Cliente = c, Total = totalPorCliente[c.Id] })
            .OrderByDescending(x => x.Total)
            .ThenBy(x => x.Cliente.NomeCompleto)
            .Select((x, indice) =>
            {
                var posicao = indice + 1;
                var foto = x.Cliente.UsuarioId.HasValue ? fotoPorUsuarioId.GetValueOrDefault(x.Cliente.UsuarioId.Value) : null;

                return new PosicaoRankingResponse(
                    posicao, x.Cliente.Id, x.Cliente.NomeCompleto, foto, x.Total,
                    premioPorPosicao.GetValueOrDefault(posicao));
            })
            .ToList();

        var agora = DateTimeOffset.UtcNow;
        var mesAtual = mes == agora.Month && ano == agora.Year;

        return new RankingMensalResponse(mes, ano, mesAtual, posicoes);
    }
}
