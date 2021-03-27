using System.Linq;
using System.Text;
using System.Security.Cryptography;
using System.Threading.Tasks;
using API.Data;
using API.Entities;
using Microsoft.AspNetCore.Mvc;
using API.DTOs;
using Microsoft.EntityFrameworkCore;
using API.Interfaces;
using AutoMapper;
using Microsoft.AspNetCore.Identity;

namespace API.Controllers
{
    public class AccountController : BaseApiController
    {
        private readonly ITokenService _tokenService;
        private readonly IMapper _mapper;
        private readonly UserManager<AppUser> _userManager;
        private readonly SignInManager<AppUser> _signInManager;

        public AccountController(UserManager<AppUser> userManager, SignInManager<AppUser> signInManager, ITokenService tokenService, IMapper mapper)
        {
            _signInManager = signInManager;
            _userManager = userManager;
            _mapper = mapper;
            _tokenService = tokenService;
        }

        [HttpPost("register")]
        public async Task<ActionResult<UserDTO>> Register(RegisterDTO registerDTO)
        {
            if (await UserExists( registerDTO.Username )) return BadRequest("Username is taken");

            var user = _mapper.Map<AppUser>( registerDTO );

            user.UserName = registerDTO.Username.ToLower();

            var userResult = await _userManager.CreateAsync( user, registerDTO.Password );

            if ( !userResult.Succeeded ) return BadRequest( userResult.Errors );

            var roleResult = await _userManager.AddToRoleAsync( user, "Member" );

            if ( !roleResult.Succeeded ) return BadRequest( roleResult.Errors );

            return new UserDTO
            {
                Username = user.UserName,
                Token = await _tokenService.CreateToken(user),
                KnownAs = user.KnownAs,
                Gender = user.Gender
            };
        }

        [HttpPost("login")]
        public async Task<ActionResult<UserDTO>> Login(LoginDTO loginDTO)
        {
            var user = await _userManager.Users
                .Include(x => x.Photos)
                .SingleOrDefaultAsync(x => x.UserName == loginDTO.Username.ToLower());

            if (user == null) return Unauthorized( "Invalid username" );

            var result = await _signInManager.CheckPasswordSignInAsync( user, loginDTO.Password, false );

            if ( !result.Succeeded ) return Unauthorized( "Incorrect password" );

            return new UserDTO
            {
                Username = user.UserName,
                Token = await _tokenService.CreateToken(user),
                PhotoUrl = user.Photos.FirstOrDefault(x => x.IsMain)?.Url,
                KnownAs = user.KnownAs,
                Gender = user.Gender
            };
        }

        private async Task<bool> UserExists(string username)
        {
            return await _userManager.Users.AnyAsync(x => x.UserName == username.ToLower());
        }
    }
}