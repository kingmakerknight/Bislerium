using System.Net;
using Bislerium.Application.DTOs.Base;
using Bislerium.Application.DTOs.Home;
using Bislerium.Application.Interfaces.Repositories.Base;
using Bislerium.Application.Interfaces.Services;
using Bislerium.Entities.Models;
using Bislerium.Entities.Utilities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Bislerium.Controllers;

[Authorize]
[ApiController]
[Route("api/home")]
public class HomeController : Controller
{
    private readonly IUserService _userService;
    private readonly IGenericRepository _genericRepository;

    public HomeController(IUserService userService, IGenericRepository genericRepository)
    {
        _userService = userService;
        _genericRepository = genericRepository;
    }

    [HttpGet("home-page-blogs")]
    public IActionResult GetHomePageBlogs(int pageNumber, int pageSize, string? sortBy = null)
    {
        var userId = _userService.UserId;

        var user = _genericRepository.GetById<User>(userId);
        
        var blogs = _genericRepository.Get<Blog>(x => x.IsActive);

        var blogDetails = blogs as Blog[] ?? blogs.ToArray();

        var blogPostDetails = new List<BlogPostDetailsDto>();
        
        foreach (var blog in blogDetails)
        {
            var reactions = _genericRepository.Get<Reaction>(x => x.BlogId == blog.Id && x.IsReactedForBlog && x.IsActive);

            var comments = _genericRepository.Get<Comment>(x => x.BlogId == blog.Id && x.IsActive);

            var reactionDetails = reactions as Reaction[] ?? reactions.ToArray();
            
            var commentDetails = comments as Comment[] ?? comments.ToArray();

            var upVotes = reactionDetails.Where(x => x.ReactionId == 1 && x.BlogId == blog.Id && x.IsReactedForBlog);
         
            var downVotes = reactionDetails.Where(x => x.ReactionId == 2 && x.BlogId == blog.Id && x.IsReactedForBlog);
         
            var commentForComments =
                commentDetails.Where(x => 
                    commentDetails.Select(z => 
                        z.CommentId).Contains(x.CommentId) && x.IsCommentForComment);
            
            var popularity = upVotes.Count() * 2 - 
                             downVotes.Count() * 1 + 
                             commentDetails.Count() + commentForComments.Count();
            
            blogPostDetails.Add(new BlogPostDetailsDto()
            {
                BlogId = blog.Id,
                Title = blog.Title,
                Body = blog.Body,
                UpVotes = reactionDetails.Count(x => x.ReactionId == 1),
                DownVotes = reactionDetails.Count(x => x.ReactionId == 2),
                IsUpVotedByUser = reactionDetails.Any(x => x.ReactionId == 1 && x.CreatedBy == user.Id),
                IsDownVotedByUser = reactionDetails.Any(x => x.ReactionId == 2 && x.CreatedBy == user.Id),
                IsEdited = blog.LastModifiedAt != null,
                CreatedAt = blog.CreatedAt,
                PopularityPoints = popularity,
                Images = _genericRepository.Get<BlogImage>(x => x.BlogId == blog.Id && x.IsActive).Select(x => x.ImageURL).ToList(),
                UploadedTimePeriod = DateTime.Now.Hour - blog.CreatedAt.Hour < 24 ? $"{(int)(DateTime.Now - blog.CreatedAt).TotalHours} hours ago" : blog.CreatedAt.ToString("dd-MM-yyyy HH:mm"),
                Comments = _genericRepository.Get<Comment>(x => x.BlogId == blog.Id && x.IsActive && x.IsCommentForBlog ).Select(x => new PostComments()
                {
                    Comment = x.Text,
                    UpVotes = _genericRepository.Get<Reaction>(z => z.BlogId == blog.Id && z.IsReactedForComment && x.IsActive).Count(z => z.ReactionId == 1),
                    DownVotes = _genericRepository.Get<Reaction>(z => z.BlogId == blog.Id && z.IsReactedForComment && x.IsActive).Count(z => z.ReactionId == 2),
                    IsUpVotedByUser = _genericRepository.Get<Reaction>(z => z.BlogId == blog.Id && z.IsReactedForComment && x.IsActive).Any(z => z.ReactionId == 1 && z.CreatedBy == user.Id),
                    IsDownVotedByUser = _genericRepository.Get<Reaction>(z => z.BlogId == blog.Id && z.IsReactedForComment && x.IsActive).Any(z => z.ReactionId == 2 && z.CreatedBy == user.Id),
                    CommentId = x.Id,
                    CommentedBy = _genericRepository.GetById<User>(x.CreatedBy).FullName,
                    ImageUrl = _genericRepository.GetById<User>(x.CreatedBy).ImageURL ?? "sample-profile.png",
                    IsUpdated = x.LastModifiedAt != null,
                    CommentedTimePeriod = DateTime.Now.Hour - x.CreatedAt.Hour < 24 ? $"{(int)(DateTime.Now - x.CreatedAt).TotalHours} hours ago" : x.CreatedAt.ToString("dd-MM-yyyy HH:mm"),
                }).Take(1).ToList()
            });
        }

        switch (sortBy)
        {
            case null:
                blogPostDetails = blogPostDetails.OrderByDescending(x => x.CreatedAt).ToList();
                break;
            case "Popularity":
                blogPostDetails = blogPostDetails.OrderByDescending(x => x.PopularityPoints).ToList();
                break;
            case "Recency":
                blogPostDetails = blogPostDetails.OrderByDescending(x => x.CreatedAt).ToList();
                break;
            default:
                blogPostDetails.Shuffle();
                break;
        }
        
        var result = new ResponseDto<List<BlogPostDetailsDto>>()
        {
            Message = "Success",
            Result = blogPostDetails.Skip(pageNumber - 1).Take(pageSize).ToList(),
            Status = "Success",
            StatusCode = HttpStatusCode.OK,
            TotalCount = blogDetails.Count()
        };

        return Ok(result);
    }
    
    [HttpGet("my-blogs")]
    public IActionResult GetBloggersBlogs(int pageNumber, int pageSize, string? sortBy = null)
    {
        var userId = _userService.UserId;

        var user = _genericRepository.GetById<User>(userId);
        
        var blogs = _genericRepository.Get<Blog>(x => x.IsActive && x.CreatedBy == user.Id);

        var blogDetails = blogs as Blog[] ?? blogs.ToArray();

        var blogPostDetails = new List<BlogPostDetailsDto>();
        
        foreach (var blog in blogDetails)
        {
            var reactions = _genericRepository.Get<Reaction>(x => x.BlogId == blog.Id && x.IsReactedForBlog && x.IsActive);

            var comments = _genericRepository.Get<Comment>(x => x.BlogId == blog.Id && x.IsActive);

            var reactionDetails = reactions as Reaction[] ?? reactions.ToArray();
            
            var commentDetails = comments as Comment[] ?? comments.ToArray();

            var upVotes = reactionDetails.Where(x => x.ReactionId == 1 && x.BlogId == blog.Id && x.IsReactedForBlog);
         
            var downVotes = reactionDetails.Where(x => x.ReactionId == 2 && x.BlogId == blog.Id && x.IsReactedForBlog);
         
            var commentForComments =
                commentDetails.Where(x => 
                    commentDetails.Select(z => 
                        z.CommentId).Contains(x.CommentId) && x.IsCommentForComment);
            
            var popularity = upVotes.Count() * 2 - 
                             downVotes.Count() * 1 + 
                             commentDetails.Count() + commentForComments.Count();
            
            blogPostDetails.Add(new BlogPostDetailsDto()
            {
                BlogId = blog.Id,
                Title = blog.Title,
                Body = blog.Body,
                UpVotes = reactionDetails.Count(x => x.ReactionId == 1),
                DownVotes = reactionDetails.Count(x => x.ReactionId == 2),
                IsUpVotedByUser = reactionDetails.Any(x => x.ReactionId == 1 && x.CreatedBy == user.Id),
                IsDownVotedByUser = reactionDetails.Any(x => x.ReactionId == 2 && x.CreatedBy == user.Id),
                IsEdited = blog.LastModifiedAt != null,
                CreatedAt = blog.CreatedAt,
                PopularityPoints = popularity,
                Images = _genericRepository.Get<BlogImage>(x => x.BlogId == blog.Id && x.IsActive).Select(x => x.ImageURL).ToList(),
                UploadedTimePeriod = DateTime.Now.Hour - blog.CreatedAt.Hour < 24 ? $"{(int)(DateTime.Now - blog.CreatedAt).TotalHours} hours ago" : blog.CreatedAt.ToString("dd-MM-yyyy HH:mm"),
                Comments = _genericRepository.Get<Comment>(x => x.BlogId == blog.Id && x.IsActive && x.IsCommentForBlog).Select(x => new PostComments()
                {
                    Comment = x.Text,
                    UpVotes = _genericRepository.Get<Reaction>(z => z.BlogId == blog.Id && z.IsReactedForComment && x.IsActive).Count(z => z.ReactionId == 1),
                    DownVotes = _genericRepository.Get<Reaction>(z => z.BlogId == blog.Id && z.IsReactedForComment && x.IsActive).Count(z => z.ReactionId == 2),
                    IsUpVotedByUser = _genericRepository.Get<Reaction>(z => z.BlogId == blog.Id && z.IsReactedForComment && x.IsActive).Any(z => z.ReactionId == 1 && z.CreatedBy == user.Id),
                    IsDownVotedByUser = _genericRepository.Get<Reaction>(z => z.BlogId == blog.Id && z.IsReactedForComment && x.IsActive).Any(z => z.ReactionId == 2 && z.CreatedBy == user.Id),
                    CommentId = x.Id,
                    CommentedBy = _genericRepository.GetById<User>(x.CreatedBy).FullName,
                    ImageUrl = _genericRepository.GetById<User>(x.CreatedBy).ImageURL ?? "sample-profile.png",
                    IsUpdated = x.LastModifiedAt != null,
                    CommentedTimePeriod = DateTime.Now.Hour - x.CreatedAt.Hour < 24 ? $"{(int)(DateTime.Now - x.CreatedAt).TotalHours} hours ago" : x.CreatedAt.ToString("dd-MM-yyyy HH:mm"),
                }).Take(1).ToList()
            });
        }

        switch (sortBy)
        {
            case null:
                blogPostDetails = blogPostDetails.OrderByDescending(x => x.CreatedAt).ToList();
                break;
            case "Popularity":
                blogPostDetails = blogPostDetails.OrderByDescending(x => x.PopularityPoints).ToList();
                break;
            case "Recency":
                blogPostDetails = blogPostDetails.OrderByDescending(x => x.CreatedAt).ToList();
                break;
            default:
                blogPostDetails.Shuffle();
                break;
        }
        
        var result = new ResponseDto<List<BlogPostDetailsDto>>()
        {
            Message = "Success",
            Result = blogPostDetails.Skip(pageNumber - 1).Take(pageSize).ToList(),
            Status = "Success",
            StatusCode = HttpStatusCode.OK,
            TotalCount = blogDetails.Count()
        };

        return Ok(result);
    }

    [HttpGet("blogs-details/{blogId:int}")]
    public IActionResult GetBlogDetails(int blogId)
    {
        var userId = _userService.UserId;

        var user = _genericRepository.GetById<User>(userId);
        
        var blog = _genericRepository.GetById<Blog>(blogId);
        
        var reactions = _genericRepository.Get<Reaction>(x => x.BlogId == blog.Id && x.IsReactedForBlog && x.IsActive);

        var comments = _genericRepository.Get<Comment>(x => x.BlogId == blog.Id && x.IsActive);

        var reactionDetails = reactions as Reaction[] ?? reactions.ToArray();
        
        var commentDetails = comments as Comment[] ?? comments.ToArray();

        var upVotes = reactionDetails.Where(x => x.ReactionId == 1 && x.BlogId == blog.Id && x.IsReactedForBlog);
     
        var downVotes = reactionDetails.Where(x => x.ReactionId == 2 && x.BlogId == blog.Id && x.IsReactedForBlog);
     
        var commentForComments =
            commentDetails.Where(x => 
                commentDetails.Select(z => 
                    z.CommentId).Contains(x.CommentId) && x.IsCommentForComment);
        
        var popularity = upVotes.Count() * 2 - 
                         downVotes.Count() * 1 + 
                         commentDetails.Count() + commentForComments.Count();
        
        var blogDetails = new BlogPostDetailsDto()
        {
            BlogId = blog.Id,
            Title = blog.Title,
            Body = blog.Body,
            UpVotes = reactionDetails.Count(x => x.ReactionId == 1),
            DownVotes = reactionDetails.Count(x => x.ReactionId == 2),
            IsUpVotedByUser = reactionDetails.Any(x => x.ReactionId == 1 && x.CreatedBy == user.Id),
            IsDownVotedByUser = reactionDetails.Any(x => x.ReactionId == 2 && x.CreatedBy == user.Id),
            IsEdited = blog.LastModifiedAt != null,
            CreatedAt = blog.CreatedAt,
            PopularityPoints = popularity,
            Images = _genericRepository.Get<BlogImage>(x => x.BlogId == blog.Id && x.IsActive).Select(x => x.ImageURL).ToList(),
            UploadedTimePeriod = DateTime.Now.Hour - blog.CreatedAt.Hour < 24 ? $"{(int)(DateTime.Now - blog.CreatedAt).TotalHours} hours ago" : blog.CreatedAt.ToString("dd-MM-yyyy HH:mm"),
            Comments = GetCommentsRecursive(blog.Id, false, true)
        };
        
        var result = new ResponseDto<BlogPostDetailsDto>()
        {
            Message = "Success",
            Result = blogDetails,
            Status = "Success",
            StatusCode = HttpStatusCode.OK,
            TotalCount = 1
        };

        return Ok(result);
    }

    [HttpPost("upvote-downvote-blog")]
    public IActionResult UpVoteDownVoteBlog(int blogId, int reactionId)
    {
        var userId = _userService.UserId;

        var user = _genericRepository.GetById<User>(userId);

        var blog = _genericRepository.GetById<Blog>(blogId);

        var existingReaction = 
            _genericRepository.Get<Reaction>(x => x.CreatedBy == user.Id && 
                x.ReactionId != 3 && x.IsReactedForBlog);

        var existingReactionDetails = existingReaction as Reaction[] ?? existingReaction.ToArray();
        
        if (existingReactionDetails.Any())
        {
            _genericRepository.RemoveMultipleEntity(existingReactionDetails);
        }

        var reaction = new Reaction()
        {
            ReactionId = reactionId,
            BlogId = blog.Id,
            CommentId = null,
            IsReactedForBlog = true,
            IsReactedForComment = false,
            CreatedAt = DateTime.Now,
            CreatedBy = user.Id,
            IsActive = true,
        };

        _genericRepository.Insert(reaction);

        return Ok(new ResponseDto<object>()
        {
            Message = "Success",
            StatusCode = HttpStatusCode.OK,
            TotalCount = 0,
            Status = "Success",
            Result = true
        });
    }
    
    [HttpPost("upvote-downvote-comment")]
    public IActionResult UpVoteDownVoteComment(int commentId, int reactionId)
    {
        var userId = _userService.UserId;

        var user = _genericRepository.GetById<User>(userId);

        var comment = _genericRepository.GetById<Comment>(commentId);

        var existingReaction = 
            _genericRepository.Get<Reaction>(x => x.CreatedBy == user.Id && 
                                                  x.ReactionId == 3 && x.IsReactedForComment);

        var existingReactionDetails = existingReaction as Reaction[] ?? existingReaction.ToArray();
        
        if (existingReactionDetails.Any())
        {
            _genericRepository.RemoveMultipleEntity(existingReactionDetails);
        }

        var reaction = new Reaction()
        {
            ReactionId = reactionId,
            BlogId = null,
            CommentId = comment.Id,
            IsReactedForBlog = false,
            IsReactedForComment = true,
            CreatedAt = DateTime.Now,
            CreatedBy = user.Id,
            IsActive = true,
        };

        _genericRepository.Insert(reaction);

        return Ok(new ResponseDto<object>()
        {
            Message = "Success",
            StatusCode = HttpStatusCode.OK,
            TotalCount = 0,
            Status = "Success",
            Result = true
        });
    }

    [HttpPost("comment-for-blog")]
    public IActionResult CommentForBlog(int blogId, string commentText)
    {
        var userId = _userService.UserId;

        var user = _genericRepository.GetById<User>(userId);
        
        var blog = _genericRepository.GetById<Blog>(blogId);

        var comment = new Comment()
        {
            BlogId = blog.Id,
            CommentId = null,
            Text = commentText,
            IsCommentForBlog = true,
            IsCommentForComment = false,
            IsActive = true,
            CreatedAt = DateTime.Now,
            CreatedBy = user.Id,
        };
        
        _genericRepository.Insert(comment);

        return Ok(new ResponseDto<object>()
        {
            Message = "Success",
            StatusCode = HttpStatusCode.OK,
            TotalCount = 0,
            Status = "Success",
            Result = true
        });
    }
    
    [HttpPost("comment-for-comment")]
    public IActionResult CommentForComment(int commentId, string commentText)
    {
        var userId = _userService.UserId;

        var user = _genericRepository.GetById<User>(userId);
        
        var commentModel = _genericRepository.GetById<Comment>(commentId);

        var comment = new Comment()
        {
            BlogId = null,
            CommentId = commentModel.Id,
            Text = commentText,
            IsCommentForBlog = false,
            IsCommentForComment = true,
            IsActive = true,
            CreatedAt = DateTime.Now,
            CreatedBy = user.Id,
        };
        
        _genericRepository.Insert(comment);

        return Ok(new ResponseDto<object>()
        {
            Message = "Success",
            StatusCode = HttpStatusCode.OK,
            TotalCount = 0,
            Status = "Success",
            Result = true
        });
    }

    [HttpDelete("delete-comment/{commentId:int}")]
    public IActionResult DeleteComment(int commentId)
    {
        var comment = _genericRepository.GetById<Comment>(commentId);

        comment.IsActive = false;
        
        _genericRepository.Update(comment);
        
        return Ok(new ResponseDto<object>()
        {
            Message = "Success",
            StatusCode = HttpStatusCode.OK,
            TotalCount = 0,
            Status = "Success",
            Result = true
        });
    }
    
    [HttpDelete("remove-blog-reaction/{blogId:int}")]
    public IActionResult RemoveBlogVote(int blogId)
    {
        var blog = _genericRepository.GetById<Blog>(blogId);

        var blogReactions = _genericRepository.Get<Reaction>(x => x.BlogId == blog.Id 
                                                                     && x.IsReactedForBlog);

        foreach (var blogReaction in blogReactions)
        {
            blogReaction.IsActive = false;
            
            _genericRepository.Update(blogReaction);
        }
        
        return Ok(new ResponseDto<object>()
        {
            Message = "Success",
            StatusCode = HttpStatusCode.OK,
            TotalCount = 0,
            Status = "Success",
            Result = true
        });
    }
    
    [HttpDelete("remove-comment-reaction/{commentId:int}")]
    public IActionResult RemoveCommentVote(int commentId)
    {
        var comment = _genericRepository.GetById<Comment>(commentId);

        var commentReactions = _genericRepository.Get<Reaction>(x => x.CommentId == comment.Id 
                                                                     && x.IsReactedForComment);

        foreach (var commentReaction in commentReactions)
        {
            commentReaction.IsActive = false;
            
            _genericRepository.Update(commentReaction);
        }
        
        return Ok(new ResponseDto<object>()
        {
            Message = "Success",
            StatusCode = HttpStatusCode.OK,
            TotalCount = 0,
            Status = "Success",
            Result = true
        });
    }
    
    [NonAction]
    private List<PostComments> GetCommentsRecursive(int blogId, bool isForComment, bool isForBlog, int? parentId = null)
    {
        var userId = _userService.UserId;

        var user = _genericRepository.GetById<User>(userId);
        
        var comments = 
            _genericRepository.Get<Comment>(x => x.BlogId == blogId && x.IsActive &&
                x.IsCommentForBlog == isForBlog && x.IsCommentForComment == isForComment && x.CommentId == parentId)
                    .Select(x => new PostComments()
                    {
                        Comment = x.Text,
                        UpVotes = _genericRepository.Get<Reaction>(z => z.BlogId == blogId && z.IsReactedForComment).Count(z => z.ReactionId == 1 && z.CommentId == x.Id),
                        DownVotes = _genericRepository.Get<Reaction>(z => z.BlogId == blogId && z.IsReactedForComment).Count(z => z.ReactionId == 2 && z.CommentId == x.Id),
                        IsUpVotedByUser = _genericRepository.Get<Reaction>(z => z.BlogId == blogId && z.IsReactedForComment).Any(z => z.ReactionId == 1 && z.CreatedBy == user.Id && z.CommentId == x.Id),
                        IsDownVotedByUser = _genericRepository.Get<Reaction>(z => z.BlogId == blogId && z.IsReactedForComment).Any(z => z.ReactionId == 2 && z.CreatedBy == user.Id && z.CommentId == x.Id),
                        CommentId = x.Id,
                        CommentedBy = _genericRepository.GetById<User>(x.CreatedBy).FullName,
                        ImageUrl = _genericRepository.GetById<User>(x.CreatedBy).ImageURL ?? "sample-profile.png",
                        IsUpdated = x.LastModifiedAt != null,
                        CommentedTimePeriod = DateTime.Now.Hour - x.CreatedAt.Hour < 24 ? $"{(int)(DateTime.Now - x.CreatedAt).TotalHours} hours ago" : x.CreatedAt.ToString("dd-MM-yyyy HH:mm"),
                        Comments = GetCommentsRecursive(blogId, true, false, x.Id)
                    }).ToList();
        
        return comments;
    }
}