@echo off
rem ==============================================================
rem  Kleio Historia - rodar o projeto no Docker
rem  Sobe o banco (PostgreSQL), a API e o site em containers.
rem  Site: http://localhost:4200   API: http://localhost:5080/swagger
rem  Para parar: Ctrl+C nesta janela (ou "docker compose down").
rem ==============================================================
cd /d "%~dp0"

if not exist ".env" (
    copy ".env.example" ".env" >nul
    echo Arquivo .env criado a partir do .env.example.
)

docker info >nul 2>&1
if errorlevel 1 goto sem_docker

echo Subindo os containers. Na primeira vez demora alguns minutos...
echo Quando aparecer que a API e o site iniciaram, abra http://localhost:4200
echo.
docker compose up --build
pause
exit /b

:sem_docker
echo O Docker nao esta rodando. Abra o Docker Desktop, espere iniciar e rode este arquivo de novo.
pause
exit /b 1
