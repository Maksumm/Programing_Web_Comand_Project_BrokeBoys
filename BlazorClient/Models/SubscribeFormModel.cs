using System.ComponentModel.DataAnnotations;
using BlazorClient.Resources;

namespace BlazorClient.Models
{
    /// <summary>
    /// Модель форми підписки. Повідомлення валідації беруться з .resx (AppResources),
    /// тому відображаються мовою, яку обрав користувач.
    /// </summary>
    public class SubscribeFormModel
    {
        [Required(ErrorMessageResourceType = typeof(AppResources), ErrorMessageResourceName = nameof(AppResources.EmailRequired))]
        [EmailAddress(ErrorMessageResourceType = typeof(AppResources), ErrorMessageResourceName = nameof(AppResources.EmailInvalid))]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessageResourceType = typeof(AppResources), ErrorMessageResourceName = nameof(AppResources.CategoryRequired))]
        public string Category { get; set; } = string.Empty;
    }
}
