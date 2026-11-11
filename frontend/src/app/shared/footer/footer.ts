import { Component } from '@angular/core';

@Component({
  selector: 'app-footer',
  template: `<footer>© Kleio História - Todos os direitos reservados</footer>`,
  styles: `
    footer {
      background: var(--marrom);
      color: var(--claro);
      text-align: center;
      padding: 1.3rem 1rem;
      font-size: 1.05rem;
    }
  `,
})
export class Footer {}
