import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { API_URL } from '../api';
import { ImagemUpload } from '../models/conteudo.model';

export const TAMANHO_MAXIMO_IMAGEM_MB = 5;
export const TIPOS_IMAGEM_ACEITOS = ['image/jpeg', 'image/png', 'image/gif', 'image/webp'];

/** Upload de imagens para o editor e para a capa (POST /api/admin/imagens). */
@Injectable({ providedIn: 'root' })
export class ImagemService {
  private readonly http = inject(HttpClient);

  /** Validação rápida no navegador (a API valida de novo, inclusive o conteúdo real). */
  validar(arquivo: File): string | null {
    if (!TIPOS_IMAGEM_ACEITOS.includes(arquivo.type)) return 'Formato não suportado. Use JPG, PNG, GIF ou WEBP.';
    if (arquivo.size > TAMANHO_MAXIMO_IMAGEM_MB * 1024 * 1024) return `A imagem excede ${TAMANHO_MAXIMO_IMAGEM_MB} MB.`;
    return null;
  }

  enviar(arquivo: File): Observable<ImagemUpload> {
    const form = new FormData();
    form.append('arquivo', arquivo, arquivo.name);
    return this.http.post<ImagemUpload>(`${API_URL}/admin/imagens`, form);
  }
}
