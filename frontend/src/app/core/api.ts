/**
 * Endereço base da API.
 *
 * É relativo de propósito: no Docker, o Nginx que serve o Angular encaminha
 * /api e /uploads para o container da API; no `ng serve`, o proxy.conf.json
 * faz o mesmo para http://localhost:5080. Navegador e API ficam na mesma
 * origem, sem necessidade de CORS e sem URLs fixas no código.
 */
export const API_URL = '/api';
