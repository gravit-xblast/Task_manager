using Microsoft.EntityFrameworkCore;
using Task_Manager.Data;
using Task_Manager.Models;


namespace Task_Manager.Authentication
{
    public interface IUserService
    {
        Task<User?> Authenticate(string email, string password);
        Task<User?> RegisterUser(RegisterRequest data, UserStatus userStatus);
        Task<User?> GetUserByEmail(string email);
        Task<User?> GetUserById(int id);
        Task<List<User>> GetAllUsers();
        Task<bool> DeleteUser(int id);

        Task<UpdateUserResult> PromoteUser(string requesterEmail, PromoteUserRequest request);

        // Task<bool> PromoteUser(string email, UserStatus newStatus); // à ajouter
    }


    public class UserService : IUserService
    {
        private readonly Task_Manager_DbContext _context;


        public UserService(Task_Manager_DbContext context)
        {
            _context = context;
        }


        // Recherche un utilisateur par email
        public async Task<User?> GetUserByEmail(string email)
        {
            return await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
        }


        // Recherche un utilisateur par Id
        public async Task<User?> GetUserById(int id)
        {
            return await _context.Users.FirstOrDefaultAsync(u => u.Id == id);
        }


        // Allow for admin - Version en lecture seule
        public async Task<List<User>> GetAllUsers()
        {
            return await _context.Users
                .AsNoTracking()
                .ToListAsync();
        }


        // Authentifie l'utilisateur : vérifie l'email puis le mot de passe hashé
        public async Task<User?> Authenticate(string email, string password)
        {
            var user = await GetUserByEmail(email);
            if (user is null)
                return null;

            if (!BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
                return null;

            return user;
        }


        public async Task<User?> RegisterUser(RegisterRequest data, UserStatus userStatus = UserStatus.Standard)
        {
            var existing = await GetUserByEmail(data.Email);
            if (existing is not null)
                return null;

            var now = DateTime.UtcNow;
            var newUser = new User
            {
                UserName = data.UserName,
                Email = data.Email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(data.Password),
                IsActive = true,
                UserStatus = userStatus,
                CreatedAt = now,
                UpdatedAt = now
            };

            _context.Users.Add(newUser);

            try
            {
                await _context.SaveChangesAsync();
                return newUser;
            }
            catch (DbUpdateException ex) when (ex.InnerException?.Message.Contains("UNIQUE") == true)
            {
                // Race condition : deux inscriptions simultanées avec le même email
                // NB : "UNIQUE" est spécifique à SQLite. Avec un autre provider
                // (SQL Server, PostgreSQL...), adapter la condition (ex: code
                // d'erreur 2627/2601 pour SQL Server) ou se baser uniquement
                // sur la vérification GetUserByEmail ci-dessus.
                return null;
            }
            catch (DbUpdateException)
            {
                // Problème DB : contrainte violée, colonne trop longue, etc.
                return null;
            }
            catch (OperationCanceledException)
            {
                // Le client a annulé la requête HTTP
                return null;
            }
        }


        // Allowed for admin
        public async Task<bool> DeleteUser(int id)
        {
            var existingUser = await GetUserById(id);
            if (existingUser == null)
                return false;

            _context.Users.Remove(existingUser);
            await _context.SaveChangesAsync();

            return true;
        }


        public async Task<UpdateUserResult> PromoteUser(string requesterEmail, PromoteUserRequest request)
        {
            // Aucun champ à modifier fourni
            if (request.NewStatus is null && request.IsActive is null)
                return UpdateUserResult.NoChanges;


            var targetUser = await GetUserByEmail(request.Email);
            if (targetUser is null)
                return UpdateUserResult.UserNotFound;

            if (request.NewStatus is not null)
                targetUser.UserStatus = request.NewStatus.Value;

            if (request.IsActive is not null)
                targetUser.IsActive = request.IsActive.Value;

            targetUser.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return UpdateUserResult.Success;
        }
    }
}