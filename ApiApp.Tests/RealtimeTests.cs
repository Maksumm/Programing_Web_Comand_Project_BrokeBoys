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

    private sealed record PriceNamesDto(string Kind, string ProductNameUk, string ProductNameEn);

    private sealed record CatalogueChangedDto(string Entity, string Kind, int CategoryId, int? ProductId, string NameUk, string NameEn);

    private sealed record IdDto(int Id);

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

    private async Task<ConcurrentQueue<T>> ListenAsync<T>(string method, params int[] categoryIds)
    {
        var inbox = new ConcurrentQueue<T>();
        var hub = CreateHub(TestTokens.Create(roles: [TestTokens.AdminRole]));
        hub.On<T>(method, message => inbox.Enqueue(message));
        await hub.StartAsync();
        foreach (var categoryId in categoryIds)
        {
            await hub.InvokeAsync("JoinCategory", categoryId);
        }

        return inbox;
    }

    private Task<ConcurrentQueue<PriceChangedDto>> ConnectAsync(params int[] categoryIds) =>
        ListenAsync<PriceChangedDto>("PriceChanged", categoryIds);

    private Task<ConcurrentQueue<CatalogueChangedDto>> ConnectCatalogueAsync(params int[] categoryIds) =>
        ListenAsync<CatalogueChangedDto>("CatalogueChanged", categoryIds);

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

    [Fact]
    public async Task PriceMessages_CarryTheProductNamesInBothLanguages()
    {
        var inbox = await ListenAsync<PriceNamesDto>("PriceChanged", 1);

        Assert.Equal(HttpStatusCode.NoContent, (await PutPriceAsync(7, productId: 1, month: 7, price: 4242m)).StatusCode);

        Assert.True(await EventuallyAsync(() => inbox.Count == 1));
        var message = inbox.Single();
        Assert.Equal("Колоті дрова, дуб", message.ProductNameUk);
        Assert.Equal("Split firewood, oak", message.ProductNameEn);
    }

    [Fact]
    public async Task NewProduct_IsAnnouncedOnlyToItsCategory()
    {
        var firewood = await ConnectCatalogueAsync(1);
        var tires = await ConnectCatalogueAsync(2);

        var created = await Client.PostAsJsonAsync("/api/products", new { categoryId = 1, nameUk = "Брикети", nameEn = "Briquettes" });
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var id = (await created.Content.ReadFromJsonAsync<IdDto>())!.Id;

        Assert.True(await EventuallyAsync(() => firewood.Count == 1));
        Assert.Equal(new CatalogueChangedDto("Product", "Created", 1, id, "Брикети", "Briquettes"), firewood.Single());

        await Task.Delay(300);
        Assert.Empty(tires);
    }

    [Fact]
    public async Task RenamingAProduct_SendsUpdated_AndRepeatingTheSameRequestSendsNothing()
    {
        var inbox = await ConnectCatalogueAsync(1);
        var body = new { categoryId = 1, nameUk = "Дуб колотий", nameEn = "Split oak" };

        Assert.Equal(HttpStatusCode.NoContent, (await Client.PutAsJsonAsync("/api/products/1", body)).StatusCode);
        Assert.True(await EventuallyAsync(() => inbox.Count == 1));
        Assert.Equal(new CatalogueChangedDto("Product", "Updated", 1, 1, "Дуб колотий", "Split oak"), inbox.Single());

        Assert.Equal(HttpStatusCode.NoContent, (await Client.PutAsJsonAsync("/api/products/1", body)).StatusCode);

        await Task.Delay(400);
        Assert.Single(inbox);
    }

    [Fact]
    public async Task MovingAProductToAnotherCategory_NotifiesBothCategories()
    {
        var firewood = await ConnectCatalogueAsync(1);
        var tires = await ConnectCatalogueAsync(2);

        var moved = await Client.PutAsJsonAsync("/api/products/1", new { categoryId = 2, nameUk = "Колоті дрова, дуб", nameEn = "Split firewood, oak" });
        Assert.Equal(HttpStatusCode.NoContent, moved.StatusCode);

        Assert.True(await EventuallyAsync(() => firewood.Count == 1 && tires.Count == 1));
        Assert.Equal(1, firewood.Single().CategoryId);
        Assert.Equal(2, tires.Single().CategoryId);
        Assert.All(new[] { firewood.Single(), tires.Single() }, m => Assert.Equal("Updated", m.Kind));
    }

    [Fact]
    public async Task DeletingAProduct_SendsDeleted_WithTheNamesItHadBeforeDeletion()
    {
        var inbox = await ConnectCatalogueAsync(3);

        Assert.Equal(HttpStatusCode.NoContent, (await Client.DeleteAsync("/api/products/5")).StatusCode);

        Assert.True(await EventuallyAsync(() => inbox.Count == 1));
        Assert.Equal(new CatalogueChangedDto("Product", "Deleted", 3, 5, "Цемент М500, 25 кг", "Cement M500, 25 kg"), inbox.Single());
    }

    [Fact]
    public async Task FailedRequests_SendNothing()
    {
        var inbox = await ConnectCatalogueAsync(1, 2, 3);

        await (await Client.PutAsJsonAsync("/api/products/999", new { categoryId = 1, nameUk = "X", nameEn = "Y" })).AssertProblemAsync(HttpStatusCode.NotFound);
        await (await Client.DeleteAsync("/api/products/999")).AssertProblemAsync(HttpStatusCode.NotFound);
        await (await Client.DeleteAsync("/api/categories/999")).AssertProblemAsync(HttpStatusCode.NotFound);
        await (await Client.PostAsJsonAsync("/api/products", new { })).AssertProblemAsync(HttpStatusCode.BadRequest);

        await Task.Delay(400);
        Assert.Empty(inbox);
    }

    [Fact]
    public async Task RenamingACategory_SendsCategoryUpdated_AndRepeatingItSendsNothing()
    {
        var inbox = await ConnectCatalogueAsync(1);
        var body = new { nameUk = "Деревина", nameEn = "Timber", descriptionUk = "Опис", descriptionEn = "Description" };

        Assert.Equal(HttpStatusCode.NoContent, (await Client.PutAsJsonAsync("/api/categories/1", body)).StatusCode);
        Assert.True(await EventuallyAsync(() => inbox.Count == 1));
        Assert.Equal(new CatalogueChangedDto("Category", "Updated", 1, null, "Деревина", "Timber"), inbox.Single());

        Assert.Equal(HttpStatusCode.NoContent, (await Client.PutAsJsonAsync("/api/categories/1", body)).StatusCode);

        await Task.Delay(400);
        Assert.Single(inbox);
    }

    [Fact]
    public async Task DeletingACategory_SendsCategoryDeleted_WithItsNames()
    {
        var created = await Client.PostAsJsonAsync("/api/categories", new { nameUk = "Інструмент", nameEn = "Tools", descriptionUk = "Опис", descriptionEn = "Description" });
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var id = (await created.Content.ReadFromJsonAsync<IdDto>())!.Id;
        var inbox = await ConnectCatalogueAsync(id);

        Assert.Equal(HttpStatusCode.NoContent, (await Client.DeleteAsync($"/api/categories/{id}")).StatusCode);

        Assert.True(await EventuallyAsync(() => inbox.Count == 1));
        Assert.Equal(new CatalogueChangedDto("Category", "Deleted", id, null, "Інструмент", "Tools"), inbox.Single());
    }

    [Fact]
    public async Task CatalogueBroadcast_RunsInItsOwnSpan_WithTags()
    {
        var spans = new ConcurrentBag<Activity>();
        using var listener = new ActivityListener
        {
            ShouldListenTo = source => source.Name == Telemetry.SourceName,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded,
            ActivityStopped = spans.Add
        };
        ActivitySource.AddActivityListener(listener);

        var renamed = await Client.PutAsJsonAsync("/api/products/1", new { categoryId = 1, nameUk = "Дуб колотий", nameEn = "Split oak" });
        Assert.Equal(HttpStatusCode.NoContent, renamed.StatusCode);

        var span = Assert.Single(spans);
        Assert.Equal("BroadcastCatalogueChange", span.OperationName);
        Assert.Equal("Product", span.GetTagItem("catalogue.entity"));
        Assert.Equal("Updated", span.GetTagItem("catalogue.kind"));
        Assert.Equal("1", span.GetTagItem("category.ids"));
    }
}
