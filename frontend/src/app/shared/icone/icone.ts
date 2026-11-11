import { Component, computed, input } from '@angular/core';

/**
 * Ícones em traço (SVG inline) usados em todo o site.
 * Uso: <app-icone nome="lixeira" [tamanho]="20" />
 * São decorativos (aria-hidden): o botão que os contém deve ter texto ou aria-label.
 */
const CAMINHOS: Record<string, string> = {
  menu: 'M2 6h20M2 12h20M2 18h20',
  usuario: 'M12 12a4 4 0 1 0 0-8 4 4 0 0 0 0 8zM4 21c1.5-4 4.5-6 8-6s6.5 2 8 6',
  voltar: 'M22 12H3M9 6l-6 6 6 6',
  imagem: 'M4 4h16a1 1 0 0 1 1 1v14a1 1 0 0 1-1 1H4a1 1 0 0 1-1-1V5a1 1 0 0 1 1-1zM9 11a2 2 0 1 0 0-4 2 2 0 0 0 0 4zM3 17l5-5 4 4 3-3 6 6',
  busca: 'M11 18a7 7 0 1 0 0-14 7 7 0 0 0 0 14zM20 20l-4-4',
  check: 'M5 12l5 5 9-10',
  lixeira: 'M4 7h16M10 11v6M14 11v6M6 7l1 13h10l1-13M9 7V4h6v3',
  editar: 'M12 4H5a1 1 0 0 0-1 1v14a1 1 0 0 0 1 1h14a1 1 0 0 0 1-1v-7M17.5 3.5l3 3L12 15H9v-3z',
  olho: 'M2 12s3.5-7 10-7 10 7 10 7-3.5 7-10 7S2 12 2 12zM12 15a3 3 0 1 0 0-6 3 3 0 0 0 0 6z',
  enviar: 'M22 2L11 13M22 2l-7 20-4-9-9-4z',
  pdf: 'M14 2H6a2 2 0 0 0-2 2v16a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V8zM14 2v6h6M12 18v-6M9 15l3-3 3 3',
  lapis: 'M16.5 3.5l4 4L8 20H4v-4zM3 22h3M9 22h1M13 22h1',
  alerta: 'M12 21a9 9 0 1 0 0-18 9 9 0 0 0 0 18zM12 7v6M12 16.5v.5',
  aviso: 'M12 3l10 18H2zM12 10v5M12 18v.5',
  tesoura: 'M6 9a3 3 0 1 0 0-6 3 3 0 0 0 0 6zM6 21a3 3 0 1 0 0-6 3 3 0 0 0 0 6zM20 4L8.1 15.9M14.5 14.5L20 20M8.1 8.1L12 12',
  lista: 'M9 6h12M9 12h12M9 18h12M4 6h.01M4 12h.01M4 18h.01',
  'lista-numerada': 'M10 6h11M10 12h11M10 18h11M4 4v4M3 12h2l-2 3h2M3 17h2v4H3',
  citacao: 'M7 7H4v6h3l-1 4M17 7h-3v6h3l-1 4',
  link: 'M10 14a4 4 0 0 0 6 0l3-3a4 4 0 0 0-6-6l-1 1M14 10a4 4 0 0 0-6 0l-3 3a4 4 0 0 0 6 6l1-1',
  separador: 'M3 12h18',
  fechar: 'M6 6l12 12M18 6L6 18',
  desfazer: 'M9 14L4 9l5-5M4 9h10a6 6 0 0 1 0 12h-3',
  refazer: 'M15 14l5-5-5-5M20 9H10a6 6 0 0 0 0 12h3',
  estrela: 'M12 3l2.8 5.7 6.2.9-4.5 4.4 1 6.2L12 17.3 6.5 20.2l1-6.2L3 9.6l6.2-.9z',
};

@Component({
  selector: 'app-icone',
  template: `
    <svg [attr.width]="tamanho()" [attr.height]="tamanho()" viewBox="0 0 24 24" fill="none"
         stroke="currentColor" [attr.stroke-width]="espessura()" stroke-linecap="round" stroke-linejoin="round"
         aria-hidden="true" focusable="false">
      <path [attr.d]="caminho()" [attr.fill]="preenchido() ? 'currentColor' : 'none'" />
    </svg>
  `,
  styles: `:host { display: inline-flex; line-height: 0; flex-shrink: 0; }`,
})
export class Icone {
  readonly nome = input.required<string>();
  readonly tamanho = input(22);
  readonly espessura = input(1.7);
  readonly preenchido = input(false);
  readonly caminho = computed(() => CAMINHOS[this.nome()] ?? '');
}
