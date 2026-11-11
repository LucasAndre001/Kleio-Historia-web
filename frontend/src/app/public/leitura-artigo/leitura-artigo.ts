import {
  Component, ElementRef, HostListener, Injector, OnDestroy, afterNextRender, computed, inject, signal, viewChild,
  viewChildren,
} from '@angular/core';
import { DatePipe } from '@angular/common';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { Title } from '@angular/platform-browser';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { HttpErrorResponse } from '@angular/common/http';
import { Observable, catchError, map, of, switchMap, tap } from 'rxjs';
import { Header } from '../../shared/header/header';
import { Footer } from '../../shared/footer/footer';
import { Icone } from '../../shared/icone/icone';
import { ArtigoService } from '../../core/services/artigo.service';
import { AdminArtigoService } from '../../core/services/admin-artigo.service';
import { ProgressoLeituraService } from '../../core/services/progresso-leitura.service';
import { ArtigoDetalhe, StatusArtigo } from '../../core/models/artigo.model';
import { mensagemDeErro } from '../../core/services/erro-api';

type EstadoTela = 'carregando' | 'ok' | 'nao-encontrado' | 'erro';

/**
 * Tela de leitura — todos os capítulos em sequência; o leitor só rola a página.
 *
 * - Rota pública /artigos/:slug → artigo publicado.
 * - Rota /admin/artigos/:id/previa (data.previa) → pré-visualização do professor.
 *
 * Ao rolar, a tela descobre qual capítulo está na parte de cima da janela (destacado no sumário)
 * e calcula o progresso do artigo inteiro. Clicar no sumário rola suavemente até o capítulo.
 * A posição fica no localStorage, por artigo, para retomar a leitura depois.
 */
@Component({
  selector: 'app-leitura-artigo',
  imports: [RouterLink, DatePipe, Header, Footer, Icone],
  templateUrl: './leitura-artigo.html',
  styleUrl: './leitura-artigo.scss',
})
export class LeituraArtigo implements OnDestroy {
  private readonly route = inject(ActivatedRoute);
  private readonly artigoService = inject(ArtigoService);
  private readonly adminArtigoService = inject(AdminArtigoService);
  private readonly progressoService = inject(ProgressoLeituraService);
  private readonly titulo = inject(Title);
  private readonly injector = inject(Injector);

  readonly previa = this.route.snapshot.data['previa'] === true;

  readonly artigo = signal<ArtigoDetalhe | null>(null);
  readonly statusPrevia = signal<StatusArtigo | null>(null);
  readonly estado = signal<EstadoTela>('carregando');
  readonly mensagemErro = signal('');

  /** Capítulo que está no topo da janela agora. */
  readonly indice = signal(0);
  /** Progresso do artigo inteiro (0 a 1), calculado pela rolagem. */
  readonly progresso = signal(0);
  readonly sumarioAberto = signal(true);
  readonly sumarioMobileAberto = signal(false);
  readonly retomadaEm = signal<number | null>(null);

  readonly capitulo = computed(() => this.artigo()?.capitulos[this.indice()] ?? null);
  readonly percentual = computed(() => Math.round(this.progresso() * 100));
  readonly minutosRestantes = computed(() => {
    const tempo = this.artigo()?.tempoLeituraMinutos ?? 0;
    return Math.max(0, Math.ceil(tempo * (1 - this.progresso())));
  });

  private readonly corpo = viewChild<ElementRef<HTMLElement>>('corpo');
  private readonly secoes = viewChildren<ElementRef<HTMLElement>>('secao');
  private slugAtual: string | null = null;
  private quadroAgendado = false;
  private temporizadorSalvar?: ReturnType<typeof setTimeout>;
  private temporizadorAviso?: ReturnType<typeof setTimeout>;

  constructor() {
    this.route.paramMap
      .pipe(
        tap(() => {
          this.salvarAgora();
          this.estado.set('carregando');
          this.artigo.set(null);
          this.indice.set(0);
          this.progresso.set(0);
          this.retomadaEm.set(null);
        }),
        switchMap(params => this.carregar(params.get('slug'), params.get('id'))),
        takeUntilDestroyed(),
      )
      .subscribe();
  }

  private carregar(slug: string | null, id: string | null): Observable<unknown> {
    const requisicao: Observable<ArtigoDetalhe> = this.previa
      ? this.adminArtigoService.obter(Number(id)).pipe(tap(a => this.statusPrevia.set(a.status)))
      : this.artigoService.obterPorSlug(slug ?? '');

    return requisicao.pipe(
      map(artigo => {
        this.slugAtual = this.previa ? null : artigo.slug;
        const salvo = this.slugAtual ? this.progressoService.obter(this.slugAtual) : null;
        const retomar = !!salvo && salvo.progresso > 0.03 && salvo.progresso < 0.98 && salvo.posicaoY > 300;

        this.artigo.set(artigo);
        this.estado.set('ok');
        this.titulo.setTitle(`${artigo.titulo} · Kleio História`);

        afterNextRender(async () => {
          if (retomar) {
            // espera as imagens carregarem para que a altura da página fique correta
            await this.aguardarImagens(2000);
            window.scrollTo({ top: salvo!.posicaoY, behavior: 'instant' });
            this.retomadaEm.set(Math.round(salvo!.progresso * 100));
            this.temporizadorAviso = setTimeout(() => this.retomadaEm.set(null), 8000);
          }
          this.atualizarPosicao();
        }, { injector: this.injector });
      }),
      catchError((erro: unknown) => {
        if (erro instanceof HttpErrorResponse && erro.status === 404) {
          this.estado.set('nao-encontrado');
        } else {
          this.mensagemErro.set(mensagemDeErro(erro, 'Não foi possível carregar o artigo.'));
          this.estado.set('erro');
        }
        this.titulo.setTitle('Artigo não encontrado · Kleio História');
        return of(null);
      }),
    );
  }

  // ---------- Sumário ----------

  /** Rola suavemente até o capítulo escolhido no sumário. */
  irParaCapitulo(i: number): void {
    this.sumarioMobileAberto.set(false);
    this.retomadaEm.set(null);
    const secao = this.secoes()[i]?.nativeElement;
    if (!secao) return;
    const reduzirMovimento = window.matchMedia('(prefers-reduced-motion: reduce)').matches;
    const folga = window.innerWidth <= 960 ? 80 : 24; // no celular o sumário fixo ocupa o topo
    window.scrollTo({
      top: secao.getBoundingClientRect().top + window.scrollY - folga,
      behavior: reduzirMovimento ? 'instant' : 'smooth',
    });
    secao.querySelector('h2')?.focus({ preventScroll: true });
  }

  alternarSumario(): void {
    this.sumarioAberto.update(v => !v);
  }

  recomecar(): void {
    if (this.slugAtual) this.progressoService.limpar(this.slugAtual);
    this.retomadaEm.set(null);
    window.scrollTo({ top: 0, behavior: 'smooth' });
  }

  fecharAviso(): void {
    this.retomadaEm.set(null);
  }

  // ---------- Rolagem e salvamento ----------

  @HostListener('window:scroll')
  @HostListener('window:resize')
  aoRolar(): void {
    // requestAnimationFrame: no máximo um cálculo por quadro, mesmo rolando rápido
    if (this.quadroAgendado) return;
    this.quadroAgendado = true;
    requestAnimationFrame(() => {
      this.quadroAgendado = false;
      this.atualizarPosicao();
      this.agendarSalvamento();
    });
  }

  @HostListener('window:pagehide')
  ngOnDestroy(): void {
    this.salvarAgora();
    clearTimeout(this.temporizadorAviso);
  }

  private atualizarPosicao(): void {
    const corpo = this.corpo()?.nativeElement;
    if (!corpo) return;

    // Progresso: quanto do texto já passou pela janela.
    const topo = corpo.getBoundingClientRect().top + window.scrollY;
    const inicio = topo - 120;
    const fim = topo + corpo.offsetHeight - window.innerHeight;
    const valor = fim <= inicio ? 1 : (window.scrollY - inicio) / (fim - inicio);
    this.progresso.set(Math.min(1, Math.max(0, valor)));

    // Capítulo atual: o último cujo título já passou de 1/3 da altura da janela.
    const linha = window.innerHeight / 3;
    let atual = 0;
    this.secoes().forEach((s, i) => {
      if (s.nativeElement.getBoundingClientRect().top <= linha) atual = i;
    });
    this.indice.set(atual);
  }

  private agendarSalvamento(): void {
    if (!this.slugAtual) return;
    clearTimeout(this.temporizadorSalvar);
    this.temporizadorSalvar = setTimeout(() => this.salvarAgora(), 400);
  }

  private salvarAgora(): void {
    clearTimeout(this.temporizadorSalvar);
    if (this.slugAtual && this.estado() === 'ok') {
      this.progressoService.salvar(this.slugAtual, this.indice(), window.scrollY, this.progresso());
    }
  }

  private aguardarImagens(limiteMs: number): Promise<void> {
    const imagens = Array.from(document.querySelectorAll<HTMLImageElement>('.pagina-leitura img'));
    const pendentes = imagens.filter(img => !img.complete).map(img => new Promise<void>(resolve => {
      img.addEventListener('load', () => resolve(), { once: true });
      img.addEventListener('error', () => resolve(), { once: true });
    }));
    const limite = new Promise<void>(resolve => setTimeout(resolve, limiteMs));
    return Promise.race([Promise.all(pendentes).then(() => undefined), limite]);
  }
}
