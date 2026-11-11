import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { catchError, throwError } from 'rxjs';
import { API_URL } from '../api';
import { AuthService } from '../services/auth.service';

/**
 * Interceptor funcional: roda antes de TODA requisição do HttpClient.
 *
 * - Anexa "Authorization: Bearer <token>" somente às rotas /api/admin
 *   (o token não é enviado à toa para endpoints públicos).
 * - Se a API responder 401 (token expirado/inválido), encerra a sessão e
 *   leva o professor de volta ao login.
 */
export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const auth = inject(AuthService);
  const router = inject(Router);
  const ehAdministrativa = req.url.startsWith(`${API_URL}/admin`);
  const token = ehAdministrativa ? auth.token() : null;

  const requisicao = token ? req.clone({ setHeaders: { Authorization: `Bearer ${token}` } }) : req;

  return next(requisicao).pipe(
    catchError((erro: unknown) => {
      if (ehAdministrativa && erro instanceof HttpErrorResponse && erro.status === 401) {
        auth.logout();
        router.navigate(['/admin/login'], { queryParams: { expirado: 1 } });
      }
      return throwError(() => erro);
    }),
  );
};
