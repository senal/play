using System.Threading.Tasks;
using MassTransit;
using Play.Catalog.Contracts;
using Play.Common;
using Play.Inventory.Service.Entities;

namespace Play.Inventory.Service.Consumers {
    public class CatalogItemCreatedConsumer : IConsumer<CatalogItemCreated> {
        private readonly IRepository<CatalogItem> _respository;

        public CatalogItemCreatedConsumer (IRepository<CatalogItem> respository) {
            _respository = respository;
        }

        public async Task Consume (ConsumeContext<CatalogItemCreated> context) {
            var message = context.Message;
            var item = await _respository.GetAsync (x => x.Id == message.ItemId);
            if (item != null) {
                return;
            }
            item = new CatalogItem {
                Id = message.ItemId,
                Name = message.Name,
                Description = message.Description
            };

            await _respository.CreateAsync (item);
        }
    }
}