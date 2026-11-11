import { Capitulo, CapituloSalvar } from './capitulo.model';

/**
 * Interfaces espelham os DTOs da API (Dtos/Artigos/*.cs).
 * Datas chegam como string ISO 8601 em UTC (ex.: "2026-11-04T13:00:00Z").
 */

export type StatusArtigo = 'rascunho' | 'publicado';

export type OrdemArtigos = 'recentes' | 'antigos' | 'titulo';

/** Card da listagem pública — ArtigoResumoDto. */
export interface ArtigoResumo {
  id: number;
  titulo: string;
  slug: string;
  resumo: string;
  categoria: string;
  imagemCapa: string | null;
  imagemBanner: string | null;
  autor: string | null;
  dataPublicacao: string | null;
  tempoLeituraMinutos: number;
  destaque: boolean;
}

/** Artigo completo para leitura — ArtigoDetalheDto. */
export interface ArtigoDetalhe {
  id: number;
  titulo: string;
  slug: string;
  resumo: string;
  categoria: string;
  imagemCapa: string | null;
  autor: string | null;
  dataPublicacao: string | null;
  tempoLeituraMinutos: number;
  capitulos: Capitulo[];
}

/** Linha da listagem do painel — ArtigoAdminResumoDto. */
export interface ArtigoAdminResumo {
  id: number;
  titulo: string;
  slug: string;
  categoria: string;
  imagemCapa: string | null;
  status: StatusArtigo;
  destaque: boolean;
  totalCapitulos: number;
  dataCriacao: string;
  dataAtualizacao: string;
  dataPublicacao: string | null;
}

/** Artigo para edição e pré-visualização — ArtigoAdminDetalheDto. */
export interface ArtigoAdminDetalhe extends ArtigoDetalhe {
  imagemBanner: string | null;
  destaque: boolean;
  status: StatusArtigo;
  arquivoPdfOrigem: string | null;
  dataCriacao: string;
  dataAtualizacao: string;
}

/** Corpo de POST/PUT /api/admin/artigos — ArtigoSalvarDto. */
export interface ArtigoSalvar {
  titulo: string;
  resumo: string;
  categoria: string;
  imagemCapa: string | null;
  imagemBanner: string | null;
  autor: string | null;
  slug: string | null;
  destaque: boolean;
  arquivoPdfOrigem: string | null;
  capitulos: CapituloSalvar[];
}

/** Filtros de GET /api/artigos — FiltroArtigosDto. */
export interface FiltroArtigos {
  busca?: string;
  categoria?: string;
  ordem?: OrdemArtigos;
  destaque?: boolean;
}
