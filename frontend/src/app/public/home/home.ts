import { Component, DestroyRef, effect, inject, input, signal, untracked } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { takeUntilDestroyed, toSignal } from '@angular/core/rxjs-interop';
import { catchError, debounceTime, distinctUntilChanged, map, of, startWith, switchMap, tap } from 'rxjs';
import { Header } from '../../shared/header/header';
import { Footer } from '../../shared/footer/footer';
import { Icone } from '../../shared/icone/icone';
import { ArtigoCard } from '../../shared/artigo-card/artigo-card';
import { ArtigoService } from '../../core/services/artigo.service';
import { SugestaoService } from '../../core/services/sugestao.service';
import { ArtigoResumo, FiltroArtigos, OrdemArtigos } from '../../core/models/artigo.model';
import { mensagemDeErro } from '../../core/services/erro-api';

@Component({
  selector: 'app-home',
  imports: [RouterLink, ReactiveFormsModule, Header, Footer, ArtigoCard, Icone],
  templateUrl: './home.html',
  styleUrl: './home.scss',
})
export class Home {
  // inject() é a forma moderna de pedir dependências (alternativa ao constructor)
  private readonly fb = inject(FormBuilder);
  private readonly router = inject(Router);
  private readonly artigoService = inject(ArtigoService);
  private readonly sugestaoService = inject(SugestaoService);
  private readonly destroyRef = inject(DestroyRef);

  /** Query param ?categoria=... (vem dos links de categoria do header). */
  readonly categoria = input<string>();

  // ---------- Destaques (carrossel) ----------
  readonly destaques = toSignal(this.artigoService.listarDestaques().pipe(catchError(() => of([]))), {
    initialValue: [] as ArtigoResumo[],
  });
  readonly slideAtual = signal(0);
  /** Tempo de cada destaque na tela antes de passar para o próximo. */
  private readonly intervaloCarrossel = 6000;
  private temporizadorCarrossel?: ReturnType<typeof setInterval>;
  private carrosselPausado = false;

  // ---------- Listagem com filtros ----------
  readonly categorias = toSignal(this.artigoService.categorias().pipe(catchError(() => of([]))), {
    initialValue: [] as string[],
  });
  readonly artigos = signal<ArtigoResumo[]>([]);
  readonly carregando = signal(true);
  readonly erroListagem = signal('');

  readonly filtros = this.fb.nonNullable.group({
    busca: [''],
    categoria: [''],
    ordem: ['recentes' as OrdemArtigos],
  });

  // ---------- Sugestão ----------
  readonly enviando = signal(false);
  readonly sugestaoEnviada = signal(false);
  readonly erroSugestao = signal('');

  readonly formSugestao = this.fb.nonNullable.group({
    nome: ['', [Validators.required, Validators.minLength(2), Validators.maxLength(120)]],
    email: ['', [Validators.required, Validators.email, Validators.maxLength(200)]],
    telefone: ['', [Validators.pattern(/^[0-9()+\-\s]{8,30}$/)]],
    temaSugerido: ['', [Validators.required, Validators.minLength(3), Validators.maxLength(200)]],
    mensagem: ['', [Validators.maxLength(2000)]],
  });

  constructor() {
    // Carrossel automático: troca de destaque a cada 6 s; para quando o mouse/foco está sobre ele
    // ou quando a aba do navegador não está visível.
    this.reiniciarCarrossel();
    this.destroyRef.onDestroy(() => clearInterval(this.temporizadorCarrossel));
    // Quando o query param muda (clique numa categoria do header), atualiza o filtro.
    effect(() => {
      const categoria = this.categoria() ?? '';
      untracked(() => {
        if (this.filtros.controls.categoria.value !== categoria) {
          this.filtros.controls.categoria.setValue(categoria);
        }
      });
    });

    // Cada alteração nos filtros consulta a API (com debounce para a digitação).
    // switchMap cancela a requisição anterior se o usuário continuar digitando.
    this.filtros.valueChanges
      .pipe(
        startWith(this.filtros.getRawValue()),
        debounceTime(300),
        map(() => this.filtros.getRawValue()),
        distinctUntilChanged((a, b) => JSON.stringify(a) === JSON.stringify(b)),
        tap(() => {
          this.carregando.set(true);
          this.erroListagem.set('');
        }),
        switchMap(filtro =>
          this.artigoService.listar(filtro as FiltroArtigos).pipe(
            catchError(erro => {
              this.erroListagem.set(mensagemDeErro(erro, 'Não foi possível carregar os artigos.'));
              return of([] as ArtigoResumo[]);
            }),
          ),
        ),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe(lista => {
        this.artigos.set(lista);
        this.carregando.set(false);
      });
  }

  temFiltroAtivo(): boolean {
    const f = this.filtros.getRawValue();
    return !!(f.busca.trim() || f.categoria || f.ordem !== 'recentes');
  }

  /** Mantém a URL sincronizada com a categoria escolhida (permite compartilhar o link). */
  aoTrocarCategoria(): void {
    const categoria = this.filtros.controls.categoria.value || null;
    this.router.navigate([], { queryParams: { categoria }, queryParamsHandling: 'merge', replaceUrl: true });
  }

  limparFiltros(): void {
    this.filtros.setValue({ busca: '', categoria: '', ordem: 'recentes' });
    this.aoTrocarCategoria();
  }

  // ---------- Carrossel ----------
  proximo(): void {
    this.avancar();
    this.reiniciarCarrossel(); // clique manual: recomeça a contagem dos 6 s
  }

  anterior(): void {
    const total = this.destaques().length;
    if (total) this.slideAtual.update(i => (i - 1 + total) % total);
    this.reiniciarCarrossel();
  }

  irPara(i: number): void {
    this.slideAtual.set(i);
    this.reiniciarCarrossel();
  }

  pausarCarrossel(pausar: boolean): void {
    this.carrosselPausado = pausar;
  }

  private avancar(): void {
    const total = this.destaques().length;
    if (total) this.slideAtual.update(i => (i + 1) % total);
  }

  private reiniciarCarrossel(): void {
    clearInterval(this.temporizadorCarrossel);
    this.temporizadorCarrossel = setInterval(() => {
      if (!this.carrosselPausado && !document.hidden && this.destaques().length > 1) this.avancar();
    }, this.intervaloCarrossel);
  }

  // ---------- Sugestão ----------
  campoInvalido(campo: keyof typeof this.formSugestao.controls): boolean {
    const controle = this.formSugestao.controls[campo];
    return controle.invalid && (controle.touched || controle.dirty);
  }

  enviarSugestao(): void {
    if (this.formSugestao.invalid) {
      this.formSugestao.markAllAsTouched();
      return;
    }
    const valores = this.formSugestao.getRawValue();
    this.enviando.set(true);
    this.sugestaoEnviada.set(false);
    this.erroSugestao.set('');

    this.sugestaoService
      .enviar({
        nome: valores.nome.trim(),
        email: valores.email.trim(),
        telefone: valores.telefone.trim() || null,
        temaSugerido: valores.temaSugerido.trim(),
        mensagem: valores.mensagem.trim() || null,
      })
      .subscribe({
        next: () => {
          this.enviando.set(false);
          this.sugestaoEnviada.set(true);
          this.formSugestao.reset();
          setTimeout(() => this.sugestaoEnviada.set(false), 6000);
        },
        error: erro => {
          this.enviando.set(false);
          this.erroSugestao.set(mensagemDeErro(erro, 'Não foi possível enviar sua sugestão.'));
        },
      });
  }
}
