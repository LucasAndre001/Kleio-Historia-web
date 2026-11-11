import { HttpErrorResponse } from '@angular/common/http';

/**
 * Transforma um erro HTTP em uma mensagem amigável.
 * A API responde no formato ProblemDetails: { title, detail, status, errors? }.
 */
export function mensagemDeErro(erro: unknown, padrao = 'Não foi possível concluir a operação.'): string {
  if (!(erro instanceof HttpErrorResponse)) return padrao;

  if (erro.status === 0) {
    return 'Não foi possível conectar ao servidor. Verifique se a API está em execução.';
  }
  if (erro.status === 429) {
    return 'Muitas tentativas em pouco tempo. Aguarde um minuto e tente novamente.';
  }

  const corpo = erro.error as { detail?: string; title?: string; errors?: Record<string, string[]> } | null;

  // Erros de validação do [ApiController]: { errors: { Campo: ["mensagem"] } }
  if (corpo?.errors) {
    const mensagens = Object.values(corpo.errors).flat();
    if (mensagens.length) return mensagens.slice(0, 3).join(' ');
  }

  return corpo?.detail || corpo?.title || padrao;
}
