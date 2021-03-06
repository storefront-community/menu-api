using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using StorefrontCommunity.Menu.API.Models.DataModel.ItemGroups;
using StorefrontCommunity.Menu.API.Models.EventModel.Published.Items;
using StorefrontCommunity.Menu.API.Models.TransferModel;
using StorefrontCommunity.Menu.API.Models.TransferModel.Items;
using StorefrontCommunity.Menu.Tests.Factories.ItemGroups;
using StorefrontCommunity.Menu.Tests.Factories.Items;
using StorefrontCommunity.Menu.Tests.Fakes;
using Xunit;

namespace StorefrontCommunity.Menu.Tests.Functional.Items
{
    public sealed class CreateItemTest
    {
        private readonly FakeApiServer _server;
        private readonly FakeApiToken _token;
        private readonly FakeApiClient _client;

        public CreateItemTest()
        {
            _server = new FakeApiServer();
            _token = new FakeApiToken(_server.JwtOptions);
            _client = new FakeApiClient(_server, _token);
        }

        [Fact]
        public async Task ShouldCreateSuccessfully()
        {
            var itemGroup = new ItemGroup().Of(_token.TenantId);

            _server.Database.ItemGroups.Add(itemGroup);
            await _server.Database.SaveChangesAsync();

            var path = "/items";
            var jsonRequest = new SaveItemJson().Build(groupId: itemGroup.Id);
            var response = await _client.PostJsonAsync(path, jsonRequest);
            var jsonResponse = await _client.ReadJsonAsync<ItemJson>(response);
            var item = await _server.Database.Items.SingleAsync();

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(_token.TenantId, item.TenantId);
            Assert.Equal(jsonRequest.Name, item.Name);
            Assert.Equal(jsonRequest.Description, item.Description);
            Assert.Equal(jsonRequest.Price, item.Price);
            Assert.Equal(jsonRequest.IsAvailable, item.IsAvailable);
            Assert.Equal(item.Id, jsonResponse.Id);
            Assert.Equal(item.Name, jsonResponse.Name);
            Assert.Equal(item.Description, jsonResponse.Description);
            Assert.Equal(item.Price, jsonResponse.Price);
            Assert.Equal(item.IsAvailable, jsonResponse.Available);
        }

        [Fact]
        public async Task ShouldPublishEventAfterCreateSuccessfully()
        {
            var itemGroup = new ItemGroup().Of(_token.TenantId);

            _server.Database.ItemGroups.Add(itemGroup);
            await _server.Database.SaveChangesAsync();

            var path = "/items";
            var jsonRequest = new SaveItemJson().Build(groupId: itemGroup.Id);
            var response = await _client.PostJsonAsync(path, jsonRequest);
            var item = await _server.Database.Items.SingleAsync();
            var publishedEvent = _server.EventBus.PublishedEvents
                .Single(@event => @event.Name == "menu.item.created");
            var payload = (ItemPayload)publishedEvent.Payload;

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(payload.Id, item.Id);
            Assert.Equal(payload.TenantId, item.TenantId);
            Assert.Equal(payload.Name, item.Name);
            Assert.Equal(payload.Description, item.Description);
            Assert.Equal(payload.Price, item.Price);
            Assert.Equal(payload.IsAvailable, item.IsAvailable);
        }

        [Fact]
        public async Task ShouldRespond422ForInexistentGroupId()
        {
            var itemGroup = new ItemGroup().Of(_token.TenantId);

            _server.Database.ItemGroups.Add(itemGroup);
            await _server.Database.SaveChangesAsync();

            var path = "/items";
            var jsonRequest = new SaveItemJson().Build(groupId: 90);
            var response = await _client.PostJsonAsync(path, jsonRequest);
            var jsonResponse = await _client.ReadJsonAsync<UnprocessableEntityError>(response);

            Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
            Assert.Equal("ITEM_GROUP_NOT_FOUND", jsonResponse.Error);
        }
    }
}
