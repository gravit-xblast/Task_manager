using Microsoft.AspNetCore.Mvc;
using Task_Manager.Authentication;
using Task_Manager.Models;

                                 
namespace Task_Manager.Controllers
{
    [ApiController]
    [Route("")]
    public class AuthenticationController : ControllerBase
    {
        private readonly IUserService _userService;
        private readonly ITokenService _tokenService;

        public AuthenticationController(IUserService userService, ITokenService tokenService)
        {
            _userService = userService;
            _tokenService = tokenService;
        }

        // -------------------------
        // POST /register
        // Accessible sans authentification
        // -------------------------
        [HttpPost("register")]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Register([FromBody] RegisterRequest data) // bool success, string? error, User? user
        {
            var (success, error, user) = await _userService.RegisterUser(data);

            if (!success)
            {
                var statusCode = error == "Email déjà utilisé" ? 400 : 500;
                return StatusCode(statusCode, new { detail = error });
            }

            return StatusCode(201, new
            {
                message = "Utilisateur créé avec succès",
                user_id = user!.Id,
                email = user.Email
            });
        }


        //[HttpPost("register")]
        //[ProducesResponseType(typeof(RegisterResponse), StatusCodes.Status201Created)]
        //[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        //[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
        //[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
        //public async Task<IActionResult> Register([FromBody] RegisterRequest data)
        //{
        //    var result = await _userService.RegisterUser(data);

        //    if (!result.success)
        //    {
        //        return result.error switch
        //        {
        //            RegisterError.EmailAlreadyUsed => Conflict(new ProblemDetails
        //            {
        //                Status = 409,
        //                Title = "Email déjà utilisé",
        //                Detail = $"L'adresse {data.Email} est associée à un compte existant."
        //            }),

        //            RegisterError.RequestCancelled => StatusCode(499), // Client Closed Request

        //            _ => Problem(
        //                                                   statusCode: 500,
        //                                                   title: "Erreur serveur",
        //                                                   detail: "Une erreur inattendue s'est produite."
        //                                               )
        //        };
        //    }

        //    return CreatedAtAction(
        //        actionName: nameof(Register),
        //        value: new RegisterResponse(result.user!.Id, result.user.Email)
        //    );
        //}


        // -------------------------
        // POST /token
        // Authentification → retourne le JWT
        // -------------------------
        [HttpPost("token")]
        [ProducesResponseType(typeof(Token), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> Login([FromBody] LoginRequest data)
        {
            var user = await _userService.Authenticate(data.Email, data.Password);

            if (user is null)
                return Unauthorized(new { detail = "Email ou mot de passe incorrect" });

            if (!user.IsActive)
                return BadRequest(new { detail = "Compte désactivé" });

            var token = _tokenService.CreateAccessToken(user);
            return Ok(new Token { AccessToken = token });
        }
    }
}

