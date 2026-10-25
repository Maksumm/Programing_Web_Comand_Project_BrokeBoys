using System.Net;
using System.Net.Http.Json;
using Microsoft.Playwright;
using Microsoft.Playwright.Xunit;

namespace E2ETests;

/// <summary>
/// Наскрізний тест: справжній API на Kestrel над PostgreSQL (Testcontainers) і справжній клієнт у Chromium.
/// Зміна ціни через API з'являється на відкритій сторінці через SignalR — без перезавантаження.
/// </summary>
[Collection("E2E")]
public class CalendarLiveUpdateTests(E2EFixture fixture) : PageTest
{
    [Fact]
    public async Task PriceChange_AppearsOnTheOpenCalendar_WithoutReload()
    {
        Assertions.SetDefaultExpectTimeout(60_000); // перший запуск Blazor WebAssembly у браузері повільний
        await fixture.ResetAsync();

        await Page.GotoAsync(fixture.ClientUrl + "calendar");

        // липень дубових дров у демо-даних = 1200; назва місяця й інші ціни інакші
        var oldPrice = Page.GetByText("1200", new() { Exact = true });
        await Expect(oldPrice).ToBeVisibleAsync();

        // клієнт уже підписаний на групу своєї категорії (інакше повідомлення пропаде)
        await Expect(Page.Locator("[data-live='connected']")).ToBeVisibleAsync();

        var response = await fixture.ApiClient.PutAsJsonAsync("/api/prices/7", new { productId = 1, month = 7, price = 4242 });
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        await Expect(Page.GetByText("4242", new() { Exact = true })).ToBeVisibleAsync();
        await Expect(oldPrice).ToBeHiddenAsync();
    }
}
