/** Imagem extraída do conteúdo do capítulo — ImagemCapituloDto. */
export interface ImagemCapitulo {
  id: number;
  url: string;
  legenda: string | null;
  textoAlternativo: string | null;
}

/** Capítulo devolvido pela API, já ordenado — CapituloDto. */
export interface Capitulo {
  id: number;
  ordem: number;
  titulo: string;
  conteudoHtml: string;
  imagens: ImagemCapitulo[];
}

/** Capítulo enviado pelo editor — CapituloSalvarDto (a ordem é a posição na lista). */
export interface CapituloSalvar {
  titulo: string;
  conteudoHtml: string;
}
