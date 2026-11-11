import { ApplicationConfig, LOCALE_ID, provideBrowserGlobalErrorListeners } from '@angular/core';
import { provideRouter, withComponentInputBinding, withInMemoryScrolling } from '@angular/router';
import { provideHttpClient, withFetch, withInterceptors } from '@angular/common/http';
import { registerLocaleData } from '@angular/common';
import localePt from '@angular/common/locales/pt';
import { routes } from './app.routes';
import { authInterceptor } from './core/interceptors/auth.interceptor';

// Datas e números no formato brasileiro (ex.: "10 de novembro de 2026").
registerLocaleData(localePt);

export const appConfig: ApplicationConfig = {
  providers: [
    provideBrowserGlobalErrorListeners(),
    { provide: LOCALE_ID, useValue: 'pt-BR' },
    provideRouter(
      routes,
      // anchorScrolling permite links como routerLink="/" fragment="sobre"
      withInMemoryScrolling({ anchorScrolling: 'enabled', scrollPositionRestoration: 'enabled' }),
      // Parâmetros da rota (ex.: :slug) chegam direto como input() no componente
      withComponentInputBinding(),
    ),
    // HttpClient com o interceptor que anexa o JWT às rotas administrativas
    provideHttpClient(withFetch(), withInterceptors([authInterceptor])),
  ],
};
