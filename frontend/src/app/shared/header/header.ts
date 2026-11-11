import { Component, input, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { Icone } from '../icone/icone';

export type SecaoAtiva = 'home' | 'artigos' | 'sugestao' | 'sobre';

const CHAVE_MENU = 'kleio.menuAberto';

/**
 * Cabeçalho público: botão de menu + marca + ação à direita
 * ("Área do professor" na Home, seta de voltar na leitura).
 * O botão de menu abre e fecha a barra de navegação (Home, Artigos, Sugestão, Sobre).
 * A escolha fica salva no navegador, então vale para todas as páginas.
 */
@Component({
  selector: 'app-header',
  imports: [RouterLink, Icone],
  templateUrl: './header.html',
  styleUrl: './header.scss',
})
export class Header {
  /** 'inicio' mostra o link da área do professor; 'voltar' mostra a seta para a Home. */
  readonly modo = input<'inicio' | 'voltar'>('inicio');
  readonly ativo = input<SecaoAtiva>('home');

  readonly menuAberto = signal(lerPreferencia());

  alternarMenu(): void {
    this.menuAberto.update(v => !v);
    try {
      localStorage.setItem(CHAVE_MENU, String(this.menuAberto()));
    } catch {
      /* sem storage: só não lembra a escolha */
    }
  }
}

/** Aberto por padrão; fechado só se o visitante fechou antes. */
function lerPreferencia(): boolean {
  try {
    return localStorage.getItem(CHAVE_MENU) !== 'false';
  } catch {
    return true;
  }
}
