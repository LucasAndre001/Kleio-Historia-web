import { Node } from '@tiptap/core';

export interface AtributosFigura {
  src: string;
  alt: string;
  legenda: string;
}

// Declara os comandos novos para o TypeScript reconhecer editor.commands.inserirFigura(...)
declare module '@tiptap/core' {
  interface Commands<ReturnType> {
    figura: {
      inserirFigura: (atributos: AtributosFigura) => ReturnType;
      atualizarFigura: (atributos: Partial<AtributosFigura>) => ReturnType;
    };
  }
}

/**
 * Nó personalizado do TipTap para imagens com legenda:
 *
 *   <figure><img src="..." alt="..."><figcaption>Legenda</figcaption></figure>
 *
 * É o mesmo HTML que a API reconhece ao preencher a tabela "Imagens"
 * (Url, Legenda, TextoAlternativo) e que a tela de leitura estiliza.
 * Imagens soltas (<img>) — comuns em conteúdo colado — também viram figuras.
 */
export const Figura = Node.create({
  name: 'figura',
  group: 'block',
  atom: true,        // editado como um bloco único (legenda pelo painel de imagem)
  draggable: true,
  selectable: true,

  addAttributes() {
    return {
      src: { default: '' },
      alt: { default: '' },
      legenda: { default: '' },
    };
  },

  parseHTML() {
    return [
      {
        tag: 'figure',
        getAttrs: elemento => {
          const img = elemento.querySelector('img');
          if (!img) return false;
          return {
            src: img.getAttribute('src') ?? '',
            alt: img.getAttribute('alt') ?? '',
            legenda: elemento.querySelector('figcaption')?.textContent?.trim() ?? '',
          };
        },
      },
      {
        tag: 'img[src]',
        getAttrs: elemento => ({
          src: elemento.getAttribute('src') ?? '',
          alt: elemento.getAttribute('alt') ?? '',
          legenda: '',
        }),
      },
    ];
  },

  renderHTML({ node }) {
    const { src, alt, legenda } = node.attrs as AtributosFigura;
    const imagem = ['img', { src, alt }] as const;
    return legenda ? ['figure', {}, imagem, ['figcaption', {}, legenda]] : ['figure', {}, imagem];
  },

  addCommands() {
    return {
      inserirFigura:
        atributos =>
        ({ commands }) =>
          commands.insertContent({ type: this.name, attrs: atributos }),
      atualizarFigura:
        atributos =>
        ({ commands }) =>
          commands.updateAttributes(this.name, atributos),
    };
  },
});
