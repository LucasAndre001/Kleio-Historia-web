import { Component, inject, output, signal } from '@angular/core';
import { HttpEventType } from '@angular/common/http';
import { AdminArtigoService } from '../../core/services/admin-artigo.service';
import { PdfImportacao } from '../../core/models/conteudo.model';
import { mensagemDeErro } from '../../core/services/erro-api';
import { Icone } from '../../shared/icone/icone';

const TAMANHO_MAXIMO_PDF_MB = 20;

/**
 * Janela "Importar PDF": envia o arquivo, mostra o resultado da extração e
 * entrega o conteúdo ao editor para revisão. Nada é salvo nem publicado aqui.
 */
@Component({
  selector: 'app-importar-pdf',
  imports: [Icone],
  templateUrl: './importar-pdf.html',
  styleUrl: './importar-pdf.scss',
})
export class ImportarPdf {
  private readonly artigoService = inject(AdminArtigoService);

  readonly fechar = output<void>();
  readonly revisar = output<PdfImportacao>();

  readonly tamanhoMaximoMb = TAMANHO_MAXIMO_PDF_MB;
  readonly arquivo = signal<File | null>(null);
  readonly arrastando = signal(false);
  readonly enviando = signal(false);
  readonly progresso = signal(0);
  readonly erro = signal('');
  readonly resultado = signal<PdfImportacao | null>(null);

  selecionar(evento: Event): void {
    const campo = evento.target as HTMLInputElement;
    this.definirArquivo(campo.files?.[0] ?? null);
    campo.value = '';
  }

  aoArrastar(evento: DragEvent, ativo: boolean): void {
    evento.preventDefault();
    this.arrastando.set(ativo);
  }

  aoSoltar(evento: DragEvent): void {
    evento.preventDefault();
    this.arrastando.set(false);
    this.definirArquivo(evento.dataTransfer?.files?.[0] ?? null);
  }

  private definirArquivo(arquivo: File | null): void {
    this.erro.set('');
    this.resultado.set(null);
    if (!arquivo) return;
    // Validação rápida no navegador; a API repete tudo e confere o conteúdo real.
    const ehPdf = arquivo.type === 'application/pdf' || arquivo.name.toLowerCase().endsWith('.pdf');
    if (!ehPdf) {
      this.erro.set('Selecione um arquivo PDF (.pdf).');
      return;
    }
    if (arquivo.size > TAMANHO_MAXIMO_PDF_MB * 1024 * 1024) {
      this.erro.set(`O arquivo tem ${(arquivo.size / 1024 / 1024).toFixed(1)} MB; o limite é ${TAMANHO_MAXIMO_PDF_MB} MB.`);
      return;
    }
    this.arquivo.set(arquivo);
    this.enviar();
  }

  enviar(): void {
    const arquivo = this.arquivo();
    if (!arquivo) return;
    this.enviando.set(true);
    this.progresso.set(0);
    this.erro.set('');
    this.artigoService.importarPdf(arquivo).subscribe({
      next: evento => {
        if (evento.type === HttpEventType.UploadProgress && evento.total) {
          this.progresso.set(Math.round((evento.loaded / evento.total) * 100));
        } else if (evento.type === HttpEventType.Response && evento.body) {
          this.resultado.set(evento.body);
          this.enviando.set(false);
        }
      },
      error: e => {
        this.enviando.set(false);
        this.erro.set(mensagemDeErro(e, 'Não foi possível importar o PDF.'));
      },
    });
  }

  confirmar(): void {
    const r = this.resultado();
    if (r) this.revisar.emit(r);
  }

  tamanhoLegivel(bytes: number): string {
    return bytes > 1024 * 1024 ? `${(bytes / 1024 / 1024).toFixed(1).replace('.', ',')} MB` : `${Math.ceil(bytes / 1024)} KB`;
  }
}
