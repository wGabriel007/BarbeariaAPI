# Barbearia API — Fase 2

API em .NET 8 (Clean Architecture) para o sistema de barbearia. Esta fase
assume que você já tem o banco criado (Fase 1 — pasta `../database`).

## Por que você precisa compilar isso, e eu não

Este código foi escrito num ambiente sem acesso à internet (o proxy de
rede bloqueia o NuGet), então **nunca consegui rodar `dotnet restore` /
`dotnet build` nas camadas Infrastructure e Api**. Domain e Application
foram compilados e testados aqui (não dependem de nenhum pacote externo).
Se o `dotnet build` no seu computador apontar algum erro nessas duas
camadas, me mande a mensagem de erro exata e eu corrijo.

## Estrutura

```
api/
├── Barbearia.sln
├── src/
│   ├── Barbearia.Domain          # Entidades, enums, regras de negócio "puras"
│   ├── Barbearia.Application     # Casos de uso (Services), DTOs, interfaces de repositório
│   ├── Barbearia.Infrastructure  # EF Core + Npgsql, repositórios, hash de senha
│   └── Barbearia.Api             # Controllers, Program.cs, Swagger
└── tests/
    └── Barbearia.Tests           # Testes de unidade (xUnit) do Domain
```

A dependência entre camadas só aponta pra dentro: `Api → Infrastructure
→ Application → Domain`. O Domain não conhece nenhuma das outras — é por
isso que ele compila sem nenhum pacote NuGet.

## Passo a passo para rodar

### 1. Pré-requisitos
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- PostgreSQL com o banco `barbearia` já criado (rode `01_schema.sql` e,
  se quiser dados de exemplo, `02_seed.sql` — pasta `../database`)

### 2. Restaurar e compilar

```bash
cd api
dotnet restore
dotnet build
```

Se der erro de pacote (`NU1101`, `NU1301`, etc.), é sinal de que sua
máquina não está com acesso ao nuget.org — confira sua conexão.

### 3. Configurar a connection string e a chave do JWT (arquivo `.env`)

**Nunca** deixe senha de banco ou chave de assinatura dentro de um
arquivo que vai pro Git. Igual ao front (`frontend/.env`), a Api lê um
arquivo `.env` — dentro de `src/Barbearia.Api`:

```bash
cd src/Barbearia.Api
cp .env.example .env
```

Abra o `.env` recém-criado e troque os dois valores de exemplo:

```
ConnectionStrings__Barbearia=Host=localhost;Port=5432;Database=barbearia;Username=postgres;Password=SUA_SENHA_AQUI
Jwt__Key=troque-por-uma-string-aleatoria-de-pelo-menos-32-caracteres
```

- Na `ConnectionStrings__Barbearia`, troque `Password=SUA_SENHA_AQUI`
  pela senha real do seu Postgres (e `Username=postgres` se usar outro
  usuário).
- Na `Jwt__Key`, cole qualquer string aleatória de pelo menos 32
  caracteres (pode gerar uma em <https://www.uuidgenerator.net/> ou só
  digitar uma frase longa sem espaço).

O `.env` já está no `.gitignore` — ele nunca vai pro Git; só o
`.env.example` (com valores fictícios) fica versionado, servindo de
modelo. O nome com `__` (dois underscores) é assim mesmo: é como se
escreve `ConnectionStrings:Barbearia`/`Jwt:Key` (com `:`) numa variável
de ambiente — o ASP.NET Core já sabe interpretar isso sem nenhum código
nosso, exceto uma linha em `Program.cs` que carrega o arquivo `.env`
para dentro das variáveis de ambiente do processo antes de tudo o resto.

Prefere usar `dotnet user-secrets` em vez de `.env`? Funciona igual —
são só duas formas diferentes de entregar a mesma configuração:
```bash
dotnet user-secrets init
dotnet user-secrets set "ConnectionStrings:Barbearia" "Host=localhost;Port=5432;Database=barbearia;Username=postgres;Password=SUA_SENHA_AQUI"
dotnet user-secrets set "Jwt:Key" "troque-por-uma-string-aleatoria-de-pelo-menos-32-caracteres"
```

### 4. Rodar

```bash
dotnet run --project src/Barbearia.Api
```

A API sobe em `http://localhost:5042` (ver
`src/Barbearia.Api/Properties/launchSettings.json`) e abre automaticamente
o Swagger em `http://localhost:5042/swagger` — de lá você já consegue
testar todos os endpoints direto do navegador, sem precisar escrever
nenhum código de front ainda.

Tem também um arquivo `src/Barbearia.Api/Barbearia.Api.http` com alguns
exemplos de requisição prontos (funciona direto no VS Code com a extensão
REST Client, ou no Rider/Visual Studio nativamente).

### 5. Rodar os testes

```bash
dotnet test
```

## O que a API expõe (visão geral)

**Toda rota exige um token (JWT) no header `Authorization: Bearer <token>`,
exceto `/api/auth/*`** — ver a seção de Login abaixo. Isso é configurado
uma vez, como padrão, em `Program.cs` (`FallbackPolicy`), então uma rota
nova que você criar já nasce protegida sem precisar lembrar de marcar
`[Authorize]` nela.

**Além de exigir login, boa parte das rotas também exige um papel
específico** (`[Authorize(Roles = "Admin,Barbeiro")]` na Controller ou na
ação) — a regra geral do sistema é: **Admin e Barbeiro têm o mesmo nível
de acesso** (gerenciam tudo), **Comum só consegue ler e marcar/cancelar o
próprio horário**. Uma chamada autenticada mas sem o papel certo recebe
`403 Forbidden`, diferente de `401` (sem token/token inválido).

| Recurso | Rota base | Quem acessa | Observação |
|---|---|---|---|
| Autenticação | `/api/auth` | Qualquer um | `POST /login`, `POST /registrar` — as únicas rotas públicas |
| Usuários | `/api/usuarios` | Admin/Barbeiro | Cadastro de contas — pra criar login de barbeiro/admin depois de já estar logado |
| Barbeiros | `/api/barbeiros` | Leitura: todos · Escrita: Admin/Barbeiro | Criado a partir de um Usuario com `tipo: "Barbeiro"` |
| Clientes | `/api/clientes` | Leitura: todos · Escrita: Admin/Barbeiro | CRUD + ativar/inativar/bloquear |
| Serviços | `/api/servicos` | Leitura: todos · Escrita: Admin/Barbeiro | Catálogo (corte, barba...) usado para calcular preço/duração dos agendamentos |
| Planos de assinatura | `/api/planos-assinatura` | Leitura: todos · Escrita: Admin/Barbeiro | Um plano inclui um ou mais serviços com limite mensal |
| Assinaturas | `/api/assinaturas` | Leitura: todos · Escrita: Admin/Barbeiro | Vínculo de um cliente a um plano |
| Agendamentos | `/api/agendamentos` | Ver observação | Cria, lista por barbeiro/período ou por cliente, e tem a máquina de estados (confirmar → iniciar atendimento → concluir, ou cancelar/não compareceu). `Criar` e `Cancelar` ficam abertos pra todo mundo (é o "marcar/cancelar meu horário"); as outras 4 transições exigem Admin/Barbeiro |
| Pagamentos | `/api/pagamentos` | Admin/Barbeiro | Só leitura + confirmar/cancelar — nasce sozinho quando um agendamento é concluído (ver seção própria abaixo) |

Todo enum (`status`, `tipo`, etc.) aparece no JSON como texto (`"Ativo"`,
`"Confirmado"`) em vez de número — configurado uma vez em `Program.cs`.

Erros de negócio voltam com o HTTP status certo, não um 500 genérico:

- `400 Bad Request` — dado violou uma regra de negócio (`DomainException`)
- `404 Not Found` — o id pedido não existe
- `409 Conflict` — o barbeiro já tem outro agendamento nesse horário

## Como funciona o login

- `POST /api/auth/registrar` — cria a conta e já devolve `{ token, usuario }`.
  O corpo NÃO aceita mais um campo `tipo` (de propósito, por segurança —
  se aceitasse, bastaria editar o JSON da requisição pra virar Admin). O
  tipo é decidido pelo servidor: a **primeira** conta criada no sistema
  vira `Admin` automaticamente (precisa de alguém pra administrar desde
  o início); toda conta seguinte que se autocadastrar por aqui nasce
  `Comum`. Quem quiser virar `Barbeiro` ou `Admin` depois precisa ser
  promovido por um Admin já existente (`POST /api/usuarios`, que aceita
  o `tipo` que quiser, mas exige token — ou direto no banco).
- `POST /api/auth/login` — mesma resposta, pra quem já tem conta. Se a
  conta estiver com status `Inativo` ou `Bloqueado`, o login é recusado.
- O token dura 8h (`Jwt:ExpiraMinutos`) e carrega o id, e-mail, nome e
  tipo do usuário — isso é o que permite, por exemplo, restringir uma
  rota só pra `"Admin"` no futuro com `[Authorize(Roles = "Admin")]`,
  sem precisar consultar o banco de novo em cada requisição.
- **Usuário bloqueado/inativo perde o acesso na hora, mesmo já logado**:
  como o token é "stateless", ele continuaria válido até expirar (até
  8h) mesmo que um Admin bloqueie a conta logo depois de emitido. Pra
  fechar essa brecha, todo request autenticado passa por
  `StatusUsuarioMiddleware` (`src/Barbearia.Api/Middleware`), que
  confere o status do usuário no banco a cada requisição e corta com
  `401` se ele não estiver mais `Ativo` — inclusive pra marcar horário.

## Como funciona o status de pagamento

- Um `Pagamento` **nasce sozinho, com status `Pendente`**, toda vez que
  um agendamento é concluído (`POST /api/agendamentos/5/concluir` —
  ver `AgendamentoService.ConcluirAsync`). Não existe
  `POST /api/pagamentos` pra criar um na mão de propósito: o valor já
  vem do `PrecoCobrado` do próprio agendamento.
- `GET /api/pagamentos?de=&ate=` — lista os pagamentos criados nesse
  intervalo (mesmo formato de `de`/`ate` que
  `GET /api/agendamentos/por-barbeiro/5` já usa). O front manda um dia
  só pra aba "Hoje" e vários dias pra aba "Histórico" — não tem
  paginação especial: como o filtro é por data, a lista de "hoje" já
  fica vazia sozinha no dia seguinte.
- `POST /api/pagamentos/5/confirmar` com `{ "forma": "Pix" }` — marca
  como `Pago` (a forma de pagamento só é registrada aqui, porque só se
  sabe como o cliente pagou nesse momento, não quando o pagamento
  nasceu `Pendente`).
- `POST /api/pagamentos/5/cancelar` — marca como `Cancelado` (cliente
  não pagou / ficou devendo).

## O que ficou de fora desta fase (de propósito)

- **Pagamento de assinatura** — o `Pagamento` já aceita ser vinculado a
  uma `Assinatura` em vez de um `Agendamento` (ver `Pagamento.Criar` no
  Domain), mas nada ainda gera esse tipo de cobrança automaticamente
  (ex.: todo dia N do mês). Fica pra depois, se fizer sentido pro seu
  fluxo.

## Próximo passo

Depois de rodar isso e confirmar que está tudo funcionando (ou me mandar
os erros pra eu corrigir), seguimos para a **Fase 3: o front-end em
React + Tailwind**, consumindo esta API.
