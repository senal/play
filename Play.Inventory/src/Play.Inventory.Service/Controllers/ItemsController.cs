using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Play.Common;
using Play.Inventory.Service.Clients;
using Play.Inventory.Service.Dtos;
using Play.Inventory.Service.Entities;

namespace Play.Inventory.Service.Controllers {
    [ApiController]
    [Route ("items")]
    [Authorize]
    public class ItemsController : ControllerBase {
        private readonly IRepository<InventoryItem> _itemRepository;
        private readonly IRepository<CatalogItem> _catalogRepository;

        public ItemsController (IRepository<InventoryItem> itemRepository, IRepository<CatalogItem> catalogRepository) {
            _itemRepository = itemRepository;
            _catalogRepository = catalogRepository;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<InventoryItemDto>>> GetAsync (Guid userId) {
            if (userId == Guid.Empty) {
                return BadRequest ( );
            }
            var inventoryItemEntities = await _itemRepository.GetAllAsync (x => x.UserId == userId);
            var catalogItemIds = inventoryItemEntities.Select (x => x.CatalogItemId);
            var catalogItemEntities = await _catalogRepository.GetAllAsync (x => catalogItemIds.Contains (x.Id));

            var inventoryItemDtos = inventoryItemEntities.Select (x => {
                var catalogItem = catalogItemEntities.Single (catalogItem => catalogItem.Id == x.CatalogItemId);
                return x.AsDto (catalogItem.Name, catalogItem.Description);
            });

            return Ok (inventoryItemDtos);
        }

        [HttpPost]
        public async Task<ActionResult> PostAsync (GrantItemsDto grantItemDto) {
            var inventoryItem = await _itemRepository.GetAsync (x => x.UserId == grantItemDto.UserId && x.CatalogItemId == grantItemDto.CatalogItemId);
            if (inventoryItem == null) {
                inventoryItem = new InventoryItem ( ) {
                CatalogItemId = grantItemDto.CatalogItemId,
                UserId = grantItemDto.UserId,
                Quantity = grantItemDto.Quantity,
                AcquiredDate = DateTimeOffset.UtcNow
                };

                await _itemRepository.CreateAsync (inventoryItem);
            } else {
                inventoryItem.Quantity += grantItemDto.Quantity;
                await _itemRepository.UpdateAsync (inventoryItem);
            }

            return Ok ( );
        }

    }
}