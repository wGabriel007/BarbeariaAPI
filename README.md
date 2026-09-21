# Barbearia API

API em **.NET 8** (Clean Architecture) que dá suporte ao sistema de gestão
de barbearias — agora uma **plataforma multi-barbearia** (multi-tenant):
várias barbearias diferentes, cada uma com seus próprios clientes,
barbeiros, agendamentos e configurações, todas isoladas entre si, rodando
no mesmo banco e na mesma Api.

## Stack

- **.NET 8** / C#
- **Entity Framework Core 8** + **Npgsql** (PostgreSQL)
- **PostgreSQL** (hospedado no [Neon](https://neon.tech) em produção)
- **JWT** para autenticação
- **BCrypt** para hash de senha
- Deploy via **Docker** no [Render](https://render.com)

## Arquitetura

Clean Architecture em 4 camadas, cada uma só conhecendo a de dentro:

```
Api → Infrastructure → Application → Domain
```

```
api/
├── Barbearia.sln
├── src/
│   ├── Barbearia.Domain          # Entidades, enums, regras de negócio "puras" — sem depender de nenhum pacote externo
│   ├── Barbearia.Application     # Casos de uso (Services), DTOs, interfaces de repositório
│   ├── Barbearia.Infrastructure  # EF Core + Npgsql, repositórios, hash de senha, runner de migração
│   └── Barbearia.Api             # Controllers, Program.cs, Swagger, middlewares
└── tests/
    └── Barbearia.Tests           # Testes de unidade (xUnit) do Domain
```

O `Domain` não referencia nenhum pacote NuGet — é por isso que compila
sozinho, sem depender de internet.

## Multi-barbearia (multi-tenant)

Cada barbearia é uma linha na tabela `empresas`, identificada por um
`slug` único (a parte do link: `/barbearia-do-joao`). Toda tabela que
pertence a uma barbearia específica (`usuarios`, `clientes`,
`agendamentos`, `servicos`, etc.) tem uma coluna `empresa_id`.

O isolamento é automático: `BarbeariaDbContext` aplica um
`HasQueryFilter` por entidade, resolvido a partir de:

- **Requisição autenticada** → o `empresa_id` vem de dentro do próprio
  token JWT.
- **Requisição anônima** (login, cadastro, ver site antes de entrar) →
  vem do header `X-Empresa-Slug`, que o front manda sempre.

Isso significa que **nenhuma consulta LINQ precisa filtrar por
`empresa_id` manualmente** — o EF Core já devolve só as linhas da
barbearia da requisição atual, em qualquer Repositorio.

Existe um papel especial, **SuperAdmin**, que é dono da plataforma (não
de uma barbearia) — cadastra/ativa/desativa barbearias pela rota
`/api/empresas`, e ignora esse filtro (`EhSuperAdmin`). Só nasce via SQL
direto (ver `database/criar_superadmin.sql`), não existe autocadastro.

## Como rodar localmente

### Pré-requisitos
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- PostgreSQL com o banco criado (rode `../database/01_schema.sql` e, se
  quiser dados de exemplo, `02_seed.sql`)

### Restaurar e compilar
```bash
cd api
dotnet restore
dotnet build
```

### Configurar variáveis de ambiente

A Api lê um arquivo `.env` dentro de `src/Barbearia.Api` (nunca vai pro
Git — só o `.env.example`, com valores fictícios, fica versionado):

```bash
cd src/Barbearia.Api
cp .env.example .env
```

Edite o `.env`:
```
ConnectionStrings__Barbearia=Host=localhost;Port=5432;Database=barbearia;Username=postgres;Password=SUA_SENHA_AQUI
Jwt__Key=troque-por-uma-string-aleatoria-de-pelo-menos-32-caracteres
```

### Rodar
```bash
dotnet run --project src/Barbearia.Api
```

Sobe em `http://localhost:5042` e abre o Swagger em
`http://localhost:5042/swagger`, de onde já dá pra testar todos os
endpoints.

### Testes
```bash
dotnet test
```

## Banco de dados e migrações

O schema **não** usa EF Core Migrations — é gerenciado em SQL puro,
pasta `../database/`:

- `01_schema.sql` / `02_seed.sql` — criam um banco do ZERO (rodado à mão,
  uma única vez, num banco novo).
- `NN_migracao_*.sql` — mudanças incrementais no schema (uma por
  funcionalidade nova). Essas a própria Api aplica sozinha ao subir
  (`MigrationRunner`, chamado no `Program.cs` antes de `app.Run()`),
  numa tabela de controle `migracoes_aplicadas` — nada precisa ser
  rodado manualmente pra isso.
- `criar_superadmin.sql` — não é migração (não roda sozinho de
  propósito): comando manual, único, pra criar a primeira conta
  SuperAdmin da plataforma.

## Autenticação e papéis

Todo endpoint exige um token JWT no header `Authorization: Bearer
<token>`, exceto `/api/auth/*`. Quatro papéis (`TipoUsuario`):

| Papel | O que faz |
|---|---|
| **SuperAdmin** | Dono da plataforma — gerencia as barbearias em si (`/api/empresas`). Não pertence a nenhuma barbearia. Login separado: `POST /api/auth/login-admin`. |
| **Admin** | Dono/gestor de UMA barbearia — acesso total dentro dela. |
| **Barbeiro** | Mesmo nível de acesso que Admin dentro da barbearia (gerencia agenda, clientes, etc.). |
| **Comum** | Cliente da barbearia — só marca/cancela o próprio horário e vê os próprios dados. |

Uma chamada autenticada mas sem o papel certo recebe `403`; sem
token/token inválido recebe `401`. Conta bloqueada/inativa perde acesso
na hora, mesmo com token ainda válido (`StatusUsuarioMiddleware` confere
o status a cada requisição).

## Principais recursos

| Recurso | Rota base | Observação |
|---|---|---|
| Autenticação | `/api/auth` | `login`, `registrar`, `login-admin` (SuperAdmin) — únicas rotas públicas |
| Empresas (barbearias) | `/api/empresas` | Só SuperAdmin — criar/listar/ativar/inativar barbearias |
| Usuários | `/api/usuarios` | Cadastro de contas (barbeiro/admin) dentro da barbearia |
| Barbeiros | `/api/barbeiros` | Perfil, horários de trabalho, ausências |
| Clientes | `/api/clientes` | CRUD + ativar/inativar/bloquear |
| Serviços | `/api/servicos` | Catálogo (corte, barba...), preço, categoria |
| Planos de assinatura | `/api/planos-assinatura` | Um plano inclui um ou mais serviços com limite mensal |
| Assinaturas | `/api/assinaturas` | Vínculo de um cliente a um plano |
| Solicitações de plano | `/api/solicitacoes-planos` | Cliente pede um plano, staff aceita/rejeita |
| Agendamentos | `/api/agendamentos` | Máquina de estados: solicitar/criar → confirmar → iniciar → concluir, ou cancelar/não compareceu. Inclui fila do dia (`minha-fila`) |
| Pagamentos | `/api/pagamentos` | Nasce sozinho quando um agendamento é concluído |
| Ranking | `/api/ranking` | Ranking mensal de clientes por corte, com prêmios configuráveis (isolado por barbearia) |
| Configuração do site | `/api/configuracao-site` | Nome, logo, cor, descrição, fotos da barbearia (área pública de cada uma) |
| Perfil | `/api/perfil` | Dados/senha/foto do usuário logado |

Erros de negócio voltam com o status certo (`400` regra violada, `404`
não encontrado, `409` conflito de horário), não um `500` genérico.

## Deploy

- **Api**: Docker no Render (`src/Barbearia.Api/Dockerfile`). A cada
  push no branch conectado, o Render builda e sobe sozinho; a Api aplica
  as migrações pendentes automaticamente ao iniciar.
- **Banco**: PostgreSQL no Neon.
- Variáveis de ambiente (`ConnectionStrings__Barbearia`, `Jwt__Key`)
  configuradas direto no painel do Render, não em `.env` (esse fica só
  pro ambiente local).
