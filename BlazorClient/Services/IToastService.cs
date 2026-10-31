namespace BlazorClient.Services
{
    /// <summary>Коротке повідомлення-«тост» у куті екрана. Окремий інтерфейс, щоб логіку сповіщень можна було перевірити без MudBlazor.</summary>
    public interface IToastService
    {
        void Info(string message);
    }
}
