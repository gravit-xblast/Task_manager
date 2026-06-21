using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
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
        [HttpDelete("delete")]
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
            if (targetUser.UserStatus != UserStatus.Standard)
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
        // READ ALL USERS /readAllusers
        // Accessible uniquement aux administrateurs (token JWT avec rôle "Admin")
        // -------------------------

        // -------------------------
        // UPDATE USER STATUS /readAllusers
        // Accessible uniquement aux administrateurs (token JWT avec rôle "super Admin")
        // -------------------------


        // -------------------------
        // UPDATE USER STATUS /readAllusers
        // Accessible uniquement aux administrateurs (token JWT avec rôle "super Admin")
        // -------------------------
    }
}
