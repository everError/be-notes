using Auth.Data;
using Auth.Models;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Microsoft.EntityFrameworkCore;

namespace Auth.Services;

public class UserService(AppDbContext context) : Auth.UserService.UserServiceBase
{
    private readonly AppDbContext _context = context;

    public override async Task<UserReply> AddUser(UserRequest request, ServerCallContext context)
    {
        var user = new User { Name = request.Name, Email = request.Email };
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        return new UserReply
        {
            Id = user.Id,
            Name = user.Name,
            Email = user.Email
        };
    }

    public override async Task<UserList> GetUsers(Empty request, ServerCallContext context)
    {
        var users = await _context.Users.Select(u => new UserReply
        {
            Id = u.Id,
            Name = u.Name,
            Email = u.Email
        }).ToListAsync();

        var list = new UserList();
        list.Users = users;

        return list;
    }

    public override async Task<SeedUsersReply> SeedUsers(SeedUsersRequest request, ServerCallContext context)
    {
        var count = request.Count > 0 ? request.Count : 100;
        var users = new List<User>();
        var rnd = new Random();

        for (int i = 0; i < count; i++)
        {
            var name = $"User{rnd.Next(1000, 9999)}";
            var email = $"{name.ToLower()}@example.com";

            users.Add(new User
            {
                Name = name,
                Email = email
            });
        }

        _context.Users.AddRange(users);
        var inserted = await _context.SaveChangesAsync();

        return new SeedUsersReply { Inserted = inserted };
    }

}
