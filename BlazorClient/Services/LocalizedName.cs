using System.Globalization;

namespace BlazorClient.Services
{
    public static class LocalizedName
    {
        /// <summary>Назва мовою інтерфейсу: українською для «uk», інакше англійською. Якщо потрібна назва порожня, береться інша.</summary>
        public static string Pick(string nameUk, string nameEn)
        {
            var ukrainian = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName == "uk";
            var preferred = ukrainian ? nameUk : nameEn;
            return !string.IsNullOrWhiteSpace(preferred) ? preferred : (ukrainian ? nameEn : nameUk);
        }
    }
}
