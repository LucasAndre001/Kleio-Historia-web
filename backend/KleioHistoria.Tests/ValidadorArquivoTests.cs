using System.Text;
using KleioHistoria.API.Services;

namespace KleioHistoria.Tests;

/// <summary>A validação deve olhar o conteúdo real, não a extensão.</summary>
public class ValidadorArquivoTests
{
    [Fact]
    public void DetectarImagem_ReconheceAssinaturas()
    {
        Assert.Equal(".jpg", ValidadorArquivo.DetectarImagem(new byte[] { 0xFF, 0xD8, 0xFF, 0xE0 }));
        Assert.Equal(".png", ValidadorArquivo.DetectarImagem(new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A }));
        Assert.Equal(".gif", ValidadorArquivo.DetectarImagem("GIF89a"u8));
        Assert.Equal(".webp", ValidadorArquivo.DetectarImagem("RIFF\0\0\0\0WEBPVP8 "u8));
    }

    [Fact]
    public void DetectarImagem_RecusaSvgETextoDisfarcado()
    {
        Assert.Null(ValidadorArquivo.DetectarImagem("<svg xmlns=\"http://www.w3.org/2000/svg\">"u8));
        Assert.Null(ValidadorArquivo.DetectarImagem("conteudo.exe renomeado"u8));
        Assert.Null(ValidadorArquivo.DetectarImagem(ReadOnlySpan<byte>.Empty));
    }

    [Fact]
    public void EhPdf_VerificaCabecalho()
    {
        Assert.True(ValidadorArquivo.EhPdf(Encoding.ASCII.GetBytes("%PDF-1.7\n...")));
        Assert.False(ValidadorArquivo.EhPdf(Encoding.ASCII.GetBytes("PK\x03\x04 isto é um zip")));
    }

    [Fact]
    public void NomeSeguroParaExibicao_RemoveCaminho()
    {
        Assert.Equal("artigo.pdf", ValidadorArquivo.NomeSeguroParaExibicao("../../etc/artigo.pdf"));
    }
}
