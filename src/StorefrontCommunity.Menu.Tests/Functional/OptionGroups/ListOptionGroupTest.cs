using System.Net;
using System.Threading.Tasks;
using StorefrontCommunity.Menu.API.Models.DataModel.ItemGroups;
using StorefrontCommunity.Menu.API.Models.DataModel.OptionGroups;
using StorefrontCommunity.Menu.API.Models.TransferModel.OptionGroups;
using StorefrontCommunity.Menu.Tests.Factories.ItemGroups;
using StorefrontCommunity.Menu.Tests.Factories.OptionGroups;
using StorefrontCommunity.Menu.Tests.Fakes;
using Xunit;

namespace StorefrontCommunity.Menu.Tests.Functional.OptionGroups
{
    public sealed class ListOptionGroupTest
    {
        private readonly FakeApiServer _server;
        private readonly FakeApiToken _token;
        private readonly FakeApiClient _client;

        public ListOptionGroupTest()
        {
            _server = new FakeApiServer();
            _token = new FakeApiToken(_server.JwtOptions);
            _client = new FakeApiClient(_server, _token);
        }

        [Fact]
        public async Task ShouldListAll()
        {
            var itemGroup = new ItemGroup().Of(_token.TenantId);
            var optionGroup1 = new OptionGroup().To(itemGroup);
            var optionGroup2 = new OptionGroup().To(itemGroup);

            _server.Database.ItemGroups.Add(itemGroup);
            _server.Database.OptionGroups.AddRange(optionGroup1, optionGroup2);
            await _server.Database.SaveChangesAsync();

            var path = "/option-groups";
            var response = await _client.GetAsync(path);
            var jsonResponse = await _client.ReadJsonAsync<OptionGroupListJson>(response);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(2, jsonResponse.Count);
            Assert.Contains(jsonResponse.OptionGroups, json => json.Id == optionGroup1.Id);
            Assert.Contains(jsonResponse.OptionGroups, json => json.Id == optionGroup2.Id);
        }

        [Fact]
        public async Task ShouldListByTitle()
        {
            var itemGroup = new ItemGroup().Of(_token.TenantId);
            var optionGroup1 = new OptionGroup().To(itemGroup);
            var optionGroup2 = new OptionGroup().To(itemGroup);

            _server.Database.ItemGroups.Add(itemGroup);
            _server.Database.OptionGroups.AddRange(optionGroup1, optionGroup2);
            await _server.Database.SaveChangesAsync();

            var path = $"/option-groups?title={optionGroup1.Title}";
            var response = await _client.GetAsync(path);
            var jsonResponse = await _client.ReadJsonAsync<OptionGroupListJson>(response);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(1, jsonResponse.Count);
            Assert.Contains(jsonResponse.OptionGroups, json => json.Id == optionGroup1.Id);
            Assert.DoesNotContain(jsonResponse.OptionGroups, json => json.Id == optionGroup2.Id);
        }
    }
}
