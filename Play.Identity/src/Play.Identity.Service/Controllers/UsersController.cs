using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Play.Identity.Service.Dtos;
using Play.Identity.Service.Entities;

namespace Play.Identity.Service.Controllers {

    [ApiController]
    [Route ("users")]
    public class UsersController : ControllerBase {

        private readonly UserManager<ApplicationUser> _userManager;
        public UsersController (UserManager<ApplicationUser> userManager) {
            _userManager = userManager;
        }

        [HttpGet]
        public ActionResult<IEnumerable<UserDto>> Get ( ) {
            var users = _userManager.Users
                .ToList ( )
                .Select (x => x.AsDto ( ))
                .AsEnumerable ( );
            return Ok (users);
        }

        // /users/{1}
        [HttpGet ("{id}")]
        public async Task<ActionResult<UserDto>> GetByIdAsync (Guid id) {
            var user = await _userManager.FindByIdAsync (id.ToString ( ));
            if (user == null) {
                return NotFound ( );
            }
            return Ok (user.AsDto ( ));
        }

        // /users/{1}
        [HttpPut ("{id}")]
        public async Task<ActionResult<UserDto>> PutAsync (Guid id, UpdateUserDto userDto) {
            var user = await _userManager.FindByIdAsync (id.ToString ( ));
            if (user == null) {
                return NotFound ( );
            }
            user.Email = userDto.Email;
            user.UserName = userDto.Email;
            user.Gil = userDto.Gil;
            await _userManager.UpdateAsync (user);
            return Ok (user.AsDto ( ));
        }

        // /users/{1}
        [HttpDelete ("{id}")]
        public async Task<ActionResult> DeleteAsync (Guid id) {
            var user = await _userManager.FindByIdAsync (id.ToString ( ));
            if (user == null) {
                return NotFound ( );
            }
            await _userManager.DeleteAsync (user);
            return Ok ( );
        }

    }
}