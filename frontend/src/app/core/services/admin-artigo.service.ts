import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpEvent } from '@angular/common/http';
import { Observable } from 'rxjs';
import { API_URL } from '../api';
import { ArtigoAdminDetalhe, ArtigoAdminResumo, ArtigoSalvar } from '../models/artigo.model';
import { PdfImportacao } from '../models/conteudo.model';

/**
 * Endpoints ADMINISTRATIVOS de artigos. O AuthInterceptor anexa o JWT
 * automaticamente a toda requisição que começa com /api/admin.
 */
@Injectable({ providedIn: 'root' })
export class AdminArtigoService {
  private readonly http = inject(HttpClient);
  private readonly url = `${API_URL}/admin/artigos`;

  listar(): Observable<ArtigoAdminResumo[]> {
    return this.http.get<ArtigoAdminResumo[]>(this.url);
  }

  obter(id: number): Observable<ArtigoAdminDetalhe> {
    return this.http.get<ArtigoAdminDetalhe>(`${this.url}/${id}`);
  }

  criar(dados: ArtigoSalvar): Observable<ArtigoAdminDetalhe> {
    return this.http.post<ArtigoAdminDetalhe>(this.url, dados);
  }

  atualizar(id: number, dados: ArtigoSalvar): Observable<ArtigoAdminDetalhe> {
    return this.http.put<ArtigoAdminDetalhe>(`${this.url}/${id}`, dados);
  }

  excluir(id: number): Observable<void> {
    return this.http.delete<void>(`${this.url}/${id}`);
  }

  publicar(id: number): Observable<ArtigoAdminDetalhe> {
    return this.http.post<ArtigoAdminDetalhe>(`${this.url}/${id}/publicar`, {});
  }

  despublicar(id: number): Observable<ArtigoAdminDetalhe> {
    return this.http.post<ArtigoAdminDetalhe>(`${this.url}/${id}/despublicar`, {});
  }

  definirDestaque(id: number, destaque: boolean): Observable<void> {
    return this.http.put<void>(`${this.url}/${id}/destaque`, { destaque });
  }

  /**
   * Envia o PDF como multipart/form-data. Com reportProgress + observe: 'events'
   * recebemos o progresso do upload para exibir na tela.
   */
  importarPdf(arquivo: File): Observable<HttpEvent<PdfImportacao>> {
    const form = new FormData();
    form.append('arquivo', arquivo, arquivo.name);
    return this.http.post<PdfImportacao>(`${this.url}/importar-pdf`, form, {
      reportProgress: true,
      observe: 'events',
    });
  }
}
