using System;
using System.Threading.Tasks;
using MassTransit;
using Play.Common;
using Play.Inventory.Contracts;
using Play.Inventory.Service.Entities;
using Play.Inventory.Service.Exceptions;

namespace Play.Inventory.Service.Consumers
{

    public class SubstractItemsConsumer : IConsumer<GrantItems>
    {
        private readonly IRepository<InventoryItem> _itemRepository;
        private readonly IRepository<CatalogItem> _catalogRepository;

        public SubstractItemsConsumer(
            IRepository<InventoryItem> itemRepository,
            IRepository<CatalogItem> catalogRepository
        )
        {
            _itemRepository = itemRepository;
            _catalogRepository = catalogRepository;
        }

        public async Task Consume(ConsumeContext<GrantItems> context)
        {
            var message = context.Message;
            var item = await _catalogRepository.GetAsync(message.CatalogItemId);
            if(item == null)
                throw new UnknownItemException(message.CatalogItemId);

            var inventoryItem = await _itemRepository.GetAsync (x => x.UserId == message.UserId && x.CatalogItemId == message.CatalogItemId);
            if (inventoryItem != null) 
            {
                inventoryItem.Quantity -= message.Quantity;
                await _itemRepository.UpdateAsync (inventoryItem);
            }
            await context.Publish(new InventoryItemsSubstracted(message.CorrelationId));
        }
    }
}