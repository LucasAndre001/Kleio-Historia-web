import { Component, input } from '@angular/core';
import { DatePipe } from '@angular/common';
import { RouterLink } from '@angular/router';
import { ArtigoResumo } from '../../core/models/artigo.model';
import { Icone } from '../icone/icone';

/** Card de artigo da página inicial: imagem com categoria + faixa com título e "Leia sobre". */
@Component({
  selector: 'app-artigo-card',
  imports: [RouterLink, DatePipe, Icone],
  template: `
    @let a = artigo();
    <article class="card">
      <div class="imagem">
        @if (a.imagemCapa) {
          <img [src]="a.imagemCapa" alt="" loading="lazy" />
        } @else {
          <div class="sem-imagem"><app-icone nome="imagem" [tamanho]="42" [espessura]="1.1" /></div>
        }
        <span class="categoria">{{ a.categoria }}</span>
      </div>
      <div class="faixa">
        <h3>{{ a.titulo }}</h3>
        <span class="meta">
          @if (a.dataPublicacao) { {{ a.dataPublicacao | date: 'dd/MM/yyyy' }} }
          @if (a.tempoLeituraMinutos > 0) { · {{ a.tempoLeituraMinutos }} min }
        </span>
        <a class="btn" [routerLink]="['/artigos', a.slug]" [attr.aria-label]="'Leia sobre ' + a.titulo">Leia sobre</a>
      </div>
    </article>
  `,
  styles: `
    :host { display: block; }
    .card {
      height: 100%; display: flex; flex-direction: column;
      background: var(--cartao); border-radius: var(--raio); overflow: hidden;
      box-shadow: var(--sombra); transition: transform 180ms ease, box-shadow 180ms ease;
    }
    .card:hover { transform: translateY(-3px); box-shadow: 0 8px 20px rgba(43, 24, 16, 0.22); }
    .imagem { position: relative; aspect-ratio: 4 / 3; }
    .imagem img, .sem-imagem { width: 100%; height: 100%; object-fit: cover; display: flex; }
    .categoria {
      position: absolute; top: 12px; left: 12px; max-width: calc(100% - 24px);
      background: rgba(253, 243, 234, 0.95); color: #6a2c1a;
      font-family: var(--fonte-marca); font-size: 0.68rem; letter-spacing: 0.1em;
      padding: 5px 10px; border-radius: 3px;
    }
    .faixa {
      flex: 1; background: var(--marrom-faixa); color: var(--claro);
      padding: 0.9rem 1rem 1rem; display: flex; flex-direction: column; align-items: center; gap: 0.45rem; text-align: center;
    }
    h3 { margin: 0; font-family: var(--fonte-marca); font-weight: 600; font-size: 1.1rem; line-height: 1.25; }
    .meta { font-size: 0.9rem; color: #f0d6c2; min-height: 1.1em; }
    .btn { margin-top: auto; min-height: 40px; padding: 0 1.8rem; font-size: 1.05rem; }
  `,
})
export class ArtigoCard {
  readonly artigo = input.required<ArtigoResumo>();
}
