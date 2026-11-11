/** Formulário público de sugestão — SugestaoCriarDto. */
export interface SugestaoNova {
  nome: string;
  email: string;
  telefone: string | null;
  temaSugerido: string;
  mensagem: string | null;
}

/** Confirmação devolvida ao visitante — SugestaoConfirmacaoDto. */
export interface SugestaoConfirmacao {
  id: number;
  dataEnvio: string;
}

/** Sugestão completa, vista no painel — SugestaoDto. */
export interface Sugestao extends SugestaoNova {
  id: number;
  dataEnvio: string;
}
