using System.Net;
using Bislerium.Application.DTOs.Account;
using Bislerium.Application.DTOs.Base;
using Bislerium.Application.DTOs.Dashboard;
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
    
    [HttpGet("dashboard-details")]
    public IActionResult GetDashboardDetails()
    {
        var blogs = _genericRepository.Get<Blog>(x => x.IsActive);
      
        var reactions = _genericRepository.Get<Reaction>(x => x.IsActive);

        var comments = _genericRepository.Get<Comment>(x => x.IsActive);

        var blogDetails = blogs as Blog[] ?? blogs.ToArray();
          
        var reactionDetails = reactions as Reaction[] ?? reactions.ToArray();

        var commentDetails = comments as Comment[] ?? comments.ToArray();

        var dashboardDetails = new DashboardCount()
        {
            Posts = blogDetails.Length,
            Comments = commentDetails.Length,
            UpVotes = reactionDetails.Count(x => x.ReactionId == 1),
            DownVotes = reactionDetails.Count(x => x.ReactionId == 2),
        };

        var blogDetailsList = new List<BlogDetails>();
      
        foreach (var blog in blogDetails)
        {
            var upVotes = reactionDetails.Where(x => x.ReactionId == 1 && x.BlogId == blog.Id && x.IsReactedForBlog);
         
            var downVotes = reactionDetails.Where(x => x.ReactionId == 2 && x.BlogId == blog.Id && x.IsReactedForBlog);
         
            var commentReactions = commentDetails.Where(x => x.BlogId == blog.Id && x.IsCommentForBlog);

            var commentForComments =
                commentDetails.Where(x => 
                    commentReactions.Select(z => 
                        z.CommentId).Contains(x.CommentId) && x.IsCommentForComment);

            var popularity = upVotes.Count() * 2 - 
                                downVotes.Count() * 1 + 
                                commentReactions.Count() + commentForComments.Count();
         
            blogDetailsList.Add(new BlogDetails()
            {
                BlogId = blog.Id,
                Blog = blog.Title,
                BloggerId = blog.CreatedBy,
                Popularity = popularity
            });
        }
      
        var bloggerDetailsList = blogDetailsList
            .GroupBy(blog => blog.BloggerId)
            .Select(group => new BloggerDetails
            {
                BloggerId = group.Key,
                BloggerName = _genericRepository.GetById<User>(group.Key).FullName,
                Popularity = group.Sum(blog => blog.Popularity)
            }).ToList();

        var popularBlogs = blogDetailsList
            .OrderByDescending(x => x.Popularity)
            .Take(10).Select(z => new PopularBlog() 
            {
                BlogId = z.BlogId,
                Blog = z.Blog
            }).ToList();

        var popularBloggers = bloggerDetailsList
            .OrderByDescending(x => x.Popularity)
            .Take(10).Select(z => new PopularBlogger() 
            {
                BloggerId = z.BloggerId,
                BloggerName = z.BloggerName
            }).ToList();

        var dashboardCounts = new DashboardDetailsDto()
        {
            DashboardCount = dashboardDetails,
            PopularBloggers = popularBloggers,
            PopularBlogs = popularBlogs
        };

        var result = new ResponseDto<DashboardDetailsDto>()
        {
            Message = "Success",
            StatusCode = HttpStatusCode.OK,
            TotalCount = 1,
            Result = dashboardCounts,
            Status = "Success"
        };

        return Ok(result);
   }
}