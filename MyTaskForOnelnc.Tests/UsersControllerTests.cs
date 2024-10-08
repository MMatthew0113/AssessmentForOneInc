using Moq;
using Xunit;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
using System;
using Microsoft.AspNetCore.Mvc;
using System.Linq;
using System.Collections.Generic;

public class UsersControllerTests
{
    // Mock ILogger
    private readonly Mock<ILogger<UsersController>> _loggerMock;

    // In-memory DB context options
    private readonly DbContextOptions<UserDbContext> _dbContextOptions;

    public UsersControllerTests()
    {
        _loggerMock = new Mock<ILogger<UsersController>>();
        _dbContextOptions = new DbContextOptionsBuilder<UserDbContext>()
            .UseInMemoryDatabase(databaseName: "TestDb")
            .Options;
    }

    [Fact]
    public async Task GetUser_ReturnsOk_WhenUserExists()
    {
        // Arrange
        using (var context = new UserDbContext(_dbContextOptions))
        {
            var user = new User
            {
                Id = Guid.NewGuid(),
                FirstName = "John",
                LastName = "Doe",
                Email = "johndoe@example.com",
                DateOfBirth = new DateTime(1990, 1, 1),
                PhoneNumber = "1234567890"
            };

            context.Users.Add(user);
            context.SaveChanges();

            var controller = new UsersController(context, _loggerMock.Object);

            // Act
            var result = await controller.GetUser(user.Id);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var returnedUser = Assert.IsType<UserResponse>(okResult.Value);
            Assert.Equal(user.Id, returnedUser.Id);
            Assert.Equal(user.FirstName, returnedUser.FirstName);
        }
    }

    [Fact]
    public async Task GetUser_ReturnsNotFound_WhenUserDoesNotExist()
    {
        // Arrange
        using (var context = new UserDbContext(_dbContextOptions))
        {
            var controller = new UsersController(context, _loggerMock.Object);

            // Act
            var result = await controller.GetUser(Guid.NewGuid());

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }
    }

    [Fact]
    public async Task CreateUser_ReturnsCreated_WhenUserIsValid()
    {
        // Arrange
        using (var context = new UserDbContext(_dbContextOptions))
        {
            var user = new User
            {
                Id = Guid.NewGuid(),
                FirstName = "John",
                LastName = "Doe",
                Email = "johndoe@example.com",
                DateOfBirth = new DateTime(1990, 1, 1),
                PhoneNumber = "1234567890"
            };

            var controller = new UsersController(context, _loggerMock.Object);

            // Act
            var result = await controller.CreateUser(user);

            // Assert
            var createdAtActionResult = Assert.IsType<CreatedAtActionResult>(result);
            var createdUser = Assert.IsType<User>(createdAtActionResult.Value);
            Assert.Equal(user.Email, createdUser.Email);
            Assert.Equal(user.FirstName, createdUser.FirstName);
        }
    }

    [Fact]
    public async Task CreateUser_ReturnsBadRequest_WhenEmailIsNotUnique()
    {
        // Arrange
        using (var context = new UserDbContext(_dbContextOptions))
        {
            var existingUser = new User
            {
                Id = Guid.NewGuid(),
                FirstName = "Jane",
                LastName = "Smith",
                Email = "johndoe@example.com",
                DateOfBirth = new DateTime(1990, 1, 1),
                PhoneNumber = "1234567890"
            };
            context.Users.Add(existingUser);
            context.SaveChanges();

            var newUser = new User
            {
                Id = Guid.NewGuid(),
                FirstName = "John",
                LastName = "Doe",
                Email = "johndoe@example.com", // Same email
                DateOfBirth = new DateTime(1995, 5, 1),
                PhoneNumber = "0987654321"
            };

            var controller = new UsersController(context, _loggerMock.Object);

            // Act
            var result = await controller.CreateUser(newUser);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("Email must be unique.", badRequestResult.Value);
        }
    }

    [Fact]
    public async Task CreateUser_ReturnsBadRequest_WhenUserIsUnder18()
    {
        // Arrange
        using (var context = new UserDbContext(_dbContextOptions))
        {
            var user = new User
            {
                Id = Guid.NewGuid(),
                FirstName = "John",
                LastName = "Doe",
                Email = "johndoe@example.com",
                DateOfBirth = DateTime.UtcNow.AddYears(-17), // User is under 18
                PhoneNumber = "1234567890"
            };

            var controller = new UsersController(context, _loggerMock.Object);

            // Act
            var result = await controller.CreateUser(user);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("User must be 18 years or older.", badRequestResult.Value);
        }
    }

    [Fact]
    public async Task DeleteUser_ReturnsNoContent_WhenUserIsDeleted()
    {
        // Arrange
        using (var context = new UserDbContext(_dbContextOptions))
        {
            var user = new User
            {
                Id = Guid.NewGuid(),
                FirstName = "John",
                LastName = "Doe",
                Email = "johndoe@example.com",
                DateOfBirth = new DateTime(1990, 1, 1),
                PhoneNumber = "1234567890"
            };
            context.Users.Add(user);
            context.SaveChanges();

            var controller = new UsersController(context, _loggerMock.Object);

            // Act
            var result = await controller.DeleteUser(user.Id);

            // Assert
            Assert.IsType<NoContentResult>(result);
            Assert.False(context.Users.Any(u => u.Id == user.Id));
        }
    }
}
