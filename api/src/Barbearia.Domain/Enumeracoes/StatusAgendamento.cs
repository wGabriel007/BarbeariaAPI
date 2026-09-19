namespace Barbearia.Domain.Enumeracoes;

/// <summary>
/// Espelha o CHECK de agendamentos.status no 01_schema.sql. Cancelado,
/// NaoCompareceu e Rejeitado são os três status que o EXCLUDE constraint
/// do banco ignora ao checar conflito de horário (eles "liberam" o
/// horário) — Pendente NÃO libera: enquanto o barbeiro não confirma ou
/// rejeita, o horário fica reservado, senão dois clientes poderiam
/// solicitar o mesmo horário e só descobrir o conflito na hora de
/// confirmar.
///
/// Dois jeitos de nascer um Agendamento (ver Agendamento.FnCriar vs.
/// Agendamento.FnSolicitar): quem o STAFF cria diretamente (walk-in,
/// telefone) nasce Agendado, igual sempre foi. Quem o próprio CLIENTE
/// (Comum) solicita nasce Pendente, e só chega a Confirmado depois que
/// um Admin/Barbeiro aprova (ou vira Rejeitado, se recusarem).
/// </summary>
public enum StatusAgendamento
{
    Agendado = 0,
    Confirmado = 1,
    EmAtendimento = 2,
    Concluido = 3,
    Cancelado = 4,
    NaoCompareceu = 5,
    Pendente = 6,
    Rejeitado = 7
}
