using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using API.DTOs;
using API.Entities;
using API.Extensions;
using API.Helpers;
using API.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace API.Data
{
    public class LikesRepository : ILikesRepository
    {
        private readonly DataContext _context;
        public LikesRepository( DataContext context )
        {
            _context = context;
        }

        public async Task<UserLike> GetUserLike( int sourceUserId, int likedUserId )
        {
            return await _context.Likes.FindAsync( sourceUserId, likedUserId );
        }

        public async Task<PagedList<LikeDTO>> GetUserLikes( LikesParams likesParams )
        {
            var users = _context.Users
                .OrderBy( u => u.Username )
                .AsQueryable();
            var likes = _context.Likes.AsQueryable();

            if ( likesParams.Predicate == "liked" )
            {
                likes = likes.Where( l => l.SourceUserId == likesParams.UserId );
                users = likes.Select( l => l.LikedUser );
            }

            if ( likesParams.Predicate == "likedBy" )
            {
                likes = likes.Where( l => l.LikeUserId == likesParams.UserId );
                users = likes.Select( l => l.SourceUser );
            }

            var likedUsers = users.Select( u => new LikeDTO
            {
                Username = u.Username,
                KnownAs = u.KnownAs,
                Age = u.DateOfBirth.CalculateAge(),
                PhotoUrl = u.Photos.FirstOrDefault( p => p.IsMain ).Url,
                City = u.City,
                Id = u.Id
            });

            return await PagedList<LikeDTO>.CreateAsync( likedUsers, likesParams.PageNumber, likesParams.PageSize );
        }

        public async Task<AppUser> GetUserWithLikes( int userId )
        {
            return await _context.Users
                .Include( x => x.LikedUsers )
                .FirstOrDefaultAsync( x => x.Id == userId );
        }
    }
}