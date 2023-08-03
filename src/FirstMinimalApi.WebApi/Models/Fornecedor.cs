using System.ComponentModel.DataAnnotations;

namespace FirstMinimalApi.WebApi.Models;

public sealed class Fornecedor
{
    public Guid Id { get; set; }

    [Required(ErrorMessage = "O fornecedor deve possuir um nome."), MinLength(3, ErrorMessage = "O nome do fornecedor deve conter pelo menos 3 caracteres.")]
    public string? Nome { get; set; }

    [Required(ErrorMessage = "O fornecedor deve possuir um documento."), StringLength(14, ErrorMessage = "O documento do fornecedor deve conter até 14 caracteres.")]
    public string? Documento { get; set; }

    public bool Ativo { get; set; }
}