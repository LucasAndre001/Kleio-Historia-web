import { inject } from '@angular/core';
import { CanActivateFn, CanDeactivateFn, Router } from '@angular/router';
import { AuthService } from '../services/auth.service';

/**
 * Impede visitantes sem login de abrirem a área administrativa.
 * Devolver uma UrlTree faz o router redirecionar sozinho, guardando o
 * endereço pedido para voltar a ele após o login.
 */
export const authGuard: CanActivateFn = (_rota, estado) => {
  const auth = inject(AuthService);
  const router = inject(Router);
  return auth.estaAutenticado()
    ? true
    : router.createUrlTree(['/admin/login'], { queryParams: { retorno: estado.url } });
};

/** Se já estiver logado, a tela de login leva direto ao painel. */
export const loginGuard: CanActivateFn = () => {
  const auth = inject(AuthService);
  const router = inject(Router);
  return auth.estaAutenticado() ? router.createUrlTree(['/admin/painel']) : true;
};

/** Componentes com formulário que podem ter alterações não salvas. */
export interface ComAlteracoesPendentes {
  temAlteracoesPendentes(): boolean;
}

/** Pergunta antes de sair do editor com alterações não salvas. */
export const alteracoesPendentesGuard: CanDeactivateFn<ComAlteracoesPendentes> = componente =>
  !componente.temAlteracoesPendentes() ||
  confirm('Existem alterações não salvas neste artigo. Deseja sair mesmo assim?');
