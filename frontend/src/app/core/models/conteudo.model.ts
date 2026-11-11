/** Resposta do upload de imagem — ImagemUploadRespostaDto. */
export interface ImagemUpload {
  url: string;
  nomeArquivo: string;
  tamanhoBytes: number;
}

/** Capítulo sugerido pela importação — CapituloExtraidoDto. */
export interface CapituloExtraido {
  titulo: string;
  conteudoHtml: string;
}

/** Texto de uma página do PDF — PaginaExtraidaDto. */
export interface PaginaExtraida {
  numero: number;
  texto: string;
}

/** Resultado de POST /api/admin/artigos/importar-pdf — PdfImportacaoDto. */
export interface PdfImportacao {
  arquivoId: string;
  nomeArquivoOriginal: string;
  totalPaginas: number;
  tituloSugerido: string | null;
  resumoSugerido: string | null;
  capitulos: CapituloExtraido[];
  paginas: PaginaExtraida[];
  avisos: string[];
}
