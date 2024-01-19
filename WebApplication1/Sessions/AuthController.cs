using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Users;

namespace WebApplication1.Sessions
{
    [ApiController]
    [Route("auth")]
    public class AuthController : Controller
    {
        private readonly DatabaseContext _context;
        private readonly IMapper _mapper;
        public AuthController(DatabaseContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }
        /// <summary>
        /// Авторизация пользователя
        /// </summary>
        /// <patam name="data">Почта и пароль</patam>
        /// <param name="userAgent">Имя клиента</param>
        /// <response code="200">Авторизация успешна</response>
        /// <response code="404">Пользователь не найден</response>
        /// <respone code="400">Некорректный запрос</respone>
        [HttpPost]
        [AllowAnonymous]
        [ProducesResponseType(typeof(TokenDTO), 200)]
        [ProducesResponseType(404)]
        [ProducesResponseType(400)]
        public async Task<ActionResult<TokenDTO>> Login([FromBody] LoginDTO data, [FromHeader(Name = "User-Agent")] string userAgent)
        {
            if (ModelState.IsValid)
            {
                var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == data.Email && u.Password == data.Password);
                if (user == null) return NotFound();

                SessionEntity session = new()
                {
                    UserAgent = userAgent,
                    User = user,
                };

                SessionTokenEntity token = new()
                {
                    Session = session,
                    Token = Guid.NewGuid(),
                    Type = SessionTokenType.Access
                };
                ///Добавляем в БД только токен т.к. сессия уже вложена в него
                _context.Add(token);

                await _context.SaveChangesAsync();

                return _mapper.Map<TokenDTO>(token);
            }
            return BadRequest(ModelState);
        }

        /// <summary>
        /// Возвращает текущего пользователя
        /// </summary>
        /// <response code="200">Текущий пользователь.</response>
        /// <response code="403">Ошибка авторизации.</response>
        /// <response code="400">Некорректный запрос.</response>
        [HttpGet]
        [Authorize]
        [ProducesResponseType(typeof(AccountDTO), 200)]
        [ProducesResponseType(403)]
        [ProducesResponseType(400)]
        public async Task<ActionResult<AccountDTO>> CurrentAccount()
        {
            if (User.Identity?.Name == null) return Forbid();

            int userId = int.Parse(User.Identity.Name);
            var user = await _context.Users.FindAsync(userId);

            if (user != null)
            {
                return new AccountDTO() { User = _mapper.Map<UserDTO>(user) };
            }

            return BadRequest();
        }
        /// <summary>
        /// Завершение сессии
        /// </summary>
        /// <response code="200">Выход выполнен.</response>
        /// <response code="400">Некорректный запрос.</response>
        [HttpDelete]
        [Authorize]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        public async Task<ActionResult> Logout()
        {
            string? token = Request.Headers[AuthenticationHandlerOptions.DefaultHeader].First()?.Split(" ").Last();

            if (token == null) return BadRequest();

            var sessionToken = await _context.SessionTokens
                .Include(t => t.Session)
                .FirstOrDefaultAsync(t => t.Token == Guid.Parse(token) && t.Type == SessionTokenType.Access);

            if (sessionToken != null)
            {
                _context.Remove(sessionToken.Session);
                await _context.SaveChangesAsync();
                return Ok();
            }
            return BadRequest();
        }

        /// <summary>
        /// Регистрация
        /// </summary>
        /// <response code="200">Регистрация выполнена.</response>
        /// <response code="400">Некорректный запрос.</response>
        [HttpPost("register")]
        public async Task<ActionResult<TokenDTO>> Login([FromBody] UserCreateDTO user, [FromHeader(Name = "User-Agent")] string userAgent)
        {
            if (ModelState.IsValid)
            {

                var created = new UserEntity
                {
                    Name = user.Name,
                    Family = user.Family,
                    Patronymic = user.Patronymic,
                    Email = user.Email,
                    Password = user.Password
                };

                _context.Add(created);
                await _context.SaveChangesAsync();

                SessionEntity session = new()
                {
                    UserAgent = userAgent,
                    User = created,
                };

                SessionTokenEntity token = new()
                {
                    Session = session,
                    Token = Guid.NewGuid(),
                    Type = SessionTokenType.Access
                };
                ///Добавляем в БД только токен т.к. сессия уже вложена в него
                _context.Add(token);

                await _context.SaveChangesAsync();

                return _mapper.Map<TokenDTO>(token);
            }

            return BadRequest();
        }

    }
}
