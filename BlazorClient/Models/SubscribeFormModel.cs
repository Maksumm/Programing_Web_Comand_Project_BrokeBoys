using System.ComponentModel.DataAnnotations;

namespace BlazorClient.Models
{
    public class SubscribeFormModel
    {
        [Required(ErrorMessage = "Введіть електронну пошту")]
        [EmailAddress(ErrorMessage = "Некоректний формат пошти")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Оберіть категорію")]
        public string Category { get; set; } = string.Empty;
    }
}