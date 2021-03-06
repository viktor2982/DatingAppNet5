using System.Collections.Generic;
using System.Threading.Tasks;
using API.DTOs;
using API.Entities;
using API.Helpers;

namespace API.Interfaces
{
    public interface IUserRepository
    {
         void Upddate(AppUser user);
         Task<IEnumerable<AppUser>> GetUsersAsync();
         Task<AppUser> GetUserByIdAsync(int id);
         Task<AppUser> GetUserByUsernameAsync(string username, bool includeAllPhotos = false);
         Task<AppUser> GetUserByPhotoId(int photoId);
         Task<PagedList<MemberDTO>> GetMembersAsync(UserParams userParams);
         Task<MemberDTO> GetMemberAsync(string username, bool isCurrentUser);
         Task<string> GetUserGender(string username);
    }
}