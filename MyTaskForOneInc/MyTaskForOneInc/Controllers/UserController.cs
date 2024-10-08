using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading.Tasks;

[Route("api/[controller]")]
[ApiController]
public class UsersController : ControllerBase
{
    private readonly UserDbContext _context;
    private readonly ILogger<UsersController> _logger;

    public UsersController(UserDbContext context, ILogger<UsersController> logger)
    {
        _context = context;
        _logger = logger;
    }

    // GET: api/users
    [HttpGet]
    public async Task<IActionResult> GetUsers()
    {
        _logger.LogInformation("Fetching all users at {Time}", DateTime.UtcNow);
        try
        {
            var users = await _context.Users
                .Select(u => new UserResponse
                {
                    Id = u.Id,
                    FirstName = u.FirstName,
                    LastName = u.LastName,
                    Email = u.Email,
                    DateOfBirth = u.DateOfBirth,
                    PhoneNumber = u.PhoneNumber,
                    Age = DateTime.UtcNow.Year - u.DateOfBirth.Year
                })
                .ToListAsync();

            _logger.LogInformation("Fetched {Count} users successfully at {Time}", users.Count, DateTime.UtcNow);
            return Ok(users);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while fetching users at {Time}", DateTime.UtcNow);
            return StatusCode(500, "Internal server error");
        }
    }

    // GET: api/users/{id}
    [HttpGet("{id}")]
    public async Task<IActionResult> GetUser(Guid id)
    {
        _logger.LogInformation("Fetching user with ID {UserId} at {Time}", id, DateTime.UtcNow);
        try
        {
            var user = await _context.Users
                .Where(u => u.Id == id)
                .Select(u => new UserResponse
                {
                    Id = u.Id,
                    FirstName = u.FirstName,
                    LastName = u.LastName,
                    Email = u.Email,
                    DateOfBirth = u.DateOfBirth,
                    PhoneNumber = u.PhoneNumber,
                    Age = DateTime.UtcNow.Year - u.DateOfBirth.Year
                })
                .FirstOrDefaultAsync();

            if (user == null)
            {
                _logger.LogWarning("User with ID {UserId} not found at {Time}", id, DateTime.UtcNow);
                return NotFound();
            }

            _logger.LogInformation("Fetched user with ID {UserId} successfully at {Time}", id, DateTime.UtcNow);
            return Ok(user);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while fetching user with ID {UserId} at {Time}", id, DateTime.UtcNow);
            return StatusCode(500, "Internal server error");
        }
    }

    // POST: api/users
    [HttpPost]
    public async Task<IActionResult> CreateUser([FromBody] User user)
    {
        _logger.LogInformation("Creating a new user at {Time}", DateTime.UtcNow);
        try
        {
            if (_context.Users.Any(u => u.Email == user.Email))
            {
                _logger.LogWarning("Attempted to create user with duplicate email {Email} at {Time}", user.Email, DateTime.UtcNow);
                return BadRequest("Email must be unique.");
            }

            var userAge = CalculateAge(user.DateOfBirth);
            if (userAge < 18)
            {
                return BadRequest("User must be 18 years or older.");
            }

            _context.Users.Add(user);
            await _context.SaveChangesAsync();
            _logger.LogInformation("User created successfully with ID {UserId} at {Time}", user.Id, DateTime.UtcNow);
            return CreatedAtAction(nameof(GetUser), new { id = user.Id }, user);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while creating a user at {Time}", DateTime.UtcNow);
            return StatusCode(500, "Internal server error");
        }
    }

    // PUT: api/users/{id}
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateUser(Guid id, [FromBody] User updatedUser)
    {
        _logger.LogInformation("Updating user with ID {UserId} at {Time}", id, DateTime.UtcNow);
        try
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null)
            {
                _logger.LogWarning("User with ID {UserId} not found for update at {Time}", id, DateTime.UtcNow);
                return NotFound();
            }

            if (_context.Users.Any(u => u.Email == updatedUser.Email && u.Id != id))
            {
                _logger.LogWarning("Attempted to update user with duplicate email {Email} at {Time}", updatedUser.Email, DateTime.UtcNow);
                return BadRequest("Email must be unique.");
            }

            var userAge = CalculateAge(updatedUser.DateOfBirth);
            if (userAge < 18)
            {
                return BadRequest("User must be 18 years or older.");
            }

            user.FirstName = updatedUser.FirstName;
            user.LastName = updatedUser.LastName;
            user.Email = updatedUser.Email;
            user.DateOfBirth = updatedUser.DateOfBirth;
            user.PhoneNumber = updatedUser.PhoneNumber;

            await _context.SaveChangesAsync();
            _logger.LogInformation("User with ID {UserId} updated successfully at {Time}", id, DateTime.UtcNow);
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while updating user with ID {UserId} at {Time}", id, DateTime.UtcNow);
            return StatusCode(500, "Internal server error");
        }
    }

    // DELETE: api/users/{id}
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteUser(Guid id)
    {
        _logger.LogInformation("Deleting user with ID {UserId} at {Time}", id, DateTime.UtcNow);
        try
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null)
            {
                _logger.LogWarning("User with ID {UserId} not found for deletion at {Time}", id, DateTime.UtcNow);
                return NotFound();
            }

            _context.Users.Remove(user);
            await _context.SaveChangesAsync();
            _logger.LogInformation("User with ID {UserId} deleted successfully at {Time}", id, DateTime.UtcNow);
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while deleting user with ID {UserId} at {Time}", id, DateTime.UtcNow);
            return StatusCode(500, "Internal server error");
        }
    }

    private int CalculateAge(DateTime dateOfBirth)
    {
        var currentDate = DateTime.UtcNow;
        var age = currentDate.Year - dateOfBirth.Year;
        if (dateOfBirth > currentDate.AddYears(-age)) age--; // Adjust if birthday hasn't occurred yet this year
        return age;
    }
}

// Response DTO for users
public class UserResponse
{
    public Guid Id { get; set; }
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public string Email { get; set; }
    public DateTime DateOfBirth { get; set; }
    public string PhoneNumber { get; set; }
    public int Age { get; set; }
}
