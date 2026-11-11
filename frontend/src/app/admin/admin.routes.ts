import { Routes } from '@angular/router';
import { alteracoesPendentesGuard, authGuard, loginGuard } from '../core/guards/auth.guard';

/**
 * Rotas da área administrativa. Este arquivo (e tudo que ele importa) só é
 * baixado quando o navegador acessa /admin — lazy loading via loadChildren.
 */
export const ADMIN_ROUTES: Routes = [
  {
    path: 'login',
    title: 'Entrar · Kleio História',
    canActivate: [loginGuard],
    loadComponent: () => import('./login/login').then(m => m.Login),
  },
  {
    path: 'painel',
    title: 'Painel administrativo · Kleio História',
    canActivate: [authGuard],
    canDeactivate: [alteracoesPendentesGuard],
    loadComponent: () => import('./painel/painel').then(m => m.Painel),
  },
  {
    // Pré-visualização: mostra o artigo exatamente como no site.
    path: 'artigos/:id/previa',
    title: 'Pré-visualização · Kleio História',
    canActivate: [authGuard],
    data: { previa: true },
    loadComponent: () => import('../public/leitura-artigo/leitura-artigo').then(m => m.LeituraArtigo),
  },
  { path: '', pathMatch: 'full', redirectTo: 'painel' },
  { path: '**', redirectTo: 'painel' },
];
