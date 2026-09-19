using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Barbearia.Application.Comum;
using Barbearia.Domain.Entidades;
using Microsoft.IdentityModel.Tokens;

namespace Barbearia.Infrastructure.Seguranca;

public class JwtTokenGenerator : IJwtTokenGenerator
{
    private readonly JwtSettings _settings;

    public JwtTokenGenerator(JwtSettings settings)
    {
        _settings = settings;
    }

    public string FnGerarToken(Usuario usuario)
    {
        // Claims = as informações que ficam "dentro" do token, assinadas
        // (não podem ser alteradas sem invalidar a assinatura). A Api
        // usa ClaimTypes.Role pra restringir rotas por tipo de usuário
        // depois (ex.: [Authorize(Roles = "Admin")]), sem precisar
        // consultar o banco de novo em cada requisição — o token já
        // "carrega" quem é a pessoa e qual o tipo dela.
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, usuario.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, usuario.Email),
            new Claim(ClaimTypes.Name, usuario.NomeCompleto),
            new Claim(ClaimTypes.Role, usuario.Tipo.ToString()),
        };

        var chave = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_settings.Key));
        var credenciais = new SigningCredentials(chave, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _settings.Issuer,
            audience: _settings.Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(_settings.ExpiraMinutos),
            signingCredentials: credenciais);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
