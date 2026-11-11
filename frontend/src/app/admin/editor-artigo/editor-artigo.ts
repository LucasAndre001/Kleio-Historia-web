import { Component, computed, inject, input, output, signal } from '@angular/core';
import { Router } from '@angular/router';
import { FormArray, FormBuilder, FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { Observable } from 'rxjs';
import { AdminArtigoService } from '../../core/services/admin-artigo.service';
import { mensagemDeErro } from '../../core/services/erro-api';
import { ArtigoAdminDetalhe, ArtigoSalvar, StatusArtigo } from '../../core/models/artigo.model';
import { PdfImportacao } from '../../core/models/conteudo.model';
import { DivisaoCapitulo, EditorRico } from '../componentes/editor-rico/editor-rico';
import { Icone } from '../../shared/icone/icone';
import { CampoImagem } from '../componentes/campo-imagem/campo-imagem';

type CapituloForm = FormGroup<{ titulo: FormControl<string>; conteudoHtml: FormControl<string> }>;
type AcaoSalvar = 'salvar' | 'publicar' | 'previa';

/**
 * Editor de artigo embutido no painel (protótipo "Editor de artigo").
 * Serve tanto para a publicação manual quanto para revisar um PDF importado:
 * os dois caminhos gravam o mesmo ArtigoSalvar (artigo + capítulos).
 */
@Component({
  selector: 'app-editor-artigo',
  imports: [ReactiveFormsModule, EditorRico, Icone, CampoImagem],
  templateUrl: './editor-artigo.html',
  styleUrl: './editor-artigo.scss',
})
export class EditorArtigo {
  private readonly fb = inject(FormBuilder);
  private readonly router = inject(Router);
  private readonly artigoService = inject(AdminArtigoService);

  /** Categorias já usadas, sugeridas no campo Categoria. */
  readonly categorias = input<string[]>([]);
  /** Avisa o painel que a lista de artigos mudou (salvou, publicou, excluiu). */
  readonly alterado = output<void>();

  // ---------- Estado ----------
  readonly id = signal<number | null>(null);
  readonly status = signal<StatusArtigo>('rascunho');
  readonly slug = signal('');
  readonly salvoEm = signal<string | null>(null);
  readonly destaque = signal(false);
  readonly arquivoPdfOrigem = signal<string | null>(null);
  readonly importacao = signal<PdfImportacao | null>(null);
  readonly ativo = signal(0);

  readonly carregando = signal(false);
  readonly salvando = signal(false);
  readonly confirmarExclusao = signal(false);
  readonly mensagem = signal<{ tipo: 'ok' | 'erro'; texto: string } | null>(null);

  readonly publicado = computed(() => this.status() === 'publicado');
  readonly rotuloEstado = computed(() => {
    if (!this.id()) return this.importacao() ? 'Novo artigo (importado de PDF, ainda não salvo)' : 'Novo artigo';
    const quando = this.salvoEm() ? ` · salvo às ${new Date(this.salvoEm()!).toLocaleTimeString('pt-BR', { hour: '2-digit', minute: '2-digit' })}` : '';
    return (this.publicado() ? 'Publicado' : 'Rascunho') + quando;
  });

  readonly form = this.fb.nonNullable.group({
    titulo: ['', [Validators.required, Validators.minLength(3), Validators.maxLength(200)]],
    resumo: ['', [Validators.maxLength(600)]],
    categoria: ['', [Validators.required, Validators.minLength(2), Validators.maxLength(80)]],
    autor: ['', [Validators.maxLength(150)]],
    imagemCapa: ['', [Validators.maxLength(500)]],
    imagemBanner: ['', [Validators.maxLength(500)]],
    capitulos: this.fb.array<CapituloForm>([]),
  });

  get capitulos(): FormArray<CapituloForm> {
    return this.form.controls.capitulos;
  }

  private temporizadorMensagem?: ReturnType<typeof setTimeout>;

  constructor() {
    this.novo();
  }

  // =====================================================================
  //  Abrir conteúdo no editor (chamado pelo painel)
  // =====================================================================

  /** Limpa o editor para uma nova publicação manual. */
  novo(): void {
    this.id.set(null);
    this.status.set('rascunho');
    this.slug.set('');
    this.salvoEm.set(null);
    this.destaque.set(false);
    this.arquivoPdfOrigem.set(null);
    this.importacao.set(null);
    this.form.reset({ titulo: '', resumo: '', categoria: '', autor: '', imagemCapa: '', imagemBanner: '' });
    this.capitulos.clear();
    this.capitulos.push(this.novoCapitulo('Capítulo 1'));
    this.ativo.set(0);
    this.mensagem.set(null);
    this.form.markAsPristine();
  }

  carregar(id: number): void {
    this.carregando.set(true);
    this.artigoService.obter(id).subscribe({
      next: artigo => {
        this.importacao.set(null);
        this.aplicarArtigo(artigo);
        this.ativo.set(0);
        this.carregando.set(false);
      },
      error: e => {
        this.carregando.set(false);
        this.mostrarMensagem('erro', mensagemDeErro(e, 'Não foi possível carregar o artigo.'));
      },
    });
  }

  /** Preenche o editor com o resultado da importação de PDF, para revisão. */
  carregarImportacao(resultado: PdfImportacao): void {
    this.novo();
    this.importacao.set(resultado);
    this.arquivoPdfOrigem.set(resultado.arquivoId);
    this.form.patchValue({
      titulo: (resultado.tituloSugerido ?? resultado.nomeArquivoOriginal.replace(/\.pdf$/i, '')).slice(0, 200),
      resumo: (resultado.resumoSugerido ?? '').slice(0, 600),
    });
    if (resultado.capitulos.length) {
      this.capitulos.clear();
      for (const c of resultado.capitulos) this.capitulos.push(this.novoCapitulo(c.titulo, c.conteudoHtml));
    }
    this.form.markAsDirty();
  }

  temAlteracoesPendentes(): boolean {
    return this.form.dirty && !this.salvando();
  }

  private aplicarArtigo(artigo: ArtigoAdminDetalhe): void {
    this.id.set(artigo.id);
    this.status.set(artigo.status);
    this.slug.set(artigo.slug);
    this.salvoEm.set(artigo.dataAtualizacao);
    this.destaque.set(artigo.destaque);
    this.arquivoPdfOrigem.set(artigo.arquivoPdfOrigem);
    this.form.patchValue({
      titulo: artigo.titulo,
      resumo: artigo.resumo,
      categoria: artigo.categoria,
      autor: artigo.autor ?? '',
      imagemCapa: artigo.imagemCapa ?? '',
      imagemBanner: artigo.imagemBanner ?? '',
    });
    const ativoAnterior = this.ativo();
    this.capitulos.clear();
    for (const c of artigo.capitulos) this.capitulos.push(this.novoCapitulo(c.titulo, c.conteudoHtml));
    if (this.capitulos.length === 0) this.capitulos.push(this.novoCapitulo('Capítulo 1'));
    this.ativo.set(Math.min(ativoAnterior, this.capitulos.length - 1));
    this.form.markAsPristine();
  }

  // =====================================================================
  //  Capítulos (abas)
  // =====================================================================

  private novoCapitulo(titulo = '', conteudoHtml = ''): CapituloForm {
    return this.fb.nonNullable.group({
      titulo: [titulo, [Validators.required, Validators.maxLength(200)]],
      conteudoHtml: [conteudoHtml],
    });
  }

  selecionar(i: number): void {
    this.ativo.set(i);
  }

  adicionarCapitulo(posicao = this.capitulos.length, titulo = '', html = ''): void {
    this.capitulos.insert(posicao, this.novoCapitulo(titulo || `Capítulo ${posicao + 1}`, html));
    this.ativo.set(posicao);
    this.form.markAsDirty();
  }

  moverCapitulo(deslocamento: -1 | 1): void {
    const i = this.ativo();
    const destino = i + deslocamento;
    if (destino < 0 || destino >= this.capitulos.length) return;
    const capitulo = this.capitulos.at(i);
    this.capitulos.removeAt(i, { emitEvent: false });
    this.capitulos.insert(destino, capitulo);
    this.ativo.set(destino);
    this.form.markAsDirty();
  }

  removerCapitulo(): void {
    const i = this.ativo();
    const capitulo = this.capitulos.at(i);
    const temConteudo = capitulo.controls.conteudoHtml.value.replace(/<[^>]+>/g, '').trim().length > 0;
    if (temConteudo && !confirm(`Excluir o capítulo "${capitulo.controls.titulo.value}" e todo o seu conteúdo?`)) return;
    this.capitulos.removeAt(i);
    if (this.capitulos.length === 0) this.capitulos.push(this.novoCapitulo('Capítulo 1'));
    this.ativo.set(Math.max(0, i - 1));
    this.form.markAsDirty();
  }

  /** O editor cortou o texto a partir do cursor: vira um novo capítulo logo depois. */
  dividirCapitulo(divisao: DivisaoCapitulo): void {
    const posicao = this.ativo() + 1;
    this.adicionarCapitulo(posicao, divisao.titulo, divisao.html);
    this.mostrarMensagem('ok', `Novo capítulo ${posicao + 1} criado. Confira o título dele.`);
  }

  // =====================================================================
  //  Salvar / publicar / excluir
  // =====================================================================

  salvar(acao: AcaoSalvar = 'salvar'): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      const capInvalido = this.capitulos.controls.findIndex(c => c.invalid);
      if (capInvalido >= 0) this.ativo.set(capInvalido);
      this.mostrarMensagem('erro', 'Preencha o título, a categoria e o título de todos os capítulos.');
      return;
    }
    if (acao === 'publicar') {
      const problema = this.validarParaPublicacao();
      if (problema) {
        this.mostrarMensagem('erro', problema);
        return;
      }
    }

    const idAtual = this.id();
    const requisicao: Observable<ArtigoAdminDetalhe> = idAtual
      ? this.artigoService.atualizar(idAtual, this.montarDados())
      : this.artigoService.criar(this.montarDados());

    this.salvando.set(true);
    requisicao.subscribe({
      next: artigo => {
        this.aplicarArtigo(artigo);
        this.importacao.set(null);
        this.alterado.emit();
        if (acao === 'publicar' && artigo.status !== 'publicado') {
          this.publicar(artigo.id);
          return;
        }
        this.salvando.set(false);
        if (acao === 'previa') {
          this.router.navigate(['/admin/artigos', artigo.id, 'previa']);
          return;
        }
        this.mostrarMensagem('ok', this.publicado() ? 'Alterações publicadas.' : 'Rascunho salvo.');
      },
      error: e => {
        this.salvando.set(false);
        this.mostrarMensagem('erro', mensagemDeErro(e, 'Não foi possível salvar o artigo.'));
      },
    });
  }

  private publicar(id: number): void {
    this.artigoService.publicar(id).subscribe({
      next: artigo => {
        this.aplicarArtigo(artigo);
        this.salvando.set(false);
        this.alterado.emit();
        this.mostrarMensagem('ok', 'Artigo publicado! Ele já aparece na página inicial.');
      },
      error: e => {
        this.salvando.set(false);
        this.mostrarMensagem('erro', `Rascunho salvo, mas não publicado: ${mensagemDeErro(e)}`);
      },
    });
  }

  despublicar(): void {
    const id = this.id();
    if (!id) return;
    this.salvando.set(true);
    this.artigoService.despublicar(id).subscribe({
      next: artigo => {
        this.status.set(artigo.status);
        this.destaque.set(false);
        this.salvoEm.set(artigo.dataAtualizacao);
        this.salvando.set(false);
        this.alterado.emit();
        this.mostrarMensagem('ok', 'Artigo despublicado: voltou a ser rascunho e saiu do site.');
      },
      error: e => {
        this.salvando.set(false);
        this.mostrarMensagem('erro', mensagemDeErro(e));
      },
    });
  }

  excluir(): void {
    const id = this.id();
    this.confirmarExclusao.set(false);
    if (!id) {
      this.novo();
      return;
    }
    this.salvando.set(true);
    this.artigoService.excluir(id).subscribe({
      next: () => {
        this.salvando.set(false);
        this.novo();
        this.alterado.emit();
        this.mostrarMensagem('ok', 'Artigo excluído.');
      },
      error: e => {
        this.salvando.set(false);
        this.mostrarMensagem('erro', mensagemDeErro(e));
      },
    });
  }

  private validarParaPublicacao(): string | null {
    const v = this.form.getRawValue();
    if (!v.resumo.trim()) return 'Para publicar, escreva um resumo do artigo.';
    const algumConteudo = v.capitulos.some(c => c.conteudoHtml.replace(/<(?!img)[^>]+>/g, '').trim().length > 0);
    return algumConteudo ? null : 'Para publicar, escreva o conteúdo de pelo menos um capítulo.';
  }

  private montarDados(): ArtigoSalvar {
    const v = this.form.getRawValue();
    return {
      titulo: v.titulo.trim(),
      resumo: v.resumo.trim(),
      categoria: v.categoria.trim(),
      autor: v.autor.trim() || null,
      slug: null,
      imagemCapa: v.imagemCapa.trim() || null,
      imagemBanner: v.imagemBanner.trim() || null,
      destaque: this.publicado() && this.destaque(),
      arquivoPdfOrigem: this.arquivoPdfOrigem(),
      capitulos: v.capitulos.map(c => ({ titulo: c.titulo.trim(), conteudoHtml: c.conteudoHtml })),
    };
  }

  invalido(campo: 'titulo' | 'categoria'): boolean {
    const c = this.form.controls[campo];
    return c.invalid && c.touched;
  }

  private mostrarMensagem(tipo: 'ok' | 'erro', texto: string): void {
    clearTimeout(this.temporizadorMensagem);
    this.mensagem.set({ tipo, texto });
    this.temporizadorMensagem = setTimeout(() => this.mensagem.set(null), tipo === 'erro' ? 9000 : 5000);
  }
}
