using Microsoft.EntityFrameworkCore;
using RoleClaimsApp.Models;

namespace RoleClaimsApp.Security;

public class RefreshTokenService
{
    private readonly int _refreshTokenDays;
    private readonly ApplicationDbContext _db;
    
    public RefreshTokenService(ApplicationDbContext db, IConfiguration config)
    {
        _db = db;
        _refreshTokenDays = config.GetValue<int?>("Jwt:RefreshTokenDays")
                            ?? throw new InvalidOperationException("Refresh token expiry not configured");
    }
    
    public async Task CreateInitialTokenAsync(
        string userId,
        string tokenHash
        )
    {
        var refreshToken = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            TokenHash = tokenHash,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddDays(_refreshTokenDays)
        };

        _db.RefreshTokens.Add(refreshToken);
        await _db.SaveChangesAsync();
    }
    
    public async Task<RefreshToken?> GetValidTokenAsync(string tokenHash)
    {
        var token = await _db.RefreshTokens
            .FirstOrDefaultAsync(t => t.TokenHash == tokenHash);

        if (token == null)
            return null;

        if (token.IsExpired || token.RevokedAt != null)
            return null;

        return token;
    }

    public async Task RevokeTokenAsync(RefreshToken token)
    {
        if (token.RevokedAt != null)
            return;

        token.RevokedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
    }

    public async Task RevokeAllTokensForUserAsync(string userId)
    {
        var tokens = await _db.RefreshTokens
            .Where(t => t.UserId == userId && t.RevokedAt == null)
            .ToListAsync();

        foreach (var token in tokens)
        {
            token.RevokedAt = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync();
    }

    public async Task<RefreshToken> RotateTokenAsync(
        RefreshToken oldToken,
        string newTokenHash)
    {
        oldToken.RevokedAt = DateTime.UtcNow;

        var newToken = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = oldToken.UserId,
            TokenHash = newTokenHash,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddDays(_refreshTokenDays),
            ReplacedByTokenId = oldToken.Id
        };

        _db.RefreshTokens.Add(newToken);
        await _db.SaveChangesAsync();

        return newToken;
    }

}