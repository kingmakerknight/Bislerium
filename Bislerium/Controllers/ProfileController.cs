using System.Net;
using Bislerium.Application.DTOs.Base;
using Bislerium.Application.DTOs.Email;
using Bislerium.Application.DTOs.Profile;
using Bislerium.Application.Interfaces.Repositories.Base;
using Bislerium.Application.Interfaces.Services;
using Bislerium.Entities.Constants;
using Bislerium.Entities.Models;
using Bislerium.Entities.Utilities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Bislerium.Controllers;

[Authorize]
[ApiController]
[Route("api/profile")]
public class ProfileController : Controller
{
    private readonly IUserService _userService;
    private readonly IEmailService _emailService;
    private readonly IGenericRepository _genericRepository;

    public ProfileController(IEmailService emailService, IGenericRepository genericRepository, IUserService userService)
    {
        _emailService = emailService;
        _genericRepository = genericRepository;
        _userService = userService;
    }

    [HttpGet("profile-details")]
    public IActionResult GetProfileDetails()
    {
        var userId = _userService.UserId;

        var user = _genericRepository.GetById<User>(userId);

        var role = _genericRepository.GetById<Role>(user.RoleId);

        var result = new ProfileDetailsDto()
        {
            UserId = user.Id,
            FullName = user.FullName,
            Username = user.UserName,
            EmailAddress = user.EmailAddress,
            RoleId = role.Id,
            RoleName = role.Name,
            ImageURL = user.ImageURL ?? "sample-profile.png",
            MobileNumber = user.MobileNo ?? ""
        };

        return Ok(new ResponseDto<ProfileDetailsDto>()
        {
            Message = "Successfully Fetched",
            StatusCode = HttpStatusCode.OK,
            TotalCount = 1,
            Status = "Success",
            Result = result
        });
    }

    [HttpPatch("update-profile-details")]
    public IActionResult UpdateProfileDetails(ProfileDetailsDto profileDetails)
    {
        var user = _genericRepository.GetById<User>(profileDetails.UserId);

        user.FullName = profileDetails.FullName;
        user.MobileNo = profileDetails.MobileNumber;
        
        _genericRepository.Update(user);
        
        return Ok(new ResponseDto<object>()
        {
            Message = "Successfully Updated",
            StatusCode = HttpStatusCode.OK,
            TotalCount = 1,
            Status = "Success",
            Result = true
        });
    }

    [HttpDelete("delete-profile")]
    public IActionResult DeleteProfile()
    {
        var userId = _userService.UserId;

        var user = _genericRepository.GetById<User>(userId);

        var blogs = _genericRepository.Get<Blog>(x => x.CreatedBy == user.Id);

        var blogImages = _genericRepository.Get<BlogImage>(x => blogs.Select(z => z.Id).Contains(x.BlogId));

        var comments = _genericRepository.Get<Comment>(x => x.CreatedBy == user.Id);

        var reactions = _genericRepository.Get<Reaction>(x => x.CreatedBy == user.Id);

        _genericRepository.RemoveMultipleEntity(reactions);
        
        _genericRepository.RemoveMultipleEntity(comments);

        _genericRepository.RemoveMultipleEntity(blogImages);
        
        _genericRepository.RemoveMultipleEntity(blogs);
        
        _genericRepository.Delete(user);
        
        return Ok(new ResponseDto<object>()
        {
            Message = "Successfully Deleted",
            StatusCode = HttpStatusCode.OK,
            TotalCount = 1,
            Status = "Success",
            Result = true
        });
    }

    [HttpPost("change-password")]
    public IActionResult ChangePassword(ChangePasswordDto changePassword)
    {
        var userId = _userService.UserId;

        var user = _genericRepository.GetById<User>(userId);

        var isValid = Password.VerifyHash(changePassword.CurrentPassword, user.Password);

        if (isValid)
        {
            user.Password = Password.HashSecret(changePassword.NewPassword);
            
            _genericRepository.Update(user);
            
            return Ok(new ResponseDto<object>()
            {
                Message = "Successfully Updated",
                StatusCode = HttpStatusCode.OK,
                TotalCount = 1,
                Status = "Success",
                Result = true
            });
        }
        
        return BadRequest(new ResponseDto<object>()
        {
            Message = "Password not valid",
            StatusCode = HttpStatusCode.BadRequest,
            TotalCount = 1,
            Status = "Invalid",
            Result = false
        });
    }

    [HttpPost("reset-password")]
    public IActionResult ResetPassword(string emailAddress)
    {
        var user = _genericRepository.GetFirstOrDefault<User>(x => x.EmailAddress == emailAddress);

        if (user == null)
        {
            return BadRequest(new ResponseDto<object>()
            {
                Message = "User not found",
                StatusCode = HttpStatusCode.BadRequest,
                TotalCount = 1,
                Status = "Invalid",
                Result = false
            });
        }

        const string newPassword = Constants.Passwords.BloggerPassword;

        var message =
            $"Dear {user.FullName}, <br><br> " +
            $"We have received a request to reset your password and it has been reset successfully. " +
            $"Your new allocated password is {newPassword}.<br><br>" +
            $"Regards,<br>" +
            $"Bislerium.";

        var email = new EmailDto()
        {
            Email = user.EmailAddress,
            Message = message,
            Subject = "Reset Password - Bislerium"
        };
        
        _emailService.SendEmail(email);
        
        return Ok(new ResponseDto<object>()
        {
            Message = "Successfully Updated",
            StatusCode = HttpStatusCode.OK,
            TotalCount = 1,
            Status = "Success",
            Result = true
        });
    }
}