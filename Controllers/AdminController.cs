using System.Linq;
using System.Threading.Tasks;
using API.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace API.Controllers
{
    public class AdminController : BaseApiController
    {
        private readonly UserManager<AppUser> _userManager;
        public AdminController(UserManager<AppUser> userManager)
        {
            _userManager = userManager;
        }

        [Authorize(Policy = "RequireAdminRole")]
        [HttpGet("users-with-roles")]
        public async Task<ActionResult> GetUsersWithRoles()
        {
            var users = await _userManager.Users
                .Include( x => x.UserRoles )
                    .ThenInclude( x => x.Role)
                .OrderBy( u => u.UserName )
                .Select( u => new 
                {
                    u.Id,
                    Username = u.UserName,
                    Roles = u.UserRoles.Select( x => x.Role.Name ).ToList()
                } )
                .ToListAsync();

            return Ok( users );
        }

        [Authorize(Policy = "RequireAdminRole")]
        [HttpPost("edit-roles/{username}")]
        public async Task<ActionResult> EditRoles( string username, [FromQuery] string roles )
        {
            var selectedRoles = roles.Split( "," ).ToArray();
            
            var user = await _userManager.FindByNameAsync( username );

            if ( user == null ) return NotFound( "Could not find user" );

            var userRoles = await _userManager.GetRolesAsync( user );
            
            var rolesResult = await _userManager.AddToRolesAsync( user, selectedRoles.Except( userRoles ) );

            if ( !rolesResult.Succeeded ) return BadRequest( $"Failed to add to roles. { rolesResult.Errors }" );

            rolesResult = await _userManager.RemoveFromRolesAsync( user, userRoles.Except( selectedRoles ) );

            if ( !rolesResult.Succeeded ) return BadRequest( $"Failed to remove from roles. { rolesResult.Errors }" );

            return Ok( await _userManager.GetRolesAsync( user ) );
        }

        [Authorize(Policy = "ModeratePhotoRole")]
        [HttpGet("photos-to-moderate")]
        public ActionResult GetPhotosForModeration()
        {
            return Ok("Admins or moderators can see this");
        }
    }
}