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

        /// <summary>Необов'язковий поріг (дробове число). Роздільник залежить від культури: «3,5» для uk-UA, «3.5» для en-US.</summary>
        [Range(0.01, 1000, ErrorMessageResourceType = typeof(AppResources), ErrorMessageResourceName = nameof(AppResources.ThresholdRange))]
        public decimal? Threshold { get; set; }
    }
}
