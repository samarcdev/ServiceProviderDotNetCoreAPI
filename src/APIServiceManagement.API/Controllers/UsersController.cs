using Microsoft.AspNetCore.Mvc;
using APIServiceManagement.API.Attributes;
using APIServiceManagement.Application.Interfaces.Services;
using APIServiceManagement.Application.DTOs.Requests;
using APIServiceManagement.Application.DTOs.Responses;
using Microsoft.AspNetCore.Authorization;

namespace APIServiceManagement.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class UsersController : ControllerBase
{
    private readonly IUserService _userService;

    public UsersController(IUserService userService)
    {
        _userService = userService;
    }

    [AuthorizeAdmin]
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var users = await _userService.GetAllUsersAsync();
        return Ok(users);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var user = await _userService.GetUserByIdAsync(id);
        return Ok(user);
    }

    [AuthorizeAdmin]
    [HttpPost]
    public async Task<IActionResult> Create(CreateUserRequest request)
    {
        await _userService.CreateUserAsync(request);
        return StatusCode(201);
    }

    [AuthorizeAdmin]
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(Guid id, UpdateUserRequest request)
    {
        await _userService.UpdateUserAsync(id, request);
        return NoContent();
    }

    [AuthorizeAdmin]
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        await _userService.DeleteUserAsync(id);
        return NoContent();
    }
}