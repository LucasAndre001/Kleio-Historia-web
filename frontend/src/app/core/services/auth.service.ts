import { Injectable, computed, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, map, tap } from 'rxjs';
import { API_URL } from '../api';
import { LoginResposta, Usuario } from '../models/usuario.model';

const CHAVE_SESSAO = 'kleio.sessao';

interface Sessao {
  token: string;
  expiraEm: string;
  usuario: Usuario;
}

/**
 * Login do professor contra a API (ASP.NET Core Identity + JWT).
 *
 * O token fica no sessionStorage: sobrevive ao F5, mas é descartado ao fechar
 * a aba — um meio-termo razoável para um painel administrativo.
 * A segurança real está na API, que valida o token em toda requisição /api/admin.
 */
@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly http = inject(HttpClient);
  private readonly sessao = signal<Sessao | null>(this.lerSessao());

  readonly usuario = computed(() => this.sessao()?.usuario ?? null);

  /** Verifica também a expiração (o token tem validade definida pela API). */
  estaAutenticado(): boolean {
    const s = this.sessao();
    if (!s) return false;
    if (new Date(s.expiraEm).getTime() <= Date.now()) {
      this.logout();
      return false;
    }
    return true;
  }

  token(): string | null {
    return this.estaAutenticado() ? this.sessao()!.token : null;
  }

  login(email: string, senha: string): Observable<Usuario> {
    return this.http.post<LoginResposta>(`${API_URL}/auth/login`, { email, senha }).pipe(
      tap(resposta => this.salvarSessao(resposta)),
      map(resposta => resposta.usuario),
    );
  }

  logout(): void {
    this.sessao.set(null);
    try {
      sessionStorage.removeItem(CHAVE_SESSAO);
    } catch {
      /* storage indisponível */
    }
  }

  private salvarSessao(resposta: LoginResposta): void {
    const sessao: Sessao = { token: resposta.token, expiraEm: resposta.expiraEm, usuario: resposta.usuario };
    this.sessao.set(sessao);
    try {
      sessionStorage.setItem(CHAVE_SESSAO, JSON.stringify(sessao));
    } catch {
      /* segue só em memória */
    }
  }

  private lerSessao(): Sessao | null {
    try {
      const bruto = sessionStorage.getItem(CHAVE_SESSAO);
      return bruto ? (JSON.parse(bruto) as Sessao) : null;
    } catch {
      return null;
    }
  }
}
