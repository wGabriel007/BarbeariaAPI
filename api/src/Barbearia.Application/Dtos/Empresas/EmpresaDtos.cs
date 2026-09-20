using Barbearia.Domain.Enumeracoes;

namespace Barbearia.Application.Dtos.Empresas;

/// <summary>
/// Criada pelo SuperAdmin (ver EmpresasController) — de um golpe só nasce
/// a Empresa, a primeira conta Admin dela (login/senha que o SuperAdmin
/// escolhe pra passar pro dono da barbearia) e a configuração inicial do
/// site (com o nome exibido = Nome da barbearia). O dono troca a própria
/// senha depois, na aba "Meu perfil".
/// </summary>
public sealed record CriarEmpresaRequest(
    string Nome,
    string Slug,
    string EmailAdmin,
    string SenhaAdmin,
    string NomeCompletoAdmin);

public sealed record EmpresaResponse(long Id, string Nome, string Slug, StatusRegistro Status, DateTimeOffset CriadoEm);

/// <summary>Devolvida só na criação — é a única vez que a senha em texto puro (a que o SuperAdmin escolheu) faz sentido aparecer numa resposta, pra ele poder repassar ao dono da barbearia.</summary>
public sealed record EmpresaCriadaResponse(EmpresaResponse Empresa, string EmailAdmin, string SenhaAdmin);
