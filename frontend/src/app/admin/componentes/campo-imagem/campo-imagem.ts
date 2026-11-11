import { Component, forwardRef, inject, input, signal } from '@angular/core';
import { ControlValueAccessor, NG_VALUE_ACCESSOR } from '@angular/forms';
import { Icone } from '../../../shared/icone/icone';
import { ImagemService, TIPOS_IMAGEM_ACEITOS } from '../../../core/services/imagem.service';
import { mensagemDeErro } from '../../../core/services/erro-api';

/**
 * Campo de imagem do editor (capa e banner do carrossel).
 *
 * Aceita as duas origens:
 *  - "Do computador": o arquivo é enviado para a API (POST /api/admin/imagens), que valida
 *    o conteúdo real, salva em /uploads/imagens e devolve a URL;
 *  - "Link da internet": a URL http(s) é guardada como está (a API recusa javascript:, data: etc.).
 *
 * Implementa ControlValueAccessor, então funciona com formControlName como um <input> comum:
 * o valor do controle é sempre a URL final da imagem ('' = sem imagem).
 */
@Component({
  selector: 'app-campo-imagem',
  imports: [Icone],
  templateUrl: './campo-imagem.html',
  styleUrl: './campo-imagem.scss',
  providers: [{ provide: NG_VALUE_ACCESSOR, useExisting: forwardRef(() => CampoImagem), multi: true }],
})
export class CampoImagem implements ControlValueAccessor {
  private readonly imagemService = inject(ImagemService);

  readonly rotulo = input.required<string>();
  readonly dica = input('');
  /** Proporção da miniatura: 'capa' (4:3) ou 'banner' (larga). */
  readonly formato = input<'capa' | 'banner'>('capa');

  readonly valor = signal('');
  readonly origem = signal<'arquivo' | 'link'>('arquivo');
  readonly enviando = signal(false);
  readonly erro = signal('');
  readonly previaQuebrada = signal(false);
  readonly desabilitado = signal(false);
  readonly tiposAceitos = TIPOS_IMAGEM_ACEITOS.join(',');

  private aoMudar: (v: string) => void = () => {};
  private aoTocar: () => void = () => {};

  // ----- ControlValueAccessor -----
  writeValue(v: string | null): void {
    const url = v ?? '';
    this.valor.set(url);
    this.previaQuebrada.set(false);
    this.erro.set('');
    // imagem que não veio de upload local é um link da internet
    if (url && !url.startsWith('/uploads/')) this.origem.set('link');
  }
  registerOnChange(fn: (v: string) => void): void { this.aoMudar = fn; }
  registerOnTouched(fn: () => void): void { this.aoTocar = fn; }
  setDisabledState(d: boolean): void { this.desabilitado.set(d); }

  // ----- Ações -----
  enviarArquivo(evento: Event): void {
    const campo = evento.target as HTMLInputElement;
    const arquivo = campo.files?.[0];
    campo.value = ''; // permite escolher o mesmo arquivo de novo
    if (!arquivo) return;

    const problema = this.imagemService.validar(arquivo);
    if (problema) {
      this.erro.set(problema);
      return;
    }
    this.erro.set('');
    this.enviando.set(true);
    this.imagemService.enviar(arquivo).subscribe({
      next: r => {
        this.enviando.set(false);
        this.definir(r.url);
      },
      error: e => {
        this.enviando.set(false);
        this.erro.set(mensagemDeErro(e, 'Falha ao enviar a imagem.'));
      },
    });
  }

  aoDigitarLink(evento: Event): void {
    const url = (evento.target as HTMLInputElement).value.trim();
    if (url && !/^https?:\/\/\S+$/i.test(url)) {
      this.erro.set('Cole um endereço que comece com http:// ou https://');
      return;
    }
    this.erro.set('');
    this.definir(url);
  }

  remover(): void {
    this.definir('');
  }

  private definir(url: string): void {
    this.valor.set(url);
    this.previaQuebrada.set(false);
    this.aoMudar(url);
    this.aoTocar();
  }
}
