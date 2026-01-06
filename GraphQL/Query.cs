using HotChocolate.Authorization;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using TournamentGraphQLApi.Data;
using TournamentGraphQLApi.Models;

namespace TournamentGraphQLApi.GraphQL;

public class Query
{
    public IQueryable<Tournament> Tournaments([Service] AppDbContext db)
        => db.Tournaments;

    public IQueryable<User> Users([Service] AppDbContext db)
        => db.Users;

    public IQueryable<Match> Matches([Service] AppDbContext db)
        => db.Matches;

    [Authorize]
    public async Task<List<Match>> MyMatches(
        ClaimsPrincipal user,
        [Service] AppDbContext db)
    {
        var idStr = user.FindFirstValue(ClaimTypes.NameIdentifier)
                    ?? user.FindFirstValue(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub);

        if (!int.TryParse(idStr, out var userId))
            return new List<Match>();

        return await db.Matches
            .Include(m => m.Player1)
            .Include(m => m.Player2)
            .Include(m => m.Winner)
            .Where(m => m.Player1Id == userId || m.Player2Id == userId)
            .OrderBy(m => m.Round)
            .ToListAsync();
    }
}
