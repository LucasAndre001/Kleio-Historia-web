import { Component, ElementRef, HostListener, afterNextRender, computed, inject, signal, viewChild } from '@angular/core';
import { DatePipe } from '@angular/common';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { Observable, forkJoin } from 'rxjs';
import { AdminArtigoService } from '../../core/services/admin-artigo.service';
import { SugestaoService } from '../../core/services/sugestao.service';
import { AuthService } from '../../core/services/auth.service';
import { mensagemDeErro } from '../../core/services/erro-api';
import { ComAlteracoesPendentes } from '../../core/guards/auth.guard';
import { ArtigoAdminResumo, StatusArtigo } from '../../core/models/artigo.model';
import { Sugestao } from '../../core/models/sugestao.model';
import { PdfImportacao } from '../../core/models/conteudo.model';
import { EditorArtigo } from '../editor-artigo/editor-artigo';
import { ImportarPdf } from '../importar-pdf/importar-pdf';
import { Icone } from '../../shared/icone/icone';

type FiltroStatus = 'todos' | StatusArtigo;
type Confirmacao = { tipo: 'artigo' | 'sugestao'; id: number; titulo: string };

/**
 * Gestão de conteúdo em uma única página (como no protótipo):
 * ações principais, editor de artigo, lista de artigos e sugestões recebidas.
 */
@Component({
  selector: 'app-painel',
  imports: [DatePipe, RouterLink, EditorArtigo, ImportarPdf, Icone],
  templateUrl: './painel.html',
  styleUrl: './painel.scss',
})
export class Painel implements ComAlteracoesPendentes {
  private readonly artigoService = inject(AdminArtigoService);
  private readonly sugestaoService = inject(SugestaoService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  readonly auth = inject(AuthService);

  private readonly editor = viewChild.required(EditorArtigo);
  private readonly secaoEditor = viewChild.required<ElementRef<HTMLElement>>('secaoEditor');

  readonly artigos = signal<ArtigoAdminResumo[]>([]);
  readonly sugestoes = signal<Sugestao[]>([]);
  readonly carregando = signal(true);
  readonly erro = signal('');
  readonly aviso = signal<{ tipo: 'ok' | 'erro'; texto: string } | null>(null);
  readonly filtro = signal<FiltroStatus>('todos');
  readonly ocupados = signal<ReadonlySet<number>>(new Set());
  readonly confirmacao = signal<Confirmacao | null>(null);
  readonly importando = signal(false);

  readonly totalPublicados = computed(() => this.artigos().filter(a => a.status === 'publicado').length);
  readonly totalRascunhos = computed(() => this.artigos().filter(a => a.status === 'rascunho').length);
  readonly categorias = computed(() => [...new Set(this.artigos().map(a => a.categoria))].sort());
  readonly artigosFiltrados = computed(() =>
    this.filtro() === 'todos' ? this.artigos() : this.artigos().filter(a => a.status === this.filtro()));

  private temporizadorAviso?: ReturnType<typeof setTimeout>;

  constructor() {
    this.carregar();
    // Vindo da pré-visualização ("Voltar ao editor"): abre o artigo no editor.
    afterNextRender(() => {
      const id = Number(this.route.snapshot.queryParamMap.get('editar'));
      if (id) {
        this.editor().carregar(id);
        this.router.navigate([], { queryParams: {}, replaceUrl: true });
        this.rolarParaEditor();
      }
    });
  }

  carregar(): void {
    this.erro.set('');
    forkJoin({ artigos: this.artigoService.listar(), sugestoes: this.sugestaoService.listar() }).subscribe({
      next: ({ artigos, sugestoes }) => {
        this.artigos.set(artigos);
        this.sugestoes.set(sugestoes);
        this.carregando.set(false);
      },
      error: e => {
        this.erro.set(mensagemDeErro(e, 'Não foi possível carregar o painel.'));
        this.carregando.set(false);
      },
    });
  }

  // ---------- Editor ----------

  novaPublicacao(): void {
    if (!this.podeTrocarConteudoDoEditor()) return;
    this.editor().novo();
    this.rolarParaEditor();
  }

  editar(artigo: ArtigoAdminResumo): void {
    if (!this.podeTrocarConteudoDoEditor()) return;
    this.editor().carregar(artigo.id);
    this.rolarParaEditor();
  }

  revisarImportacao(resultado: PdfImportacao): void {
    if (!this.podeTrocarConteudoDoEditor()) return;
    this.importando.set(false);
    this.editor().carregarImportacao(resultado);
    this.rolarParaEditor();
  }

  private podeTrocarConteudoDoEditor(): boolean {
    return !this.editor().temAlteracoesPendentes()
      || confirm('O artigo aberto no editor tem alterações não salvas. Deseja descartá-las?');
  }

  private rolarParaEditor(): void {
    this.secaoEditor().nativeElement.scrollIntoView({ behavior: 'smooth', block: 'start' });
  }

  temAlteracoesPendentes(): boolean {
    return this.editor().temAlteracoesPendentes();
  }

  @HostListener('window:beforeunload', ['$event'])
  aoFecharAba(evento: BeforeUnloadEvent): void {
    if (this.temAlteracoesPendentes()) evento.preventDefault();
  }

  // ---------- Ações da lista ----------

  publicar(a: ArtigoAdminResumo): void {
    this.executar(a.id, this.artigoService.publicar(a.id), `"${a.titulo}" foi publicado.`);
  }

  despublicar(a: ArtigoAdminResumo): void {
    this.executar(a.id, this.artigoService.despublicar(a.id), `"${a.titulo}" voltou para rascunho.`);
  }

  alternarDestaque(a: ArtigoAdminResumo): void {
    this.executar(a.id, this.artigoService.definirDestaque(a.id, !a.destaque),
      a.destaque ? 'Artigo removido dos destaques.' : 'Artigo adicionado aos destaques da página inicial.');
  }

  pedirExclusao(tipo: Confirmacao['tipo'], id: number, titulo: string): void {
    this.confirmacao.set({ tipo, id, titulo });
  }

  confirmarExclusao(): void {
    const alvo = this.confirmacao();
    if (!alvo) return;
    this.confirmacao.set(null);
    if (alvo.tipo === 'artigo') {
      if (this.editor().id() === alvo.id) this.editor().novo();
      this.executar(alvo.id, this.artigoService.excluir(alvo.id), `"${alvo.titulo}" foi excluído.`);
    } else {
      this.executar(-alvo.id, this.sugestaoService.excluir(alvo.id), 'Sugestão excluída.');
    }
  }

  estaOcupado(id: number): boolean {
    return this.ocupados().has(id);
  }

  sair(): void {
    if (!this.podeTrocarConteudoDoEditor()) return;
    this.editor().novo();
    this.auth.logout();
    this.router.navigate(['/admin/login']);
  }

  private executar(id: number, acao: Observable<unknown>, sucesso: string): void {
    this.ocupados.update(s => new Set(s).add(id));
    acao.subscribe({
      next: () => {
        this.liberar(id);
        this.mostrarAviso('ok', sucesso);
        this.carregar();
      },
      error: e => {
        this.liberar(id);
        this.mostrarAviso('erro', mensagemDeErro(e));
      },
    });
  }

  private liberar(id: number): void {
    this.ocupados.update(s => {
      const novo = new Set(s);
      novo.delete(id);
      return novo;
    });
  }

  private mostrarAviso(tipo: 'ok' | 'erro', texto: string): void {
    clearTimeout(this.temporizadorAviso);
    this.aviso.set({ tipo, texto });
    this.temporizadorAviso = setTimeout(() => this.aviso.set(null), 5000);
  }
}
