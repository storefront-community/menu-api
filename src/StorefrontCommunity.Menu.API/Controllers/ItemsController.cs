using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StorefrontCommunity.Menu.API.Authorization;
using StorefrontCommunity.Menu.API.Models.DataModel;
using StorefrontCommunity.Menu.API.Models.DataModel.Items;
using StorefrontCommunity.Menu.API.Models.IntegrationModel.EventBus;
using StorefrontCommunity.Menu.API.Models.ServiceModel;
using StorefrontCommunity.Menu.API.Models.TransferModel;
using StorefrontCommunity.Menu.API.Models.TransferModel.ItemGroups;
using StorefrontCommunity.Menu.API.Models.TransferModel.Items;

namespace StorefrontCommunity.Menu.API.Controllers
{
    /// <summary>
    /// Items that belongs to an item group.
    /// </summary>
    [Route("items"), Authorize]
    [ApiExplorerSettings(GroupName = "Items")]
    [Consumes("application/json")]
    [Produces("application/json")]
    public sealed class ItemsController : Controller
    {
        private readonly ApiDbContext _dbContext;
        private readonly IMessageBroker _messageBroker;

        public ItemsController(ApiDbContext dbContext, IMessageBroker messageBroker)
        {
            _dbContext = dbContext;
            _messageBroker = messageBroker;
        }

        /// <summary>
        /// Find an item by ID.
        /// </summary>
        /// <param name="id">Item ID.</param>
        /// <returns>Returns item data.</returns>
        /// <response code="200">Item data</response>
        /// <response code="422">Errors: ITEM_NOT_FOUND</response>
        [HttpGet, Route("{id:long}")]
        [ProducesResponseType(statusCode: 200, type: typeof(ItemJson))]
        [ProducesResponseType(statusCode: 422, type: typeof(UnprocessableEntityError))]
        public async Task<IActionResult> Find([FromRoute] long id)
        {
            var catalog = new ItemCatalog(_dbContext, _messageBroker);
            var tenantId = User.Claims.TenantId();

            await catalog.Find(tenantId, id);

            if (catalog.ItemNotExists)
            {
                return new ItemNotFoundError();
            }

            return new ItemJson(catalog.Item);
        }

        /// <summary>
        /// List items ordered by name. Search by: name, availability.
        /// </summary>
        /// <param name="query">URL query sring parameters.</param>
        /// <returns>List of items</returns>
        /// <response code="200">Search result</response>
        [HttpGet, Route("")]
        [ProducesResponseType(statusCode: 200, type: typeof(ItemListJson))]
        public async Task<IActionResult> List([FromQuery] ItemListQuery query)
        {
            var itemQuery = _dbContext.Items
                .WhereTenantId(User.Claims.TenantId())
                .WhereNameContains(query.Name)
                .WhereAvailability(query.Available);

            var items = await itemQuery
                .OrderByName()
                .Skip(query.Index.Value)
                .Take(query.Length.Value)
                .ToListAsync();

            var count = await itemQuery.CountAsync();

            return new ItemListJson(items, count);
        }

        /// <summary>
        /// Create an item.
        /// </summary>
        /// <param name="json">Item data.</param>
        /// <returns>Created item.</returns>
        /// <response code="200">Item data</response>
        [HttpPost, Route("")]
        [ProducesResponseType(statusCode: 200, type: typeof(ItemListJson))]
        public async Task<IActionResult> Create([FromBody] SaveItemJson json)
        {
            var catalog = new ItemCatalog(_dbContext, _messageBroker);

            var item = json.MapTo(new Item
            {
                TenantId = User.Claims.TenantId()
            });

            await catalog.Add(item);

            if (catalog.GroupNotExists)
            {
                return new ItemGroupNotFoundError();
            }

            return new ItemJson(catalog.Item);
        }

        /// <summary>
        /// Update an item.
        /// </summary>
        /// <param name="id">Item ID.</param>
        /// <param name="json">Item data.</param>
        /// <returns>Updated item</returns>
        /// <response code="200">Item data</response>
        /// <response code="422">Errors: ITEM_NOT_FOUND, ITEM_GROUP_NOT_FOUND</response>
        [HttpPut, Route("{id:long}")]
        [ProducesResponseType(statusCode: 200, type: typeof(ItemJson))]
        [ProducesResponseType(statusCode: 422, type: typeof(UnprocessableEntityError))]
        public async Task<IActionResult> Update([FromRoute] long id,
            [FromBody] SaveItemJson json)
        {
            var catalog = new ItemCatalog(_dbContext, _messageBroker);
            var tenantId = User.Claims.TenantId();

            await catalog.Find(tenantId, id);

            if (catalog.ItemNotExists)
            {
                return new ItemNotFoundError();
            }

            json.MapTo(catalog.Item);

            await catalog.Update();

            if (catalog.GroupNotExists)
            {
                return new ItemGroupNotFoundError();
            }

            return new ItemJson(catalog.Item);
        }

        /// <summary>
        /// Delete an item.
        /// </summary>
        /// <param name="id">Item ID.</param>
        /// <returns>No content</returns>
        /// <response code="200">Item data</response>
        /// <response code="422">Errors: ITEM_NOT_FOUND, ITEM_GROUP_NOT_FOUND</response>
        [HttpDelete, Route("{id:long}")]
        [ProducesResponseType(statusCode: 204)]
        [ProducesResponseType(statusCode: 422, type: typeof(UnprocessableEntityError))]
        public async Task<IActionResult> Delete([FromRoute] long id)
        {
            var catalog = new ItemCatalog(_dbContext, _messageBroker);
            var tenantId = User.Claims.TenantId();

            await catalog.Find(tenantId, id);

            if (catalog.ItemNotExists)
            {
                return new ItemNotFoundError();
            }

            await catalog.Delete();

            return NoContent();
        }
    }
}
