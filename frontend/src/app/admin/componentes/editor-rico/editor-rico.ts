import {
  Component, ElementRef, OnDestroy, afterNextRender, forwardRef, inject, input, output, signal, viewChild,
} from '@angular/core';
import { ControlValueAccessor, NG_VALUE_ACCESSOR } from '@angular/forms';
import { Editor } from '@tiptap/core';
import StarterKit from '@tiptap/starter-kit';
import { Placeholder } from '@tiptap/extensions';
import { DOMSerializer } from '@tiptap/pm/model';
import { Figura } from './figura.extension';
import { ImagemService } from '../../../core/services/imagem.service';
import { mensagemDeErro } from '../../../core/services/erro-api';
import { Icone } from '../../../shared/icone/icone';

/** Emitido quando o professor divide o capítulo no ponto do cursor. */
export interface DivisaoCapitulo {
  titulo: string;
  html: string;
}

type Painel = 'nenhum' | 'link' | 'imagem';

/**
 * Editor de texto rico baseado no TipTap (ProseMirror).
 *
 * Implementa ControlValueAccessor: funciona dentro de um Reactive Form como
 * um <input> comum (formControlName="conteudoHtml"), lendo e gravando HTML.
 */
@Component({
  selector: 'app-editor-rico',
  imports: [Icone],
  templateUrl: './editor-rico.html',
  styleUrl: './editor-rico.scss',
  providers: [{ provide: NG_VALUE_ACCESSOR, useExisting: forwardRef(() => EditorRico), multi: true }],
})
export class EditorRico implements ControlValueAccessor, OnDestroy {
  private readonly imagemService = inject(ImagemService);

  readonly placeholder = input('Escreva o conteúdo do capítulo…');
  readonly permitirDivisao = input(true);
  readonly dividir = output<DivisaoCapitulo>();

  private readonly area = viewChild.required<ElementRef<HTMLDivElement>>('area');
  private editor?: Editor;
  private valorPendente = '';
  private onChange: (valor: string) => void = () => {};
  private onTouched: () => void = () => {};

  /** Incrementado a cada transação: faz a barra de ferramentas reavaliar os botões ativos. */
  readonly versao = signal(0);
  readonly desabilitado = signal(false);
  readonly painel = signal<Painel>('nenhum');
  readonly enviandoImagem = signal(false);
  readonly erroPainel = signal('');

  // Campos dos painéis de link e imagem
  readonly linkUrl = signal('');
  readonly imagemUrl = signal('');
  readonly imagemLegenda = signal('');
  readonly imagemAlt = signal('');
  readonly editandoFigura = signal(false);

  constructor() {
    // O TipTap precisa de um elemento real do DOM: criamos o editor depois da renderização.
    afterNextRender(() => this.criarEditor());
  }

  private criarEditor(): void {
    this.editor = new Editor({
      element: this.area().nativeElement,
      content: this.valorPendente,
      editable: !this.desabilitado(),
      extensions: [
        StarterKit.configure({
          heading: { levels: [2, 3, 4] },
          codeBlock: false,
          code: false,
          link: { openOnClick: false, autolink: true },
        }),
        Placeholder.configure({ placeholder: this.placeholder() }),
        Figura,
      ],
      onUpdate: ({ editor }) => this.onChange(editor.isEmpty ? '' : editor.getHTML()),
      onTransaction: () => this.versao.update(v => v + 1),
      onBlur: () => this.onTouched(),
    });
  }

  ngOnDestroy(): void {
    this.editor?.destroy();
  }

  // ---------- ControlValueAccessor ----------
  writeValue(valor: string | null): void {
    this.valorPendente = valor ?? '';
    if (this.editor && this.editor.getHTML() !== this.valorPendente) {
      this.editor.commands.setContent(this.valorPendente, { emitUpdate: false });
    }
  }
  registerOnChange(fn: (valor: string) => void): void {
    this.onChange = fn;
  }
  registerOnTouched(fn: () => void): void {
    this.onTouched = fn;
  }
  setDisabledState(desabilitado: boolean): void {
    this.desabilitado.set(desabilitado);
    this.editor?.setEditable(!desabilitado);
  }

  // ---------- Barra de ferramentas ----------
  ativo(nome: string, atributos?: Record<string, unknown>): boolean {
    this.versao(); // dependência do signal: reavalia a cada transação
    return this.editor?.isActive(nome, atributos) ?? false;
  }

  podeDesfazer(): boolean {
    this.versao();
    return this.editor?.can().undo() ?? false;
  }

  podeRefazer(): boolean {
    this.versao();
    return this.editor?.can().redo() ?? false;
  }

  executar(comando: string): void {
    const c = this.editor?.chain().focus();
    if (!c) return;
    switch (comando) {
      case 'negrito': c.toggleBold().run(); break;
      case 'italico': c.toggleItalic().run(); break;
      case 'sublinhado': c.toggleUnderline().run(); break;
      case 'titulo': c.toggleHeading({ level: 3 }).run(); break;
      case 'subtitulo': c.toggleHeading({ level: 4 }).run(); break;
      case 'lista': c.toggleBulletList().run(); break;
      case 'lista-numerada': c.toggleOrderedList().run(); break;
      case 'citacao': c.toggleBlockquote().run(); break;
      case 'separador': c.setHorizontalRule().run(); break;
      case 'desfazer': c.undo().run(); break;
      case 'refazer': c.redo().run(); break;
    }
  }

  // ---------- Link ----------
  abrirPainelLink(): void {
    this.erroPainel.set('');
    this.linkUrl.set((this.editor?.getAttributes('link')['href'] as string | undefined) ?? 'https://');
    this.painel.set(this.painel() === 'link' ? 'nenhum' : 'link');
  }

  aplicarLink(): void {
    const url = this.linkUrl().trim();
    if (!/^(https?:\/\/|mailto:)/i.test(url)) {
      this.erroPainel.set('O link deve começar com http://, https:// ou mailto:');
      return;
    }
    const c = this.editor?.chain().focus().extendMarkRange('link');
    if (this.editor?.state.selection.empty && !this.ativo('link')) {
      // Sem texto selecionado: insere o próprio endereço como texto do link.
      c?.insertContent({ type: 'text', text: url, marks: [{ type: 'link', attrs: { href: url } }] }).run();
    } else {
      c?.setLink({ href: url }).run();
    }
    this.painel.set('nenhum');
  }

  removerLink(): void {
    this.editor?.chain().focus().extendMarkRange('link').unsetLink().run();
    this.painel.set('nenhum');
  }

  // ---------- Imagem ----------
  abrirPainelImagem(): void {
    this.erroPainel.set('');
    const figuraSelecionada = this.ativo('figura');
    this.editandoFigura.set(figuraSelecionada);
    const atual = figuraSelecionada ? this.editor!.getAttributes('figura') : {};
    this.imagemUrl.set((atual['src'] as string) ?? '');
    this.imagemLegenda.set((atual['legenda'] as string) ?? '');
    this.imagemAlt.set((atual['alt'] as string) ?? '');
    this.painel.set(this.painel() === 'imagem' ? 'nenhum' : 'imagem');
  }

  enviarArquivo(evento: Event): void {
    const campo = evento.target as HTMLInputElement;
    const arquivo = campo.files?.[0];
    campo.value = '';
    if (!arquivo) return;

    const erro = this.imagemService.validar(arquivo);
    if (erro) {
      this.erroPainel.set(erro);
      return;
    }
    this.enviandoImagem.set(true);
    this.erroPainel.set('');
    this.imagemService.enviar(arquivo).subscribe({
      next: r => {
        this.imagemUrl.set(r.url);
        this.enviandoImagem.set(false);
      },
      error: e => {
        this.erroPainel.set(mensagemDeErro(e, 'Falha ao enviar a imagem.'));
        this.enviandoImagem.set(false);
      },
    });
  }

  aplicarImagem(): void {
    const src = this.imagemUrl().trim();
    if (!src.startsWith('/uploads/') && !/^https?:\/\//i.test(src)) {
      this.erroPainel.set('Envie um arquivo ou informe um endereço http(s).');
      return;
    }
    const atributos = { src, legenda: this.imagemLegenda().trim(), alt: this.imagemAlt().trim() };
    const c = this.editor?.chain().focus();
    if (this.editandoFigura()) c?.atualizarFigura(atributos).run();
    else c?.inserirFigura(atributos).run();
    this.painel.set('nenhum');
  }

  fecharPainel(): void {
    this.painel.set('nenhum');
  }

  // ---------- Divisão de capítulo ----------
  /**
   * Corta o conteúdo a partir do bloco onde está o cursor e o envia para um novo
   * capítulo. Se esse bloco for um título (h2–h4), ele vira o título do novo capítulo.
   * Muito útil para organizar textos importados de PDF.
   */
  dividirAqui(): void {
    const editor = this.editor;
    if (!editor) return;
    const { state } = editor;
    const $cursor = state.selection.$from;
    const inicio = $cursor.depth > 0 ? $cursor.before(1) : $cursor.pos;
    const fim = state.doc.content.size;

    if (inicio <= 0) {
      this.erroPainel.set('Posicione o cursor no parágrafo que deve iniciar o novo capítulo (não no primeiro).');
      this.painel.set('nenhum');
      setTimeout(() => this.erroPainel.set(''), 4000);
      return;
    }

    // Serializa o trecho cortado como HTML usando o mesmo esquema do editor.
    const trecho = state.doc.slice(inicio, fim).content;
    const container = document.createElement('div');
    container.appendChild(DOMSerializer.fromSchema(state.schema).serializeFragment(trecho));

    let titulo = '';
    const primeiro = container.firstElementChild;
    if (primeiro && /^H[2-4]$/.test(primeiro.tagName)) {
      titulo = primeiro.textContent?.trim() ?? '';
      primeiro.remove();
    }

    editor.chain().focus().deleteRange({ from: inicio, to: fim }).run();
    this.dividir.emit({ titulo, html: container.innerHTML });
  }
}
