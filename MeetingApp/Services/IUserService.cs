using MeetingApp.Models.Dtos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MeetingApp.Services
{
    public interface IUserService
    {
        Task<List<UserDto>> GetMyUsersAsync();
        Task<bool> AddUserToAdminAsync(int userId);
        Task<bool> RemoveUserFromAdminAsync(int userId);
    }
}
