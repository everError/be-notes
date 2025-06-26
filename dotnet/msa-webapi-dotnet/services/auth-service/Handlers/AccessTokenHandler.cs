using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace auth_service.Handlers;

public sealed class AccessTokenHandler
{
    private static readonly Lazy<AccessTokenHandler> _instance = new(() => new AccessTokenHandler());

    private readonly byte[] _key;

    public readonly TokenValidationParameters TokenValidationParameters;

    /// <summary>
    /// 외부에서 인스턴스를 직접 생성할 수 없도록 private 생성자 사용
    /// </summary>
    private AccessTokenHandler()
    {
        _key = Encoding.UTF8.GetBytes("your-super-secure-and-long-secret-key!");
        TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(_key),
            ValidateIssuer = false,
            ValidateAudience = false,
            ClockSkew = TimeSpan.Zero // 시간 차이 허용 없음
        };
    }

    /// <summary>
    /// 싱글톤 인스턴스 접근
    /// </summary>
    public static AccessTokenHandler Instance => _instance.Value;

    /// <summary>
    /// Access Token을 생성합니다.
    /// </summary>
    public string GenerateAccessToken(string username, int expiresInMinutes = 60)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(
            [
                new Claim(ClaimTypes.Name, username),
                new Claim(ClaimTypes.Role, "User") // 필요 시 Role 추가 가능
            ]),
            Expires = DateTime.UtcNow.AddMinutes(expiresInMinutes),
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(_key), SecurityAlgorithms.HmacSha256Signature)
        };

        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }
    /// <summary>
    /// Refresh Token을 생성 (고유하고 예측 불가능한 값 생성)
    /// </summary>
    public string GenerateRefreshToken()
    {
        var randomNumber = new byte[32]; // 256비트 (32바이트) 난수 생성
        using (var rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(randomNumber);
        }
        return Convert.ToBase64String(randomNumber); // Base64 인코딩하여 문자열로 변환
    }

    /// <summary>
    /// Access Token이 유효한지 검증합니다.
    /// </summary>
    public bool ValidateToken(string token)
    {
        try
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var principal = tokenHandler.ValidateToken(token, TokenValidationParameters, out _);
            return principal != null;
        }
        catch
        {
            return false;
        }
    }
}
