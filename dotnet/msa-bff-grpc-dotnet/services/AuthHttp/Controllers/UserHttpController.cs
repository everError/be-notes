using Auth.Data;
using Auth.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Auth.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UserHttpController(AppDbContext db) : ControllerBase
{
    private readonly AppDbContext _db = db;

    [HttpGet]
    public async Task<IActionResult> GetUsers()
    {
        var users = await _db.Users.ToListAsync();

        var userList = new UserList
        {
            Users = [.. users.Select(u => new UserReply
            {
                Id = u.Id,
                Name = u.Name,
                Email = u.Email
            })]
        };

        return Ok(userList);
    }
    [HttpDelete]
    public async Task<IActionResult> DeleteAllUsers()
    {
        await _db.Users.ExecuteDeleteAsync();
        return Ok();
    }
}
