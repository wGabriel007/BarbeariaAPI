using Barbearia.Domain.Entidades;
using Barbearia.Domain.Enumeracoes;
using Xunit;

namespace Barbearia.Tests.Dominio;

public class BarbeiroTests
{
    [Fact]
    public void FnCriar_NasceAtivoENaoAusente()
    {
        var barbeiro = Barbeiro.FnCriar(usuarioId: 1);

        Assert.Equal(StatusRegistro.Ativo, barbeiro.Status);
        Assert.False(barbeiro.Ausente);
    }

    [Fact]
    public void FnMarcarAusente_DepoisMarcarPresente_VoltaAoNormal()
    {
        var barbeiro = Barbeiro.FnCriar(usuarioId: 1);

        barbeiro.FnMarcarAusente();
        Assert.True(barbeiro.Ausente);

        barbeiro.FnMarcarPresente();
        Assert.False(barbeiro.Ausente);
    }

    // Ausente é À PARTE de Status — inativar/bloquear o cadastro não deve
    // mexer nessa flag sozinho (nem o inverso), são dois conceitos
    // independentes (ver comentário em Barbeiro.Ausente).
    [Fact]
    public void FnMarcarAusente_NaoMudaStatus()
    {
        var barbeiro = Barbeiro.FnCriar(usuarioId: 1);

        barbeiro.FnMarcarAusente();

        Assert.Equal(StatusRegistro.Ativo, barbeiro.Status);
    }
}
