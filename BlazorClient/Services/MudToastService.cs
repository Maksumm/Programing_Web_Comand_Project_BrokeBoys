using MudBlazor;

namespace BlazorClient.Services
{
    /// <summary>Показує тост через MudBlazor (<c>MudSnackbarProvider</c> уже є в макеті).</summary>
    public sealed class MudToastService(ISnackbar snackbar) : IToastService
    {
        public void Info(string message) => snackbar.Add(message, Severity.Info);
    }
}
