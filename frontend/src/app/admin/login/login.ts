import { Component, inject, signal } from '@angular/core';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { HttpErrorResponse } from '@angular/common/http';
import { AuthService } from '../../core/services/auth.service';
import { mensagemDeErro } from '../../core/services/erro-api';
import { Icone } from '../../shared/icone/icone';

@Component({
  selector: 'app-login',
  imports: [ReactiveFormsModule, RouterLink, Icone],
  templateUrl: './login.html',
  styleUrl: './login.scss',
})
export class Login {
  private readonly fb = inject(FormBuilder);
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);

  readonly erro = signal('');
  readonly carregando = signal(false);
  readonly senhaVisivel = signal(false);
  readonly sessaoExpirada = this.route.snapshot.queryParamMap.has('expirado');

  readonly form = this.fb.nonNullable.group({
    email: ['', [Validators.required, Validators.email]],
    senha: ['', Validators.required],
  });

  campoInvalido(campo: 'email' | 'senha'): boolean {
    const c = this.form.controls[campo];
    return c.invalid && c.touched;
  }

  entrar(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }
    this.carregando.set(true);
    this.erro.set('');
    const { email, senha } = this.form.getRawValue();

    this.auth.login(email.trim(), senha).subscribe({
      next: () => {
        // Volta para a página que o professor tentou abrir antes do login (se houver).
        const retorno = this.route.snapshot.queryParamMap.get('retorno');
        const destino = retorno?.startsWith('/admin') ? retorno : '/admin/painel';
        this.router.navigateByUrl(destino);
      },
      error: (falha: unknown) => {
        this.carregando.set(false);
        this.form.controls.senha.reset();
        this.erro.set(
          falha instanceof HttpErrorResponse && falha.status === 401
            ? 'E-mail ou senha inválidos. Confira os dados e tente novamente.'
            : mensagemDeErro(falha, 'Não foi possível entrar.'),
        );
      },
    });
  }
}
