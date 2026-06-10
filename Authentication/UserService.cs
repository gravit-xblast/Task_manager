using Task_Manager.Models;
using Microsoft.EntityFrameworkCore;
using Task_Manager.Data;


namespace Task_Manager.Authentication
{
    public interface IUserService
    {
        Task<User?> Authenticate(string email, string password);
        Task<(bool success, string? error, User? user)> RegisterUser(RegisterRequest data, bool isAdmin = false); // Pourquoi 'User?'
        Task<User?> GetUserByEmail(string email);
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

        //public async Task<(bool success, string? error, User? user)> RegisterUser(RegisterRequest data, bool isAdmin = false)
        //{
        //    try
        //    {
        //        // Vérification si l'email existe déjà
        //        var existing = await GetUserByEmail(data.Email);
        //        if (existing is not null)
        //            return (false, "Email déjà utilisé", null);

        //        // Création de l'utilisateur
        //        var newUser = new User
        //        {
        //            UserName = data.UserName,
        //            Email = data.Email,
        //            PasswordHash = BCrypt.Net.BCrypt.HashPassword(data.Password),
        //            IsActive = true,
        //            IsAdmin = isAdmin,
        //            CreatedAt = DateTime.UtcNow
        //        };

        //        // Sauvegarde en base
        //        // Add() dit à EF Core "prépare un INSERT pour cet utilisateur".SaveChangesAsync() envoie réellement la requête SQL à la base de données.
        //        _context.Users.Add(newUser);
        //        await _context.SaveChangesAsync();

        //        return (true, null, newUser);
        //    }
        //    catch (Exception ex) // à revoir
        //    {
        //        return (false, "Erreur interne", null);
        //    }
        //}


        public async Task<(bool success, string? error, User? user)> RegisterUser(RegisterRequest data, bool isAdmin = false)
        {
            var existing = await GetUserByEmail(data.Email);
            if (existing is not null)
                return (false, "Email déjà utilisé", null);

            var newUser = new User
            {
                UserName = data.UserName,
                Email = data.Email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(data.Password),
                IsActive = true,
                IsAdmin = isAdmin,
                CreatedAt = DateTime.UtcNow
            };

            _context.Users.Add(newUser);

            try
            {
                await _context.SaveChangesAsync();
                return (true, null, newUser);
            }
            catch (DbUpdateException ex) when (ex.InnerException?.Message.Contains("UNIQUE") == true)
            {
                // Race condition : deux inscriptions simultanées avec le même email
                return (false, "Email déjà utilisé", null);
            }
            catch (DbUpdateException)
            {
                // Problème DB : contrainte violated, colonne trop longue, etc.
                return (false, "Erreur lors de la sauvegarde", null);
            }
            catch (OperationCanceledException)
            {
                // Le client a annulé la requête HTTP
                return (false, "Requête annulée", null);
            }
        }
    }
}