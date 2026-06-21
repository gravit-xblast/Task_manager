using Microsoft.AspNetCore.Authorization;
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
        [AllowAnonymous]
        [HttpPost("register")]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Register([FromBody] RegisterRequest data, UserStatus userStatus = UserStatus.Standard)
        {
            // Remarque : RegisterUser(data) appelle le service avec isAdmin = false
            // par défaut -> impossible de créer un compte admin via cet endpoint
            // public, ce qui est le comportement souhaité.
            var user = await _userService.RegisterUser(data, userStatus);

            if (user is null)
            {
                return StatusCode(400, new RegisterResponse
                {
                    Message = "Email déjà utilisé",
                    UserName = null,
                    Email = null
                });
            }

            return StatusCode(201, new RegisterResponse
            {
                Message = "Utilisateur créé avec succès",
                UserName = user.UserName,
                Email = user.Email,
                UserStatus = user.UserStatus
            });
        }
        // -------------------------
        // POST /token
        // Authentification → retourne le JWT
        // -------------------------
        [AllowAnonymous]
        [HttpPost("token")]
        [ProducesResponseType(typeof(Token), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Login([FromBody] LoginRequest data)
        {
            var user = await _userService.Authenticate(data.Email, data.Password);

            if (user is null)
                return Unauthorized(new { detail = "Email ou mot de passe incorrect" });

            if (!user.IsActive)
                return BadRequest(new { detail = "Compte désactivé" });

            var accessToken = _tokenService.CreateAccessToken(user);

            // access_token, token_type, expires_in
            return Ok(new Token
            {
                AccessToken = accessToken,
                TokenType = "Bearer",
                ExpiresIn = _tokenService.AccessTokenExpiresInSeconds
            });
        }
    }
}























//using Microsoft.AspNetCore.Mvc;
//using Task_Manager.Authentication;
//using Task_Manager.Models;

                                 
//namespace Task_Manager.Controllers
//{
//    [ApiController]
//    [Route("")]
//    public class AuthenticationController : ControllerBase
//    {
//        private readonly IUserService _userService;
//        private readonly ITokenService _tokenService;

//        public AuthenticationController(IUserService userService, ITokenService tokenService)
//        {
//            _userService = userService;
//            _tokenService = tokenService;
//        }

//        // -------------------------
//        // POST /register
//        // Accessible sans authentification
//        // -------------------------
//        [HttpPost("register")]
//        [ProducesResponseType(StatusCodes.Status201Created)]
//        [ProducesResponseType(StatusCodes.Status400BadRequest)]
//        public async Task<IActionResult> Register([FromBody] RegisterRequest data)
//        {
//            var user = await _userService.RegisterUser(data);
//            var response = new RegisterResponse();

//            if (user is null)
//            {
//                response.Message = "Email déjà utilisé";
//                response.UserName = null;
//                response.Email = null;

//                return StatusCode(400, response); //vérifier status code
//            }

//            response.Message = "Utilisateur créé avec succès";
//            response.UserName = user?.UserName; // user!.Id ??? syntaxe 
//            response.Email = user?.Email;

//            return StatusCode(201, response);
//        }


//        // -------------------------
//        // POST /token
//        // Authentification → retourne le JWT
//        // -------------------------
//        [HttpPost("token")]
//        [ProducesResponseType(typeof(Token), StatusCodes.Status200OK)]
//        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
//        public async Task<IActionResult> Login([FromBody] RegisterRequest data)
//        {
//            var user = await _userService.Authenticate(data.Email, data.Password);

//            if (user is null)
//                return Unauthorized(new { detail = "Email ou mot de passe incorrect" });

//            if (!user.IsActive)
//                return BadRequest(new { detail = "Compte désactivé" });

//            var token = _tokenService.CreateAccessToken(user);
//            return Ok(new Token { AccessToken = token });
//        }
//    }
//}

