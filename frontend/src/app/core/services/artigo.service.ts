import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable, shareReplay } from 'rxjs';
import { API_URL } from '../api';
import { ArtigoDetalhe, ArtigoResumo, FiltroArtigos } from '../models/artigo.model';

/**
 * Endpoints PÚBLICOS de artigos (somente publicados).
 * Os mocks da primeira fase foram substituídos por chamadas HttpClient,
 * mantendo as mesmas interfaces TypeScript.
 */
@Injectable({ providedIn: 'root' })
export class ArtigoService {
  private readonly http = inject(HttpClient);
  private readonly url = `${API_URL}/artigos`;

  /** As categorias mudam pouco: guardamos a resposta para o header e a Home compartilharem. */
  private categorias$?: Observable<string[]>;

  listar(filtro: FiltroArtigos = {}): Observable<ArtigoResumo[]> {
    let params = new HttpParams();
    if (filtro.busca?.trim()) params = params.set('busca', filtro.busca.trim());
    if (filtro.categoria) params = params.set('categoria', filtro.categoria);
    if (filtro.ordem) params = params.set('ordem', filtro.ordem);
    if (filtro.destaque) params = params.set('destaque', 'true');
    return this.http.get<ArtigoResumo[]>(this.url, { params });
  }

  listarDestaques(): Observable<ArtigoResumo[]> {
    return this.listar({ destaque: true });
  }

  categorias(): Observable<string[]> {
    this.categorias$ ??= this.http.get<string[]>(`${this.url}/categorias`).pipe(shareReplay(1));
    return this.categorias$;
  }

  obterPorSlug(slug: string): Observable<ArtigoDetalhe> {
    return this.http.get<ArtigoDetalhe>(`${this.url}/${encodeURIComponent(slug)}`);
  }
}
