using Barbearia.Application.Abstracoes;

namespace Barbearia.Api.Seguranca;

/// <summary>
/// Implementação de ICurrentTenantService que mora na Api (não na
/// Infrastructure nem na Application) porque é quem sabe COMO resolver
/// "qual barbearia é esta requisição" — a partir do JWT ou de um header
/// HTTP (ver EmpresaResolverMiddleware, o único lugar que ESCREVE nestas
/// propriedades; todo o resto do sistema só lê, através da interface).
///
/// Registrada como Scoped — uma instância por requisição HTTP, a MESMA
/// instância injetada tanto no middleware (que escreve) quanto no
/// BarbeariaDbContext/Services (que só leem), porque o container de DI
/// devolve a interface e a classe concreta apontando pro mesmo objeto
/// dentro do mesmo escopo (ver registro em Program.cs).
/// </summary>
public class CurrentTenantService : ICurrentTenantService
{
    public long? EmpresaId { get; set; }
    public bool EhSuperAdmin { get; set; }
}
