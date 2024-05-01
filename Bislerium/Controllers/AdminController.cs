using System.Net;
using Bislerium.Application.DTOs.Account;
using Bislerium.Application.DTOs.Base;
using Bislerium.Application.DTOs.User;
using Bislerium.Application.Interfaces.Repositories.Base;
using Bislerium.Entities.Constants;
using Bislerium.Entities.Models;
using Bislerium.Entities.Utilities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Bislerium.Controllers;

[Authorize]
[ApiController]
[Route("api/admin")]
public class AdminController : Controller
{
    private readonly IGenericRepository _genericRepository;

    public AdminController(IGenericRepository genericRepository)
    {
        _genericRepository = genericRepository;
    }

    [HttpGet("get-all-users")]
    public IActionResult GetAllUsers()
    {
        var users = _genericRepository.Get<User>();

        var result = users.Select(x => new UserDetailDto()
        {
            Id = x.Id,
            RoleId = x.RoleId,
            EmailAddress = x.EmailAddress,
            ImageURL = x.ImageURL ?? "sample-profile.png",
            Username = x.UserName,
            Name = x.FullName,
            RoleName = _genericRepository.GetById<Role>(x.RoleId).Name
        }).ToList();
        
        return Ok(new ResponseDto<List<UserDetailDto>>()
        {
            Message = "Successfully Retrieved",
            Result = result,
            Status = "Success",
            StatusCode = HttpStatusCode.OK,
            TotalCount = 1
        });
    }
    
    [HttpPost("register-admin")]
    public IActionResult RegisterAdministrator(RegisterDto register)
    {
        var existingUser = _genericRepository.GetFirstOrDefault<User>(x =>
            x.EmailAddress == register.EmailAddress || x.UserName == register.Username);

        if (existingUser == null)
        {
            var role = _genericRepository.GetFirstOrDefault<Role>(x => x.Name == Constants.Roles.Admin);

            var appUser = new User
            {
                FullName = register.FullName,
                EmailAddress = register.EmailAddress,
                RoleId = role!.Id,
                Password = Password.HashSecret(Constants.Passwords.AdminPassword),
                UserName = register.Username,
                MobileNo = register.MobileNumber,
                ImageURL = register.ImageURL
            };

            _genericRepository.Insert(appUser);
         
            return Ok(new ResponseDto<object>()
            {
                Message = "Successfully registered",
                Result = true,
                Status = "Success",
                StatusCode = HttpStatusCode.OK,
                TotalCount = 1
            });
        }
      
        return BadRequest(new ResponseDto<bool>()
        {
            Message = "Existing user with the same user name or email address",
            Result = false,
            Status = "Bad Request",
            StatusCode = HttpStatusCode.BadRequest,
            TotalCount = 0
        });
    }
}