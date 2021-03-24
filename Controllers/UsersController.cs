using System.Security.Claims;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using API.Data;
using API.DTOs;
using API.Entities;
using API.Interfaces;
using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using API.Extensions;
using API.Helpers;

namespace API.Controllers
{
    [Authorize]
    public class UsersController : BaseApiController
    {
        private readonly IUserRepository _userRepository;
        private readonly IMapper _mapper;
        private readonly IPhotoService _photoService;

        public UsersController(IUserRepository userRepository, IMapper mapper, IPhotoService photoService)
        {
            _photoService = photoService;
            _mapper = mapper;
            _userRepository = userRepository;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<MemberDTO>>> GetUsers([FromQuery] UserParams userParams)
        {
            var user = await _userRepository.GetUserByUsernameAsync( User.GetUsername() );
            userParams.CurrentUsername = user.Username;

            if ( string.IsNullOrEmpty( userParams.Gender ) ) userParams.Gender = user.Gender == "male" ? "female" : "male";

            var users = await _userRepository.GetMembersAsync( userParams );

            Response.AddPaginationHeader( users.CurrentPage, users.PageSize, users.TotalCount, users.TotalPages );

            return Ok(users);
        }

        [HttpGet("{username}", Name = nameof(GetUser))]
        public async Task<ActionResult<MemberDTO>> GetUser(string username)
        {
            return await _userRepository.GetMemberAsync(username);
        }

        [HttpPut]
        public async Task<ActionResult> UpdateUser(MemberUpdateDTO memberUpdateDTO)
        {
            var username = User.GetUsername();

            var user = await _userRepository.GetUserByUsernameAsync(username);

            _mapper.Map(memberUpdateDTO, user);

            _userRepository.Upddate(user);

            if (await _userRepository.SaveAllAsync()) return NoContent();

            return BadRequest("Failed to update user");
        }

        [HttpPost("add-photo")]
        public async Task<ActionResult<PhotoDTO>> AddPhoto(IFormFile file)
        {
            var username = User.GetUsername();

            var user = await _userRepository.GetUserByUsernameAsync(username);

            var uploadResult = await _photoService.AddPhotoAsync(file);

            if (uploadResult.Error != null) return BadRequest(uploadResult.Error.Message);

            var photo = new Photo
            {
                Url = uploadResult.SecureUrl.AbsoluteUri,
                PublicId = uploadResult.PublicId
            };

            if ( user.Photos.Count == 0 )
            {
                photo.IsMain = true;
            }

            user.Photos.Add( photo );

            if ( await _userRepository.SaveAllAsync() )
            {
                return CreatedAtRoute("GetUser", new { username = user.Username }, _mapper.Map<PhotoDTO>(photo));
            }

            return BadRequest("Problem adding photo");
        }

        [HttpPut("set-main-photo/{photoId}")]
        public async Task<ActionResult> SetMainPhoto(int photoId)
        {   
            var username = User.GetUsername();

            var user = await _userRepository.GetUserByUsernameAsync(username);

            var photo = user.Photos.FirstOrDefault(x => x.Id == photoId);

            if ( photo.IsMain ) return BadRequest("This is already your main photo");

            var currentMainPhoto = user.Photos.FirstOrDefault(x => x.IsMain);

            if ( currentMainPhoto != null ) currentMainPhoto.IsMain = false;

            photo.IsMain = true;

            if ( await _userRepository.SaveAllAsync() ) return NoContent();

            return BadRequest("Failed to set main photo");
        }

        [HttpDelete("delete-photo/{photoId}")]
        public async Task<ActionResult> DeletePhoto(int photoId)
        {
            var username = User.GetUsername();

            var user = await _userRepository.GetUserByUsernameAsync(username);

            var photo = user.Photos.FirstOrDefault(x => x.Id == photoId);

            if ( photo == null ) return NotFound();

            if ( photo.IsMain ) return BadRequest("You cannot delete your main photo");

            if ( photo.PublicId != null ) // If exists in Cloudinary
            {
                var deletionResult = await _photoService.DeletePhotoAsync( photo.PublicId );

                if ( deletionResult.Error != null ) return BadRequest( deletionResult.Error.Message );
            }

            user.Photos.Remove( photo );

            if ( await _userRepository.SaveAllAsync() ) return Ok();

            return BadRequest("Failed to delete the photo");
        }
    }
}