using Bislerium.Application.Interfaces.Repositories.Base;
using Bislerium.Application.Interfaces.Services;
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

    // public IActionResult GetHomePageBlogs(string? sortBy = null)
    // {
    //     var blogs = _genericRepository.Get<Blog>(x => x.IsActive);
    // }
    //
    // public IActionResult GetBloggersBlogs(string? sortBy = null)
    // {
    // }
}