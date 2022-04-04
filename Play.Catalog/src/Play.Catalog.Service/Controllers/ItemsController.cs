using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MassTransit;
using Microsoft.AspNetCore.Mvc;
using Play.Catalog.Contracts;
using Play.Catalog.Service.Dtos;
using Play.Catalog.Service.Entities;
using Play.Common;

namespace Play.Catalog.Service.Controllers {
    [ApiController]
    [Route ("items")]
    public class ItemsController : ControllerBase {
        private readonly IRepository<Item> _itemsRepository;
        private readonly IPublishEndpoint _publishEndpoint;

        public ItemsController (IRepository<Item> itemRepository, IPublishEndpoint publishEndpoint) {
            _itemsRepository = itemRepository;
            _publishEndpoint = publishEndpoint;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<ItemDto>>> GetAsync ( ) {
            var items = (await _itemsRepository.GetAllAsync ( ))
                .Select (x => x.AsDto ( ));
            return Ok (items);
        }

        [HttpGet ("{id}")]
        public async Task<ActionResult<ItemDto>> GetByIdAsync (Guid id) {
            var item = (await _itemsRepository.GetAsync (id)).AsDto ( );
            if (item == null) {
                return NotFound ( );
            }
            return item;
        }

        [HttpPost]
        public async Task<ActionResult<ItemDto>> PostAsync (CreateItemDto createItemDto) {
            var item = new Item {
                Name = createItemDto.Name,
                Description = createItemDto.Description,
                Price = createItemDto.Price,
                CreatedDate = DateTimeOffset.UtcNow
            };
            await _itemsRepository.CreateAsync (item);
            await _publishEndpoint.Publish (new CatalogItemCreated (item.Id, item.Name, item.Description));
            return CreatedAtAction (nameof (GetByIdAsync), new { id = item.Id }, item);
        }

        // PUT /items/{id}
        [HttpPut ("{id}")]
        public async Task<IActionResult> PutAsync (Guid id, UpdateItemDto updateItemDto) {
            var item = await _itemsRepository.GetAsync (id);
            if (item == null) {
                return NotFound ( );
            }
            item.Name = updateItemDto.Name;
            item.Description = updateItemDto.Description;
            item.Price = updateItemDto.Price;

            await _itemsRepository.UpdateAsync (item);
            await _publishEndpoint.Publish (new CatalogItemUpdated (item.Id, item.Name, item.Description));
            return NoContent ( );
        }

        // DELETE /items/{id}
        [HttpDelete ("{id}")]
        public async Task<IActionResult> Delete (Guid id) {
            var item = await _itemsRepository.GetAsync (id);
            if (item == null) {
                return NotFound ( );
            }

            await _itemsRepository.DeleteAsync (id);
            await _publishEndpoint.Publish (new CatalogItemDeleted (id));
            return NoContent ( );
        }
    }

}