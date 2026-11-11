import { Injectable } from '@angular/core';

/** O que guardamos por artigo no localStorage. */
export interface ProgressoLeitura {
  slug: string;
  capitulo: number;     // índice do capítulo aberto (0 = primeiro)
  posicaoY: number;     // pixels rolados dentro do capítulo
  progresso: number;    // 0 a 1
  atualizadoEm: string; // ISO
}

const PREFIXO = 'kleio.leitura.';

/**
 * Salva e recupera a posição de leitura no localStorage do navegador.
 * Cada artigo usa sua própria chave (kleio.leitura.{slug}), então as posições
 * de artigos diferentes nunca se misturam. Não exige conta de usuário.
 */
@Injectable({ providedIn: 'root' })
export class ProgressoLeituraService {
  obter(slug: string): ProgressoLeitura | null {
    try {
      const bruto = localStorage.getItem(PREFIXO + slug);
      if (!bruto) return null;
      const dado = JSON.parse(bruto) as ProgressoLeitura;
      if (dado.slug !== slug || !Number.isFinite(dado.posicaoY)) return null;
      return { ...dado, capitulo: Number.isInteger(dado.capitulo) ? dado.capitulo : 0 };
    } catch {
      return null; // storage bloqueado ou JSON corrompido
    }
  }

  salvar(slug: string, capitulo: number, posicaoY: number, progresso: number): void {
    const dado: ProgressoLeitura = {
      slug,
      capitulo,
      posicaoY: Math.max(0, Math.round(posicaoY)),
      progresso: Math.min(1, Math.max(0, progresso)),
      atualizadoEm: new Date().toISOString(),
    };
    try {
      localStorage.setItem(PREFIXO + slug, JSON.stringify(dado));
    } catch {
      /* sem storage: a leitura funciona normalmente, só não é lembrada */
    }
  }

  limpar(slug: string): void {
    try {
      localStorage.removeItem(PREFIXO + slug);
    } catch {
      /* ignora */
    }
  }
}
