using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net;
using System.Net.Http.Json;
using ApiApp.Realtime;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.SignalR.Client;

namespace ApiApp.Tests;

/// <summary>Повідомлення SignalR отримують лише групи, підписані на категорію зміненої ціни.</summary>
[Collection("Api")]
public class RealtimeTests(PostgresFixture fixture) : ApiTestBase(fixture)
{
    private sealed record PriceChangedDto(string Kind, int CategoryId, int ProductId, int Month, decimal? Price);

    private readonly List<HubConnection> _hubs = [];

    public override async Task DisposeAsync()
    {
        foreach (var hub in _hubs)
        {
            await hub.DisposeAsync();
        }

        await base.DisposeAsync();
    }

    private HubConnection CreateHub(string? token)
    {
        var hub = new HubConnectionBuilder()
            .WithUrl(new Uri(Factory.Server.BaseAddress, "hubs/prices"), options =>
            {
                options.HttpMessageHandlerFactory = _ => Factory.Server.CreateHandler();
                options.Transports = HttpTransportType.LongPolling;
                if (token is not null)
                {
                    options.AccessTokenProvider = () => Task.FromResult<string?>(token);
                }
            })
            .Build();
        _hubs.Add(hub);
        return hub;
    }

    private async Task<ConcurrentQueue<PriceChangedDto>> ConnectAsync(params int[] categoryIds)
    {
        var inbox = new ConcurrentQueue<PriceChangedDto>();
        var hub = CreateHub(TestTokens.Create(roles: [TestTokens.AdminRole]));
        hub.On<PriceChangedDto>("PriceChanged", message => inbox.Enqueue(message));
        await hub.StartAsync();
        foreach (var categoryId in categoryIds)
        {
            await hub.InvokeAsync("JoinCategory", categoryId);
        }

        return inbox;
    }

    private static async Task<bool> EventuallyAsync(Func<bool> condition)
    {
        for (var i = 0; i < 60; i++)
        {
            if (condition())
            {
                return true;
            }

            await Task.Delay(50);
        }

        return condition();
    }

    private Task<HttpResponseMessage> PutPriceAsync(int id, int productId, int month, decimal price) =>
        Client.PutAsJsonAsync($"/api/prices/{id}", new { productId, month, price });

    [Fact]
    public async Task Hub_RejectsConnectionsWithoutToken_OrWithTokenForAnotherApi()
    {
        await Assert.ThrowsAnyAsync<Exception>(() => CreateHub(token: null).StartAsync());

        var foreignAudience = TestTokens.Create(audience: "api://99999999-9999-9999-9999-999999999999");
        await Assert.ThrowsAnyAsync<Exception>(() => CreateHub(foreignAudience).StartAsync());
    }

    [Fact]
    public async Task ChangedPrice_IsSentOnlyToGroupsOfItsCategory()
    {
        var subscribedToFirewood = await ConnectAsync(1);
        var subscribedToTires = await ConnectAsync(2);
        var notSubscribed = await ConnectAsync();

        Assert.Equal(HttpStatusCode.NoContent, (await PutPriceAsync(7, productId: 1, month: 7, price: 4242m)).StatusCode);

        Assert.True(await EventuallyAsync(() => subscribedToFirewood.Count == 1));
        var message = Assert.Single(subscribedToFirewood);
        Assert.Equal(new PriceChangedDto("Updated", 1, 1, 7, 4242m), message);

        await Task.Delay(300);
        Assert.Empty(subscribedToTires);
        Assert.Empty(notSubscribed);
    }

    [Fact]
    public async Task DeletingAndCreatingPrices_Notify_WithTheirKind()
    {
        var inbox = await ConnectAsync(1);

        Assert.Equal(HttpStatusCode.NoContent, (await Client.DeleteAsync("/api/prices/1")).StatusCode);
        Assert.Equal(HttpStatusCode.Created, (await Client.PostAsJsonAsync("/api/prices", new { productId = 1, month = 1, price = 1800m })).StatusCode);

        Assert.True(await EventuallyAsync(() => inbox.Count == 2));
        Assert.Equal(new[] { "Deleted", "Created" }, inbox.Select(m => m.Kind));
        Assert.Null(inbox.First().Price);
        Assert.Equal(1800m, inbox.Last().Price);
    }

    [Fact]
    public async Task MovingAPriceToAnotherCategory_NotifiesBothCategories()
    {
        // звільняємо січень товару 3 (категорія «Шини»), щоб ціну №1 (категорія «Дрова») було куди перенести
        Assert.Equal(HttpStatusCode.NoContent, (await Client.DeleteAsync("/api/prices/25")).StatusCode);
        var firewood = await ConnectAsync(1);
        var tires = await ConnectAsync(2);

        Assert.Equal(HttpStatusCode.NoContent, (await PutPriceAsync(1, productId: 3, month: 1, price: 5m)).StatusCode);

        Assert.True(await EventuallyAsync(() => firewood.Count == 1 && tires.Count == 1));
        Assert.Equal(1, firewood.Single().CategoryId);
        Assert.Equal(2, tires.Single().CategoryId);
    }

    [Fact]
    public async Task ReplaceWithoutChanges_SendsNothing()
    {
        var inbox = await ConnectAsync(1);

        Assert.Equal(HttpStatusCode.NoContent, (await PutPriceAsync(7, productId: 1, month: 7, price: 1200m)).StatusCode);

        await Task.Delay(400);
        Assert.Empty(inbox);
    }

    [Fact]
    public async Task Broadcast_RunsInItsOwnSpan_WithTags()
    {
        var spans = new ConcurrentBag<Activity>();
        using var listener = new ActivityListener
        {
            ShouldListenTo = source => source.Name == Telemetry.SourceName,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded,
            ActivityStopped = spans.Add
        };
        ActivitySource.AddActivityListener(listener);

        Assert.Equal(HttpStatusCode.NoContent, (await PutPriceAsync(7, productId: 1, month: 7, price: 4242m)).StatusCode);

        var span = Assert.Single(spans);
        Assert.Equal("BroadcastPriceChange", span.OperationName);
        Assert.Equal("Updated", span.GetTagItem("price.kind"));
        Assert.Equal(1, span.GetTagItem("product.id"));
    }
}
