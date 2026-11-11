import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { API_URL } from '../api';
import { Sugestao, SugestaoConfirmacao, SugestaoNova } from '../models/sugestao.model';

@Injectable({ providedIn: 'root' })
export class SugestaoService {
  private readonly http = inject(HttpClient);

  /** Público: qualquer visitante pode enviar (sem login). */
  enviar(sugestao: SugestaoNova): Observable<SugestaoConfirmacao> {
    return this.http.post<SugestaoConfirmacao>(`${API_URL}/sugestoes`, sugestao);
  }

  /** Administrativo: lista as sugestões recebidas. */
  listar(): Observable<Sugestao[]> {
    return this.http.get<Sugestao[]>(`${API_URL}/admin/sugestoes`);
  }

  excluir(id: number): Observable<void> {
    return this.http.delete<void>(`${API_URL}/admin/sugestoes/${id}`);
  }
}
