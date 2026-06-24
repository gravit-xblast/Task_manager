using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Task_Manager.Authentication;
using Task_Manager.Models;


namespace Task_Manager.Controllers
{
    [ApiController]
    [Route("")]
    [Authorize] // Toute action de ce contrôleur exige un utilisateur authentifié (Bearer JWT)
    public class UserController : ControllerBase
    {
        private readonly IUserService _userService;

        public UserController(IUserService userService)
        {
            _userService = userService;
        }

        // -------------------------
        // DELETE /delete
        // Accessible uniquement aux administrateurs (token JWT avec rôle "Admin")
        // -------------------------
        
        [Authorize(Policy = "AdminOnly")]
        [HttpDelete("user/delete")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Delete([FromBody] DeleteUserRequest request)
        {
            var targetUser = await _userService.GetUserByEmail(request.Email);

            if (targetUser is null)
            {
                return NotFound(new { detail = "Utilisateur introuvable" });
            }

            // Garde-fou : un administrateur ne peut pas supprimer un autre compte
            // administrateur (ni le sien) via cet endpoint, afin d'éviter de se
            // retrouver sans aucun compte admin sur l'application.
            if (targetUser.UserStatus != UserStatus.Standard) // Admin & SuperAdmin
            {
                return StatusCode(StatusCodes.Status403Forbidden,
                    new { detail = "Impossible de supprimer un compte administrateur" });
            }

            // var deleted = await _userService.DeleteUser(request.Email);
            var deleted = await _userService.DeleteUser(targetUser.Id);

            if (!deleted)
            {
                return NotFound(new { detail = "Utilisateur introuvable" });
            }

            return NoContent();
        }


        // -------------------------
        // READ ALL USERS /ReadAllUsers
        // Accessible uniquement aux administrateurs (token JWT avec rôle "Admin")
        // -------------------------

        [Authorize(Policy = "AdminOnly")]
        [HttpGet("users/get")]
        [ProducesResponseType(typeof(IEnumerable<UserResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        public async Task<IActionResult> ReadAllUsers()
        {
            var users = await _userService.GetAllUsers();

            if (!users.Any())
                return NoContent();

            var response = users.Select(user => new UserResponse
            {
                Id = user.Id,
                UserName = user.UserName,
                Email = user.Email,
                UserStatus = user.UserStatus,
                IsActive = user.IsActive,
                CreatedAt = user.CreatedAt
            });

            return Ok(response);
        }


        // -------------------------
        // READ USER / ReadUser
        // Accessible uniquement aux administrateurs (token JWT avec rôle "super Admin")
        // ------------------------- 

        [Authorize(Policy = "AdminOnly")]
        [HttpGet("user/get")]
        [ProducesResponseType(typeof(IEnumerable<UserResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        public async Task<IActionResult> ReadUser([FromQuery] string email)
        {
            var user = await _userService.GetUserByEmail(email);
            if (user is null)
            {
                return NoContent();
            }

            var response = (new UserResponse
            {
                Id = user.Id,
                UserName = user.UserName,
                Email = user.Email,
                UserStatus = user.UserStatus,
                IsActive = user.IsActive,
                CreatedAt = user.CreatedAt
            });

            return Ok(response);
        }


        // -------------------------
        // PATCH /user/promote
        // Accessible uniquement aux SuperAdmins
        // -------------------------

        [Authorize(Policy = "SuperAdminOnly")]
        [HttpPatch("user/promote")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> PromoteUser([FromBody] PromoteUserRequest request)
        {
            // Récupère l'email du SuperAdmin depuis le token JWT
            var requesterEmail = User.FindFirst(ClaimTypes.Email)?.Value;
            if (requesterEmail is null)
                return Unauthorized();

            var result = await _userService.PromoteUser(requesterEmail, request);

            return result switch
            {
                UpdateUserResult.Success => Ok(new { detail = "Utilisateur mis à jour" }),
                UpdateUserResult.UserNotFound => NotFound(new { detail = "Utilisateur introuvable" }),
                UpdateUserResult.Forbidden => StatusCode(403, new { detail = "Impossible de modifier son propre compte" }),
                UpdateUserResult.NoChanges => BadRequest(new { detail = "Aucun champ à modifier fourni" }),
                _ => StatusCode(500, new { detail = "Erreur inattendue" })
            };
        }
    }
}
