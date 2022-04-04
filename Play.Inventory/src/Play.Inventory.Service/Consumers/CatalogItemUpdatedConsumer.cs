using System.Threading.Tasks;
using MassTransit;
using Play.Catalog.Contracts;
using Play.Common;
using Play.Inventory.Service.Entities;

namespace Play.Inventory.Service.Consumers {
    public class CatalogItemUpdatedConsumer : IConsumer<CatalogItemUpdated> {
        private readonly IRepository<CatalogItem> _respository;

        public CatalogItemUpdatedConsumer (IRepository<CatalogItem> respository) {
            _respository = respository;
        }

        public async Task Consume (ConsumeContext<CatalogItemUpdated> context) {
            var message = context.Message;
            var item = await _respository.GetAsync (x => x.Id == message.ItemId);
            if (item == null) {

                item = new CatalogItem {
                Id = message.ItemId,
                Name = message.Name,
                Description = message.Description
                };

                await _respository.CreateAsync (item);
            } else {

                item.Name = message.Name;
                item.Description = message.Description;

                await _respository.UpdateAsync (item);
            }

        }
    }
}