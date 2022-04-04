using System.Threading.Tasks;
using MassTransit;
using Play.Catalog.Contracts;
using Play.Common;
using Play.Inventory.Service.Entities;

namespace Play.Inventory.Service.Consumers {
    public class CatalogItemDeletedConsumer : IConsumer<CatalogItemDeleted> {
        private readonly IRepository<CatalogItem> _respository;

        public CatalogItemDeletedConsumer (IRepository<CatalogItem> respository) {
            _respository = respository;
        }

        public async Task Consume (ConsumeContext<CatalogItemDeleted> context) {
            var message = context.Message;
            var item = await _respository.GetAsync (x => x.Id == message.id);
            if (item == null) {
                return;
            }

            await _respository.DeleteAsync (item.Id);
        }
    }
}