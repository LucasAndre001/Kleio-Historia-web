namespace KleioHistoria.API.Dtos.Imagens;

/// <summary>Resultado do upload: a URL local para usar no editor ou como capa.</summary>
public record ImagemUploadRespostaDto(string Url, string NomeArquivo, long TamanhoBytes);
