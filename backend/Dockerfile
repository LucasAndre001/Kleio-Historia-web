# ===================================================================
#  Kleio História — API (ASP.NET Core 10)
#  Build em duas etapas ("multi-stage"):
#   1) imagem com o SDK compila e publica a aplicação;
#   2) imagem final só com o runtime (menor e mais segura).
# ===================================================================

# ---------- Etapa 1: build ----------
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copiar só os .csproj primeiro aproveita o cache do Docker:
# o "restore" (download de pacotes) só roda de novo se as dependências mudarem.
COPY KleioHistoria.API/KleioHistoria.API.csproj KleioHistoria.API/
RUN dotnet restore KleioHistoria.API/KleioHistoria.API.csproj

COPY KleioHistoria.API/ KleioHistoria.API/
RUN dotnet publish KleioHistoria.API/KleioHistoria.API.csproj \
    -c Release -o /app/publish --no-restore /p:UseAppHost=false

# ---------- Etapa 2: runtime ----------
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

# curl é usado pelo healthcheck do docker-compose.
RUN apt-get update \
    && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*

# Pasta de uploads com dono = usuário sem privilégios da imagem ($APP_UID).
# Quando o volume nomeado é criado pela primeira vez, ele herda essas permissões.
RUN mkdir -p /app/uploads/imagens /app/uploads/pdfs && chown -R $APP_UID /app/uploads

COPY --from=build /app/publish .

ENV ASPNETCORE_HTTP_PORTS=8080 \
    Armazenamento__CaminhoRaiz=/app/uploads

EXPOSE 8080
USER $APP_UID

ENTRYPOINT ["dotnet", "KleioHistoria.API.dll"]
