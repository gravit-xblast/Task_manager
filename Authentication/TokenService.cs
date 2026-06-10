using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Task_Manager.Models;

namespace Task_Manager.Authentication
{
    // Contrat pour la génération de tokens JWT.
    public interface ITokenService
    {
        // Génère un token JWT signé pour l'utilisateur donné.
        string CreateAccessToken(User user);
    }


    // Service de création de tokens JWT.
    // Configuration requise dans appsettings.json ou variables d'environnement :
    //   - Jwt:jwt_secretKey  => clé secrète de signature (HMAC-SHA256)
    //   - Jwt:Issuer         => émetteur du token
    //   - Jwt:Audience       => destinataire du token
    //   - Jwt:ExpireMinutes  => durée de validité en minutes
    public class TokenService : ITokenService
    {
        private readonly IConfiguration _config;

        public TokenService(IConfiguration config)
        {
            _config = config;
        }

        // Crée un token JWT contenant les claims suivants :
        //   - Email    : adresse email de l'utilisateur
        //   - Sub      : subject (email)
        //   - Jti      : identifiant unique du token (GUID)
        //   - Role     : "Admin" ou "User" selon user.IsAdmin
        // Lève InvalidOperationException si jwt_secretKey est manquant.
        public string CreateAccessToken(User user)
        {
            var secretKey = _config["Jwt:SecretKey"] ?? throw new InvalidOperationException("Jwt:SecretKey missing");

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            // Claims = informations encodées dans le token
            var claims = new List<Claim>
            {
                new(ClaimTypes.Email, user.Email),
                new(JwtRegisteredClaimNames.Sub, user.Email),
                new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),

                //(pas besoin de toucher la DB à chaque requête)
                new(ClaimTypes.Role, user.IsAdmin ? "Admin" : "User"),
            };

            // var expireMinutes = _config.GetValue<int>("Jwt:ExpireMinutes");
            var expireMinutes = _config.GetValue<int>("Jwt:ExpireMinutes", 60);

            var token = new JwtSecurityToken(
                issuer: _config["Jwt:Issuer"],
                audience: _config["Jwt:Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(expireMinutes),
                signingCredentials: credentials
            );
            return new JwtSecurityTokenHandler().WriteToken(token); // conversion du token en string
        }
    }
}















