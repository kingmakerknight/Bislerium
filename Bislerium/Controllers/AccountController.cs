using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Security.Claims;
using System.Text;
using Bislerium.Application.DTOs.Account;
using Bislerium.Application.DTOs.Base;
using Bislerium.Application.DTOs.Dashboard;
using Bislerium.Application.Interfaces.Repositories.Base;
using Bislerium.Entities.Constants;
using Bislerium.Entities.Models;
using Bislerium.Entities.Utilities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using JwtRegisteredClaimNames = Microsoft.IdentityModel.JsonWebTokens.JwtRegisteredClaimNames;

namespace Bislerium.Controllers;

[ApiController]
[Route("api/account")]
public class AccountController : Controller
{
   private readonly IGenericRepository _genericRepository;
   private readonly JWTSettings _jwtSettings;

   public AccountController(IGenericRepository genericRepository, IOptions<JWTSettings> jwtSettings)
   {
      _genericRepository = genericRepository;
      _jwtSettings = jwtSettings.Value;

   }

   [HttpPost("login")]
   public IActionResult Login(LoginDto loginRequest)
   {
      var user = _genericRepository.GetFirstOrDefault<User>(x => x.EmailAddress == loginRequest.EmailAddress);

      if (user == null)
      {
         return NotFound(new ResponseDto<bool>()
         {
            Message = "User not found",
            Result = false,
            Status = "Not Found",
            StatusCode = HttpStatusCode.NotFound,
            TotalCount = 0
         });
      }

      var isPasswordValid = Password.VerifyHash(loginRequest.Password, user.Password);

      if (!isPasswordValid)
      {
         return Unauthorized(new ResponseDto<bool>()
         {
            Message = "Password incorrect",
            Result = false,
            Status = "Unauthorized",
            StatusCode = HttpStatusCode.Unauthorized,
            TotalCount = 0
         });
      }

      var role = _genericRepository.GetById<Role>(user.RoleId);
      
      var authClaims = new List<Claim>
      {
         new(ClaimTypes.NameIdentifier, (user.Id.ToString() ?? null) ?? string.Empty),
         new(ClaimTypes.Name, user.FullName),
         new(ClaimTypes.Email, user.EmailAddress),
         new(ClaimTypes.Role, role.Name ?? ""),
         new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
      };
      
      var symmetricSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.Key));
                
      var signingCredentials = new SigningCredentials(symmetricSigningKey, SecurityAlgorithms.HmacSha256);
                
      var expirationTime = DateTime.UtcNow.AddMinutes(Convert.ToInt32(_jwtSettings.DurationInMinutes));
                
      var accessToken = new JwtSecurityToken(
         _jwtSettings.Issuer,
         _jwtSettings.Audience,
         claims: authClaims,
         signingCredentials: signingCredentials,
         expires: expirationTime
      );

      var userDetails = new UserDto()
      {
         Id = user.Id,
         Name = user.FullName,
         Username = user.UserName,
         EmailAddress = user.EmailAddress,
         RoleId = role.Id,
         Role = role.Name ?? "",
         Token = new JwtSecurityTokenHandler().WriteToken(accessToken)
      };

      return Ok(new ResponseDto<UserDto>()
      {
         Message = "Successfully authenticated",
         Result = userDetails,
         Status = "Success",
         StatusCode = HttpStatusCode.OK,
         TotalCount = 1
      });
   }

   [HttpPost("register")]
   public IActionResult Register(RegisterDto register)
   {
      var existingUser = _genericRepository.GetFirstOrDefault<User>(x =>
         x.EmailAddress == register.EmailAddress || x.UserName == register.Username);

      if (existingUser == null)
      {
         var role = _genericRepository.GetFirstOrDefault<Role>(x => x.Name == Constants.Roles.Blogger);

         var appUser = new User()
         {
            FullName = register.FullName,
            EmailAddress = register.EmailAddress,
            RoleId = role!.Id,
            Password = Password.HashSecret(register.Password),
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
         })
         .ToList();

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