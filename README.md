# Kleio História

Plataforma acadêmica de publicação e leitura de artigos históricos.

## Como rodar o projeto

```powershell
git clone https://github.com/SEU-USUARIO/kleio-historia.git
cd kleio-historia
```

## Opção 1: Docker

Dê dois cliques no **`iniciar-docker.bat`**, ou rode no terminal:

```powershell
copy .env.example .env        # só na primeira vez (Linux/macOS: cp .env.example .env)
docker compose up --build
```

| O quê | Endereço |
|---|---|
| Site | http://localhost:4200 |
| Área do professor | http://localhost:4200/admin/login |
| API (Swagger) | http://localhost:5080/swagger |

Para parar: `Ctrl+C` e depois `docker compose down`. Para apagar o banco e começar do zero: `docker compose down -v`.

## Opção 2: backend e frontend em terminais separados

### Primeira vez (configuração)

No PowerShell, na pasta do projeto:

```powershell
# 1. Banco PostgreSQL no Docker (porta 5433)
copy .env.example .env
docker compose up -d db

# 2. Configurações locais da API (ficam no "user-secrets" do .NET, fora do código)
cd backend\KleioHistoria.API
dotnet user-secrets set "ConnectionStrings:Padrao" "Host=localhost;Port=5433;Database=kleio_historia;Username=kleio;Password=troque_esta_senha_local"
dotnet user-secrets set "Jwt:Chave" "troque-por-uma-frase-longa-e-aleatoria-com-32+-caracteres"
dotnet user-secrets set "Admin:Email" "professor@kleio.local"
dotnet user-secrets set "Admin:Senha" "Kleio2026!"

# 3. Cria as tabelas com as migrations do Entity Framework
dotnet tool restore
dotnet ef database update --connection "Host=localhost;Port=5433;Database=kleio_historia;Username=kleio;Password=troque_esta_senha_local"
cd ..\..
```

A senha da connection string precisa ser a mesma do `POSTGRES_PASSWORD` no `.env`. Se usar um PostgreSQL instalado no PC em vez do Docker, troque por `Host=localhost;Port=5432;Database=kleio_historia;Username=postgres;Password=SUA_SENHA` nos dois comandos.

### Terminal 1: backend

```powershell
docker compose up -d db          # se o banco do Docker não estiver rodando
cd backend\KleioHistoria.API
dotnet run
```

A API sobe em http://localhost:5080 e o Swagger abre no navegador.

### Terminal 2: frontend

```powershell
cd frontend
npm install                      # só na primeira vez
npx ng serve --open
```

O site abre em http://localhost:4200. As chamadas para `/api` são repassadas para o backend pelo `proxy.conf.json`.

## Login de demonstração

`professor@kleio.local` / `Kleio2026!` (definido no `.env` ou no user-secrets). Na primeira execução são criados 4 artigos de exemplo: 3 publicados e 1 rascunho.

## Migrations (Entity Framework)

As migrations ficam em `backend/KleioHistoria.API/Data/Migrations`. A API também aplica as que estiverem pendentes ao iniciar. Para criar uma nova depois de alterar uma entidade:

```powershell
cd backend\KleioHistoria.API
dotnet ef migrations add NomeDaMudanca --output-dir Data/Migrations
dotnet ef database update --connection "Host=localhost;Port=5433;Database=kleio_historia;Username=kleio;Password=troque_esta_senha_local"
```

## Portas ocupadas?

No modo Docker, se 4200, 5080 ou 5433 já estiverem em uso, troque `FRONTEND_PORTA_HOST`, `API_PORTA_HOST` ou `POSTGRES_PORTA_HOST` no `.env`.
