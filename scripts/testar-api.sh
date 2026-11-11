#!/usr/bin/env bash
# ===================================================================
#  Teste de fumaça da API do Kleio História (cenários da seção 18).
#  Requer a aplicação em execução (docker compose up) e o curl.
#  No Windows, rode pelo Git Bash ou WSL.
#
#    ./scripts/testar-api.sh                       (usa os valores do .env)
#    API=http://localhost:5080 ./scripts/testar-api.sh
# ===================================================================
set -u

# Lê ADMIN_EMAIL / ADMIN_SENHA / API_PORTA_HOST do .env, se existir.
if [ -f .env ]; then
  set -a; . ./.env; set +a
fi
API="${API:-http://localhost:${API_PORTA_HOST:-5080}}"
EMAIL="${ADMIN_EMAIL:-professor@kleio.local}"
SENHA="${ADMIN_SENHA:-Kleio2026!}"

ok=0; falhas=0
passo() { printf '\n\033[1m%s\033[0m\n' "$1"; }
confere() { # descricao, esperado, obtido
  if [ "$2" = "$3" ]; then ok=$((ok+1)); printf '  \033[32m✓\033[0m %s\n' "$1";
  else falhas=$((falhas+1)); printf '  \033[31m✗\033[0m %s (esperado %s, obtido %s)\n' "$1" "$2" "$3"; fi
}
status() { curl -s -o /dev/null -w '%{http_code}' "$@"; }

passo "0. Saúde da API ($API)"
confere "GET /health" 200 "$(status "$API/health")"

passo "1. Listagem pública"
confere "GET /api/artigos" 200 "$(status "$API/api/artigos")"
confere "pesquisa sem acento (revolucao)" 200 "$(status "$API/api/artigos?busca=revolucao")"
confere "filtro por categoria" 200 "$(status --get --data-urlencode 'categoria=Brasil Império' "$API/api/artigos")"
corpo=$(curl -s "$API/api/artigos?busca=bastilha")
case "$corpo" in *revolucao-francesa*) r=sim;; *) r=nao;; esac
confere "busca encontra palavra no conteúdo do capítulo" sim "$r"

passo "2. Leitura por slug"
confere "artigo publicado" 200 "$(status "$API/api/artigos/revolucao-francesa")"
confere "rascunho não é público" 404 "$(status "$API/api/artigos/roma-antiga")"
confere "slug inexistente" 404 "$(status "$API/api/artigos/nao-existe")"

passo "3. Sugestão"
confere "sugestão válida" 201 "$(status -X POST "$API/api/sugestoes" -H 'Content-Type: application/json' \
  -d '{"nome":"Teste Automatizado","email":"teste@exemplo.com","telefone":null,"temaSugerido":"Revolta da Vacina","mensagem":null}')"
confere "sugestão inválida" 400 "$(status -X POST "$API/api/sugestoes" -H 'Content-Type: application/json' -d '{"nome":"","email":"x"}')"

passo "4. Autenticação"
confere "senha incorreta" 401 "$(status -X POST "$API/api/auth/login" -H 'Content-Type: application/json' -d "{\"email\":\"$EMAIL\",\"senha\":\"errada123\"}")"
confere "área admin sem token" 401 "$(status "$API/api/admin/artigos")"
TOKEN=$(curl -s -X POST "$API/api/auth/login" -H 'Content-Type: application/json' -d "{\"email\":\"$EMAIL\",\"senha\":\"$SENHA\"}" \
  | sed -n 's/.*"token":"\([^"]*\)".*/\1/p')
[ -n "$TOKEN" ] && r=sim || r=nao
confere "login correto devolve JWT" sim "$r"
AUTH="Authorization: Bearer $TOKEN"
confere "área admin com token" 200 "$(status -H "$AUTH" "$API/api/admin/artigos")"

passo "5. Publicação manual"
NOVO=$(curl -s -X POST "$API/api/admin/artigos" -H "$AUTH" -H 'Content-Type: application/json' -d '{
  "titulo":"Artigo do teste automatizado","resumo":"Resumo de teste.","categoria":"Testes","imagemCapa":null,"autor":null,
  "slug":null,"destaque":false,"arquivoPdfOrigem":null,
  "capitulos":[{"titulo":"Primeiro","conteudoHtml":"<p>Texto <strong>seguro</strong></p><script>alert(1)</script>"}]}')
ID=$(echo "$NOVO" | sed -n 's/^{"id":\([0-9]*\).*/\1/p')
SLUG=$(echo "$NOVO" | sed -n 's/.*"slug":"\([^"]*\)".*/\1/p')
[ -n "$ID" ] && r=sim || r=nao
confere "rascunho criado" sim "$r"
case "$NOVO" in *"<script"*) r=nao;; *) r=sim;; esac
confere "HTML sanitizado (sem <script>)" sim "$r"
confere "rascunho fora da área pública" 404 "$(status "$API/api/artigos/$SLUG")"
confere "publicar" 200 "$(status -X POST -H "$AUTH" "$API/api/admin/artigos/$ID/publicar")"
confere "agora aparece publicamente" 200 "$(status "$API/api/artigos/$SLUG")"
confere "despublicar" 200 "$(status -X POST -H "$AUTH" "$API/api/admin/artigos/$ID/despublicar")"
confere "saiu da área pública" 404 "$(status "$API/api/artigos/$SLUG")"

passo "6. Uploads"
printf 'nao sou imagem' > /tmp/kleio-falso.png
confere "imagem falsa recusada" 422 "$(status -X POST -H "$AUTH" -F "arquivo=@/tmp/kleio-falso.png;type=image/png" "$API/api/admin/imagens")"
printf '%%PDF-1.4 corrompido' > /tmp/kleio-falso.pdf
confere "PDF corrompido recusado" 422 "$(status -X POST -H "$AUTH" -F "arquivo=@/tmp/kleio-falso.pdf;type=application/pdf" "$API/api/admin/artigos/importar-pdf")"

passo "Limpeza"
confere "excluir artigo de teste" 204 "$(status -X DELETE -H "$AUTH" "$API/api/admin/artigos/$ID")"

printf '\n%d verificações aprovadas, %d falharam.\n' "$ok" "$falhas"
[ "$falhas" -eq 0 ]
