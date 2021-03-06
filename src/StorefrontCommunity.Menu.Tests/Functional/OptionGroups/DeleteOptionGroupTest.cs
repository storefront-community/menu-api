using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using StorefrontCommunity.Menu.API.Models.DataModel.ItemGroups;
using StorefrontCommunity.Menu.API.Models.DataModel.OptionGroups;
using StorefrontCommunity.Menu.API.Models.DataModel.Options;
using StorefrontCommunity.Menu.API.Models.EventModel.Published.OptionGroups;
using StorefrontCommunity.Menu.API.Models.TransferModel;
using StorefrontCommunity.Menu.Tests.Factories.ItemGroups;
using StorefrontCommunity.Menu.Tests.Factories.OptionGroups;
using StorefrontCommunity.Menu.Tests.Factories.Options;
using StorefrontCommunity.Menu.Tests.Fakes;
using Xunit;

namespace StorefrontCommunity.Menu.Tests.Functional.OptionGroups
{
    public sealed class DeleteOptionGroupTest
    {
        private readonly FakeApiServer _server;
        private readonly FakeApiToken _token;
        private readonly FakeApiClient _client;

        public DeleteOptionGroupTest()
        {
            _server = new FakeApiServer();
            _token = new FakeApiToken(_server.JwtOptions);
            _client = new FakeApiClient(_server, _token);
        }

        [Fact]
        public async Task ShouldDeleteSuccessfully()
        {
            var itemGroup = new ItemGroup().Of(_token.TenantId);
            var optionGroup = new OptionGroup().To(itemGroup);

            _server.Database.ItemGroups.Add(itemGroup);
            _server.Database.OptionGroups.Add(optionGroup);
            await _server.Database.SaveChangesAsync();

            var path = $"/option-groups/{optionGroup.Id}";
            var response = await _client.DeleteAsync(path);
            var hasBeenDeleted = !await _server.Database.OptionGroups
                .WhereKey(_token.TenantId, optionGroup.Id)
                .AnyAsync();

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
            Assert.True(hasBeenDeleted);
        }

        [Fact]
        public async Task ShouldPublishEventAfterDeleteSuccessfully()
        {
            var itemGroup = new ItemGroup().Of(_token.TenantId);
            var optionGroup = new OptionGroup().To(itemGroup);

            _server.Database.ItemGroups.Add(itemGroup);
            _server.Database.OptionGroups.Add(optionGroup);
            await _server.Database.SaveChangesAsync();

            var path = $"/option-groups/{optionGroup.Id}";
            var response = await _client.DeleteAsync(path);
            var publishedEvent = _server.EventBus.PublishedEvents
                .Single(@event => @event.Name == "menu.option-group.deleted");
            var payload = (OptionGroupPayload)publishedEvent.Payload;

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
            Assert.Equal(payload.Id, optionGroup.Id);
            Assert.Equal(payload.TenantId, optionGroup.TenantId);
            Assert.Equal(payload.Title, optionGroup.Title);
        }

        [Fact]
        public async Task ShouldRespond422ForInexistentId()
        {
            var itemGroup = new ItemGroup().Of(_token.TenantId);

            _server.Database.ItemGroups.Add(itemGroup);
            await _server.Database.SaveChangesAsync();

            var path = "/option-groups/5";
            var response = await _client.DeleteAsync(path);
            var jsonResponse = await _client.ReadJsonAsync<UnprocessableEntityError>(response);

            Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
            Assert.Equal("OPTION_GROUP_NOT_FOUND", jsonResponse.Error);
        }

        [Fact]
        public async Task ShouldRespond422IfGroupHasOptions()
        {
            var itemGroup = new ItemGroup().Of(_token.TenantId);
            var optionGroup = new OptionGroup().To(itemGroup);
            var option = new Option().To(optionGroup);

            _server.Database.ItemGroups.Add(itemGroup);
            _server.Database.OptionGroups.Add(optionGroup);
            _server.Database.Options.Add(option);

            await _server.Database.SaveChangesAsync();

            var path = $"/option-groups/{optionGroup.Id}";
            var response = await _client.DeleteAsync(path);
            var jsonResponse = await _client.ReadJsonAsync<UnprocessableEntityError>(response);
            var hasBeenDeleted = !await _server.Database.OptionGroups
                .WhereKey(_token.TenantId, optionGroup.Id)
                .AnyAsync();

            Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
            Assert.Equal("OPTION_GROUP_HAS_OPTIONS", jsonResponse.Error);
            Assert.False(hasBeenDeleted);
        }
    }
}
