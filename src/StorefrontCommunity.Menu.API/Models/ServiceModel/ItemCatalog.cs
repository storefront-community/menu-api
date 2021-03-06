using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using StorefrontCommunity.Menu.API.Models.DataModel;
using StorefrontCommunity.Menu.API.Models.DataModel.Items;
using StorefrontCommunity.Menu.API.Models.EventModel.Published.Items;
using StorefrontCommunity.Menu.API.Models.IntegrationModel.EventBus;

namespace StorefrontCommunity.Menu.API.Models.ServiceModel
{
    public sealed class ItemCatalog
    {
        private readonly ApiDbContext _dbContext;
        private readonly IMessageBroker _messageBroker;

        public ItemCatalog(ApiDbContext dbContext, IMessageBroker messageBroker)
        {
            _dbContext = dbContext;
            _messageBroker = messageBroker;
        }

        public Item Item { get; private set; }
        public bool GroupNotExists { get; private set; }
        public bool ItemNotExists { get; private set; }

        public async Task Add(Item item)
        {
            Item = item;

            await CheckIfGroupExists();

            if (GroupNotExists) return;

            _dbContext.Add(Item);

            await _dbContext.SaveChangesAsync();

            _messageBroker.Publish(new ItemCreatedEvent(Item));
        }

        public async Task Find(long tenantId, long itemId)
        {
            Item = await _dbContext.Items
                .WhereKey(tenantId, itemId)
                .SingleOrDefaultAsync();

            ItemNotExists = Item == null;
        }

        public async Task Update()
        {
            await CheckIfGroupExists();

            if (GroupNotExists) return;

            await _dbContext.SaveChangesAsync();

            _messageBroker.Publish(new ItemUpdatedEvent(Item));
        }

        public async Task Delete()
        {
            _dbContext.Items.Remove(Item);

            await _dbContext.SaveChangesAsync();

            _messageBroker.Publish(new ItemDeletedEvent(Item));
        }

        private async Task CheckIfGroupExists()
        {
            var itemGroupCatalog = new ItemGroupCatalog(_dbContext, _messageBroker);

            await itemGroupCatalog.Find(Item.TenantId, Item.ItemGroupId);

            GroupNotExists = itemGroupCatalog.GroupNotExists;
        }
    }
}
