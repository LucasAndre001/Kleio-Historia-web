/** Usuário autenticado — UsuarioDto. */
export interface Usuario {
  id: number;
  nome: string;
  email: string;
  perfis: string[];
}

/** Corpo de POST /api/auth/login — LoginDto. */
export interface LoginRequisicao {
  email: string;
  senha: string;
}

/** Resposta do login — LoginRespostaDto. */
export interface LoginResposta {
  token: string;
  expiraEm: string;
  usuario: Usuario;
}
