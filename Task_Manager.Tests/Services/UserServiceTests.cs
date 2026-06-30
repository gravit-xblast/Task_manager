using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Task_Manager.Authentication;
using Task_Manager.Data;
using Task_Manager.Models;

namespace Task_Manager.Tests.Services
{
    public class UserServiceTests
    {
        // -------------------------
        // Helpers
        // -------------------------

        // Chaque test reçoit une base InMemory isolée (Guid unique)
        // pour éviter toute interférence entre les tests.
        private Task_Manager_DbContext GetInMemoryContext()
        {
            var options = new DbContextOptionsBuilder<Task_Manager_DbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            return new Task_Manager_DbContext(options);
        }

        private RegisterRequest BuildRegisterRequest(
            string email = "user@test.com",
            string userName = "testuser",
            string password = "Password123")
        {
            return new RegisterRequest
            {
                Email = email,
                UserName = userName,
                Password = password
            };
        }

        // Insère un utilisateur directement en base (sans passer par RegisterUser)
        // utile pour tester Authenticate, Delete, Promote sans dépendre de RegisterUser.
        private async Task<User> SeedUser(
            Task_Manager_DbContext context,
            string email = "user@test.com",
            string password = "Password123",
            UserStatus status = UserStatus.Standard,
            bool isActive = true)
        {
            var user = new User
            {
                UserName = "seeduser",
                Email = email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
                UserStatus = status,
                IsActive = isActive,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            context.Users.Add(user);
            await context.SaveChangesAsync();
            return user;
        }


        // =========================================================
        // RegisterUser
        // =========================================================

        [Fact]
        public async Task RegisterUser_NewEmail_ReturnsUser()
        {
            var context = GetInMemoryContext();
            var service = new UserService(context);

            var result = await service.RegisterUser(BuildRegisterRequest());

            result.Should().NotBeNull();
            result!.Email.Should().Be("user@test.com");
            result.UserName.Should().Be("testuser");
        }

        [Fact]
        public async Task RegisterUser_NewEmail_DefaultStatus_IsStandard()
        {
            var context = GetInMemoryContext();
            var service = new UserService(context);

            var result = await service.RegisterUser(BuildRegisterRequest());

            result!.UserStatus.Should().Be(UserStatus.Standard);
        }

        [Fact]
        public async Task RegisterUser_WithSuperAdminStatus_ReturnsSuperAdmin()
        {
            var context = GetInMemoryContext();
            var service = new UserService(context);

            var result = await service.RegisterUser(BuildRegisterRequest(), UserStatus.SuperAdmin);

            result!.UserStatus.Should().Be(UserStatus.SuperAdmin);
        }

        [Fact]
        public async Task RegisterUser_NewAccount_IsActiveTrue()
        {
            var context = GetInMemoryContext();
            var service = new UserService(context);

            var result = await service.RegisterUser(BuildRegisterRequest());

            result!.IsActive.Should().BeTrue();
        }

        [Fact]
        public async Task RegisterUser_PasswordIsHashed()
        {
            var context = GetInMemoryContext();
            var service = new UserService(context);

            var result = await service.RegisterUser(BuildRegisterRequest(password: "Password123"));

            result!.PasswordHash.Should().NotBe("Password123");
            BCrypt.Net.BCrypt.Verify("Password123", result.PasswordHash).Should().BeTrue();
        }

        [Fact]
        public async Task RegisterUser_DuplicateEmail_ReturnsNull()
        {
            var context = GetInMemoryContext();
            var service = new UserService(context);
            var request = BuildRegisterRequest();

            await service.RegisterUser(request);
            var result = await service.RegisterUser(request); // doublon

            result.Should().BeNull();
        }

        [Fact]
        public async Task RegisterUser_CreatedAt_IsSet()
        {
            var context = GetInMemoryContext();
            var service = new UserService(context);
            var before = DateTime.UtcNow;

            var result = await service.RegisterUser(BuildRegisterRequest());

            result!.CreatedAt.Should().BeOnOrAfter(before);
            result.UpdatedAt.Should().BeOnOrAfter(before);
        }


        // =========================================================
        // Authenticate
        // =========================================================

        [Fact]
        public async Task Authenticate_ValidCredentials_ReturnsUser()
        {
            var context = GetInMemoryContext();
            var service = new UserService(context);
            await SeedUser(context, email: "user@test.com", password: "Password123");

            var result = await service.Authenticate("user@test.com", "Password123");

            result.Should().NotBeNull();
            result!.Email.Should().Be("user@test.com");
        }

        [Fact]
        public async Task Authenticate_WrongPassword_ReturnsNull()
        {
            var context = GetInMemoryContext();
            var service = new UserService(context);
            await SeedUser(context, email: "user@test.com", password: "Password123");

            var result = await service.Authenticate("user@test.com", "WrongPassword");

            result.Should().BeNull();
        }

        [Fact]
        public async Task Authenticate_UnknownEmail_ReturnsNull()
        {
            var context = GetInMemoryContext();
            var service = new UserService(context);

            var result = await service.Authenticate("nobody@test.com", "Password123");

            result.Should().BeNull();
        }

        [Fact]
        public async Task Authenticate_EmptyDatabase_ReturnsNull()
        {
            var context = GetInMemoryContext();
            var service = new UserService(context);

            var result = await service.Authenticate("user@test.com", "Password123");

            result.Should().BeNull();
        }


        // =========================================================
        // GetUserByEmail
        // =========================================================

        [Fact]
        public async Task GetUserByEmail_ExistingEmail_ReturnsUser()
        {
            var context = GetInMemoryContext();
            var service = new UserService(context);
            await SeedUser(context, email: "user@test.com");

            var result = await service.GetUserByEmail("user@test.com");

            result.Should().NotBeNull();
            result!.Email.Should().Be("user@test.com");
        }

        [Fact]
        public async Task GetUserByEmail_UnknownEmail_ReturnsNull()
        {
            var context = GetInMemoryContext();
            var service = new UserService(context);

            var result = await service.GetUserByEmail("nobody@test.com");

            result.Should().BeNull();
        }


        // =========================================================
        // GetUserById
        // =========================================================

        [Fact]
        public async Task GetUserById_ExistingId_ReturnsUser()
        {
            var context = GetInMemoryContext();
            var service = new UserService(context);
            var seeded = await SeedUser(context);

            var result = await service.GetUserById(seeded.Id);

            result.Should().NotBeNull();
            result!.Id.Should().Be(seeded.Id);
        }

        [Fact]
        public async Task GetUserById_UnknownId_ReturnsNull()
        {
            var context = GetInMemoryContext();
            var service = new UserService(context);

            var result = await service.GetUserById(9999);

            result.Should().BeNull();
        }


        // =========================================================
        // GetAllUsers
        // =========================================================

        [Fact]
        public async Task GetAllUsers_EmptyDatabase_ReturnsEmptyList()
        {
            var context = GetInMemoryContext();
            var service = new UserService(context);

            var result = await service.GetAllUsers();

            result.Should().NotBeNull();
            result.Should().BeEmpty();
        }

        [Fact]
        public async Task GetAllUsers_MultipleUsers_ReturnsAll()
        {
            var context = GetInMemoryContext();
            var service = new UserService(context);
            await SeedUser(context, email: "user1@test.com");
            await SeedUser(context, email: "user2@test.com");
            await SeedUser(context, email: "user3@test.com");

            var result = await service.GetAllUsers();

            result.Should().HaveCount(3);
        }


        // =========================================================
        // DeleteUser
        // =========================================================

        [Fact]
        public async Task DeleteUser_ExistingId_ReturnsTrue()
        {
            var context = GetInMemoryContext();
            var service = new UserService(context);
            var seeded = await SeedUser(context);

            var result = await service.DeleteUser(seeded.Id);

            result.Should().BeTrue();
        }

        [Fact]
        public async Task DeleteUser_ExistingId_UserRemovedFromDatabase()
        {
            var context = GetInMemoryContext();
            var service = new UserService(context);
            var seeded = await SeedUser(context);

            await service.DeleteUser(seeded.Id);

            var user = await service.GetUserById(seeded.Id);
            user.Should().BeNull();
        }

        [Fact]
        public async Task DeleteUser_UnknownId_ReturnsFalse()
        {
            var context = GetInMemoryContext();
            var service = new UserService(context);

            var result = await service.DeleteUser(9999);

            result.Should().BeFalse();
        }


        // =========================================================
        // PromoteUser
        // =========================================================

        [Fact]
        public async Task PromoteUser_ValidRequest_ReturnsSuccess()
        {
            var context = GetInMemoryContext();
            var service = new UserService(context);
            await SeedUser(context, email: "user@test.com");

            var result = await service.PromoteUser(
                "admin@test.com",
                new PromoteUserRequest { Email = "user@test.com", NewStatus = UserStatus.Admin }
            );

            result.Should().Be(UpdateUserResult.Success);
        }

        [Fact]
        public async Task PromoteUser_ValidRequest_UserStatusUpdated()
        {
            var context = GetInMemoryContext();
            var service = new UserService(context);
            await SeedUser(context, email: "user@test.com", status: UserStatus.Standard);

            await service.PromoteUser(
                "admin@test.com",
                new PromoteUserRequest { Email = "user@test.com", NewStatus = UserStatus.Admin }
            );

            var updated = await service.GetUserByEmail("user@test.com");
            updated!.UserStatus.Should().Be(UserStatus.Admin);
        }

        [Fact]
        public async Task PromoteUser_UnknownEmail_ReturnsUserNotFound()
        {
            var context = GetInMemoryContext();
            var service = new UserService(context);

            var result = await service.PromoteUser(
                "admin@test.com",
                new PromoteUserRequest { Email = "nobody@test.com", NewStatus = UserStatus.Admin }
            );

            result.Should().Be(UpdateUserResult.UserNotFound);
        }

        [Fact]
        public async Task PromoteUser_SelfModification_ReturnsForbidden()
        {
            var context = GetInMemoryContext();
            var service = new UserService(context);
            await SeedUser(context, email: "admin@test.com", status: UserStatus.SuperAdmin);

            var result = await service.PromoteUser(
                "admin@test.com",
                new PromoteUserRequest { Email = "admin@test.com", NewStatus = UserStatus.Standard }
            );

            result.Should().Be(UpdateUserResult.Forbidden);
        }

        [Fact]
        public async Task PromoteUser_ValidRequest_UpdatedAtRefreshed()
        {
            var context = GetInMemoryContext();
            var service = new UserService(context);
            var seeded = await SeedUser(context, email: "user@test.com");
            var originalUpdatedAt = seeded.UpdatedAt;

            await Task.Delay(10); // garantit un écart de temps mesurable

            await service.PromoteUser(
                "admin@test.com",
                new PromoteUserRequest { Email = "user@test.com", NewStatus = UserStatus.Admin }
            );

            var updated = await service.GetUserByEmail("user@test.com");
            updated!.UpdatedAt.Should().BeAfter(originalUpdatedAt);
        }
    }
}
