using System.ComponentModel.DataAnnotations;

namespace BlazorClient.Models
{
    public class SubscribeFormModel
    {
        [Required(ErrorMessage = "Поле обов'язкове для заповнення")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Оберіть категорію")]
        public string Category { get; set; } = string.Empty;

        // Додаємо також SelectedCategory для сумісності з іншими викликами
        public string SelectedCategory
        {
            get => Category;
            set => Category = value;
        }
    }
}