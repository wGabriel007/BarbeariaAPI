# Build de produção da Api — imagem final só com o runtime do .NET
# (nunca o SDK inteiro), pra ficar pequena e não carregar ferramenta de
# build pra dentro do que vai rodar de verdade.
#
# IMPORTANTE: o contexto deste build precisa ser a RAIZ do repositório
# (não api/src/Barbearia.Api), porque:
#   1) os 4 projetos (Domain/Application/Infrastructure/Api) são
#      irmãos dentro de api/src/, com ProjectReference relativo entre
#      eles;
#   2) Barbearia.Infrastructure.csproj embute os arquivos
#      database/*_migracao_*.sql como EmbeddedResource com um caminho
#      relativo que sobe até a raiz do repo (ver esse .csproj) — sem a
#      pasta database/ no contexto, o build "funciona" mas a Api sobe
#      sem nenhuma migração pra aplicar.
# O docker-compose.yml na raiz já configura isso certo
# (build.context: . / build.dockerfile: api/src/Barbearia.Api/Dockerfile);
# se for buildar na mão, use: docker build -f api/src/Barbearia.Api/Dockerfile .

# --- estágio 1: build/publish (usa o SDK, mais pesado) ---------------
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copia só os .csproj primeiro (cache de camada do Docker): o
# 'dotnet restore' só precisa ser refeito quando um .csproj muda, não
# a cada alteração de código C#, então isolar essa camada evita baixar
# os pacotes NuGet de novo em todo build durante o desenvolvimento.
COPY api/src/Barbearia.Domain/Barbearia.Domain.csproj api/src/Barbearia.Domain/
COPY api/src/Barbearia.Application/Barbearia.Application.csproj api/src/Barbearia.Application/
COPY api/src/Barbearia.Infrastructure/Barbearia.Infrastructure.csproj api/src/Barbearia.Infrastructure/
COPY api/src/Barbearia.Api/Barbearia.Api.csproj api/src/Barbearia.Api/
RUN dotnet restore api/src/Barbearia.Api/Barbearia.Api.csproj

# Agora o resto do código — e a pasta database/ (ver o comentário
# grande no topo do arquivo sobre o EmbeddedResource).
COPY api/src/ api/src/
COPY database/ database/

RUN dotnet publish api/src/Barbearia.Api/Barbearia.Api.csproj \
    -c Release \
    -o /app/publish \
    --no-restore

# --- estágio 2: runtime (só o ASP.NET Core, sem o SDK) ----------------
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

COPY --from=build /app/publish .

# wwwroot/uploads (fotos de usuário, logo, fotos da barbearia — ver
# Barbearia.Infrastructure/Armazenamento/ArmazenamentoArquivosLocal.cs)
# precisa sobreviver a um 'docker compose up' novo — o docker-compose.yml
# monta um volume nomeado exatamente aqui.
RUN mkdir -p /app/wwwroot/uploads

EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

ENTRYPOINT ["dotnet", "Barbearia.Api.dll"]
