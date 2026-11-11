import { Routes } from '@angular/router';

/**
 * Rotas públicas são carregadas sob demanda (loadComponent).
 * Toda a área administrativa fica em admin.routes.ts e só é baixada pelo
 * navegador quando alguém acessa /admin (lazy loading com loadChildren).
 * Assim o visitante nunca carrega o editor de texto, o upload de PDF etc.
 */
export const routes: Routes = [
  {
    path: '',
    title: 'Kleio História',
    loadComponent: () => import('./public/home/home').then(m => m.Home),
  },
  {
    path: 'artigos/:slug',
    loadComponent: () => import('./public/leitura-artigo/leitura-artigo').then(m => m.LeituraArtigo),
  },
  {
    path: 'admin',
    loadChildren: () => import('./admin/admin.routes').then(m => m.ADMIN_ROUTES),
  },
  { path: '**', redirectTo: '' },
];
