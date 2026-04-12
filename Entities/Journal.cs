using System.ComponentModel.DataAnnotations;

namespace MyDergiApp.Entities;

public class Journal
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Dergi adı zorunludur.")]
    [StringLength(200)]
    [Display(Name = "Dergi Adı")]
    public string Name { get; set; } = string.Empty;

    [StringLength(50)]
    [Display(Name = "ISSN")]
    public string? Issn { get; set; }

    [StringLength(1000)]
    [Display(Name = "Açıklama")]
    public string? Description { get; set; }

    [Display(Name = "Aktif mi?")]
    public bool IsActive { get; set; } = true;
}
