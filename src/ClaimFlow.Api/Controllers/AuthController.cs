using ClaimFlow.Api.Contracts.Auth;
using ClaimFlow.Api.Data;
using ClaimFlow.Api.Security;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace ClaimFlow.Api.Controllers;

[ApiController]
[Route("api/auth")]
[EnableRateLimiting("login")]
public class AuthController(ClaimFlowDbContext db, JwtTokenService tokenService, IOptions<JwtOptions> jwtOptions) : ControllerBase
{
    [HttpPost("login")]
    [ProducesResponseType<LoginResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<LoginResponse>> Login(LoginRequest request, CancellationToken cancellationToken)
    {
        var user = await db.Users.SingleOrDefaultAsync(u => u.Email == request.Email, cancellationToken);
        if (user is null || !PasswordHasher.Verify(request.Password, user.PasswordHash, user.PasswordSalt))
        {
            return Unauthorized(new ProblemDetails
            {
                Title = "Invalid credentials",
                Status = StatusCodes.Status401Unauthorized,
                Detail = "Email or password is incorrect."
            });
        }

        var nowUtc = DateTime.UtcNow;
        var token = tokenService.IssueToken(user, nowUtc);
        var expiresAtUtc = nowUtc.AddMinutes(jwtOptions.Value.AccessTokenMinutes);

        return Ok(new LoginResponse(token, expiresAtUtc, user.DisplayName, user.Role.ToString()));
    }
}
