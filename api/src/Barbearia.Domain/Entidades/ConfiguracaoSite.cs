using System.Text.RegularExpressions;
using Barbearia.Domain.Comum;

namespace Barbearia.Domain.Entidades;

/// <summary>
/// Configuração visual do site — UMA linha só (singleton, Id sempre
/// IdUnico), criada pela migração (ver 09_migracao_configuracao_site.sql)
/// e nunca inserida pela aplicação; por isso não tem um "Criar()" nem
/// entra em nenhum IRepositorio&lt;T&gt; genérico (ver IConfiguracaoSiteRepository,
/// que só sabe FnObterAsync — não existe "adicionar" nem "listar").
///
/// Só Admin altera (ver ConfiguracaoSiteService/Controller); qualquer
/// pessoa — inclusive deslogada, na tela de FnLogin — só LÊ, pra já
/// mostrar a marca certa da barbearia antes mesmo de entrar.
/// </summary>
public class ConfiguracaoSite
{
    public const long IdUnico = 1;

    /// <summary>
    /// Limite da galeria (ver FnAdicionarFoto) — o bastante pra mostrar o
    /// espaço sem virar um álbum infinito pra rolar. Público de propósito:
    /// ConfiguracaoSiteService confere isso ANTES de gastar um upload de
    /// arquivo pro armazenamento (ver FnAdicionarFotoAsync) — sem isso, uma
    /// tentativa de adicionar a 13ª foto salvaria o arquivo físico primeiro
    /// e só descobriria o limite atingido ao chamar FnAdicionarFoto abaixo,
    /// deixando um arquivo órfão em disco pra cada tentativa rejeitada.
    /// </summary>
    public const int MaximoFotos = 12;

    private const int TamanhoMaximoEndereco = 300;
    private const int TamanhoMaximoTelefone = 20;
    private const int TamanhoMaximoInstagram = 100;
    private const int TamanhoMaximoHorarioFuncionamento = 200;

    private static readonly Regex FormatoHex = new("^#[0-9a-fA-F]{6}$", RegexOptions.Compiled);

    public long Id { get; private set; } = IdUnico;
    public string NomeBarbearia { get; private set; } = "Barbearia";
    public string? LogoUrl { get; private set; }

    /// <summary>Cor de destaque (botões, ícones, item ativo do menu) em hex, ex.: "#334562" — null usa a paleta padrão do sistema.</summary>
    public string? CorPrimaria { get; private set; }

    /// <summary>
    /// Informações públicas do negócio (ver FnAtualizarInformacoes) — ao
    /// contrário de NomeBarbearia/LogoUrl/CorPrimaria (decisão de dono do
    /// negócio, só Admin — ver ConfiguracaoSiteController), estas aqui são
    /// operação do dia a dia: Admin OU Barbeiro configuram, na aba
    /// "Sobre a barbearia" (ver paginas/SobreABarbearia.jsx).
    /// </summary>
    public string? Descricao { get; private set; }
    public string? Endereco { get; private set; }
    public string? Telefone { get; private set; }
    public string? Instagram { get; private set; }
    public string? HorarioFuncionamento { get; private set; }

    // Mesma nota de EF Core que Barbeiro.Horarios: a propriedade pública
    // devolve o campo _fotos direto (não .AsReadOnly()), pra sempre ser a
    // MESMA instância de List<T> — é o que permite o EF Core materializar/
    // rastrear a coleção vinda do banco direto no campo privado.
    private readonly List<FotoBarbearia> _fotos = new();
    public IReadOnlyCollection<FotoBarbearia> Fotos => _fotos;

    private ConfiguracaoSite()
    {
    }

    public void FnAtualizar(string nomeBarbearia, string? corPrimaria)
    {
        if (string.IsNullOrWhiteSpace(nomeBarbearia))
            throw new DomainException("Nome da barbearia é obrigatório.");

        if (corPrimaria is not null && !FormatoHex.IsMatch(corPrimaria))
            throw new DomainException("Cor precisa estar no formato hexadecimal, ex.: #334562.");

        NomeBarbearia = nomeBarbearia.Trim();
        CorPrimaria = corPrimaria;
    }

    public void FnDefinirLogo(string logoUrl) => LogoUrl = logoUrl;
    public void FnRemoverLogo() => LogoUrl = null;

    /// <summary>
    /// Descrição, endereço, telefone, Instagram e horário de
    /// funcionamento — tudo opcional/texto livre (quem exibe decide o que
    /// fazer com um campo vazio, ex.: não mostrar a seção "Localização"
    /// sem endereço — ver SobreABarbearia.jsx), então aqui só limpamos
    /// espaços e cortamos o que passar do tamanho da coluna no banco.
    /// </summary>
    public void FnAtualizarInformacoes(string? descricao, string? endereco, string? telefone, string? instagram, string? horarioFuncionamento)
    {
        Descricao = FnLimpar(descricao, null);
        Endereco = FnLimpar(endereco, TamanhoMaximoEndereco);
        Telefone = FnLimpar(telefone, TamanhoMaximoTelefone);
        Instagram = FnLimpar(instagram, TamanhoMaximoInstagram);
        HorarioFuncionamento = FnLimpar(horarioFuncionamento, TamanhoMaximoHorarioFuncionamento);
    }

    /// <summary>Adiciona mais uma foto à galeria — sempre no fim (ver FotoBarbearia, sem coluna de ordem própria).</summary>
    public void FnAdicionarFoto(string url)
    {
        if (_fotos.Count >= MaximoFotos)
            throw new DomainException($"Máximo de {MaximoFotos} fotos na galeria — remova alguma antes de adicionar outra.");

        _fotos.Add(FotoBarbearia.FnCriar(Id, url));
    }

    public void FnRemoverFoto(long fotoId)
    {
        var foto = _fotos.FirstOrDefault(f => f.Id == fotoId)
            ?? throw new DomainException("Esta foto não existe.");

        _fotos.Remove(foto);
    }

    private static string? FnLimpar(string? valor, int? tamanhoMaximo)
    {
        if (string.IsNullOrWhiteSpace(valor))
            return null;

        var limpo = valor.Trim();
        return tamanhoMaximo is int max && limpo.Length > max ? limpo[..max] : limpo;
    }
}
