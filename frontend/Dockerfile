# ===================================================================
#  Kleio História — Frontend (Angular 21)
#  1) Node compila o Angular para arquivos estáticos (HTML/CSS/JS);
#  2) Nginx serve esses arquivos e encaminha /api e /uploads para a API.
# ===================================================================

# ---------- Etapa 1: build ----------
FROM node:22-alpine AS build
WORKDIR /app

# Instala dependências primeiro (cache do Docker): só repete se o package*.json mudar.
COPY package.json package-lock.json ./
RUN npm ci --no-audit --no-fund

COPY . .
RUN npx ng build --configuration production

# ---------- Etapa 2: servidor web ----------
FROM nginx:1.27-alpine AS final

COPY nginx.conf /etc/nginx/conf.d/default.conf
COPY --from=build /app/dist/kleio-historia/browser /usr/share/nginx/html

EXPOSE 80
HEALTHCHECK --interval=15s --timeout=3s --retries=5 CMD wget -q -O /dev/null http://127.0.0.1/ || exit 1
