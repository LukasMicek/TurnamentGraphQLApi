using HotChocolate.Authorization;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using TournamentGraphQLApi.Auth;
using TournamentGraphQLApi.Data;
using TournamentGraphQLApi.Models;

namespace TournamentGraphQLApi.GraphQL;

public class Mutation
{

    public async Task<AuthPayload> Register(
        RegisterInput input,
        [Service] AppDbContext db,
        [Service] JwtTokenService jwt)
    {
        if (string.IsNullOrWhiteSpace(input.FirstName) ||
            string.IsNullOrWhiteSpace(input.LastName) ||
            string.IsNullOrWhiteSpace(input.Email) ||
            string.IsNullOrWhiteSpace(input.Password))
            throw new GraphQLException("Invalid input.");

        var email = input.Email.Trim().ToLowerInvariant();

        if (await db.Users.AnyAsync(u => u.Email == email))
            throw new GraphQLException("Email already exists.");

        var user = new User
        {
            FirstName = input.FirstName.Trim(),
            LastName = input.LastName.Trim(),
            Email = email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(input.Password)
        };

        db.Users.Add(user);
        await db.SaveChangesAsync();

        var token = jwt.CreateToken(user);
        return new AuthPayload(token, user);
    }

    public async Task<AuthPayload> Login(
        LoginInput input,
        [Service] AppDbContext db,
        [Service] JwtTokenService jwt)
    {
        var email = input.Email.Trim().ToLowerInvariant();
        var user = await db.Users.FirstOrDefaultAsync(u => u.Email == email);
        if (user is null) throw new GraphQLException("Invalid credentials.");

        if (!BCrypt.Net.BCrypt.Verify(input.Password, user.PasswordHash))
            throw new GraphQLException("Invalid credentials.");

        var token = jwt.CreateToken(user);
        return new AuthPayload(token, user);
    }


    public async Task<Tournament> CreateTournament(
        string name,
        DateTime startDate,
        [Service] AppDbContext db)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new GraphQLException("Name is required.");

        var t = new Tournament
        {
            Name = name.Trim(),
            StartDate = startDate,
            Status = "Draft"
        };

        db.Tournaments.Add(t);
        await db.SaveChangesAsync();
        return t;
    }


    public async Task<Tournament> AddParticipant(
        int tournamentId,
        int userId,
        [Service] AppDbContext db)
    {
        var t = await db.Tournaments
            .Include(x => x.Participants)
            .FirstOrDefaultAsync(x => x.Id == tournamentId);

        if (t is null) throw new GraphQLException("Tournament not found.");

        var u = await db.Users.FirstOrDefaultAsync(x => x.Id == userId);
        if (u is null) throw new GraphQLException("User not found.");

        var exists = await db.TournamentParticipants.AnyAsync(tp => tp.TournamentId == tournamentId && tp.UserId == userId);
        if (!exists)
        {
            db.TournamentParticipants.Add(new TournamentParticipant { TournamentId = tournamentId, UserId = userId });
            await db.SaveChangesAsync();
        }

        return t;
    }

    public async Task<Tournament> StartTournament(int tournamentId, [Service] AppDbContext db)
    {
        var t = await db.Tournaments.FirstOrDefaultAsync(x => x.Id == tournamentId);
        if (t is null) throw new GraphQLException("Tournament not found.");

        t.Status = "Started";
        await db.SaveChangesAsync();
        return t;
    }


    public async Task<Tournament> FinishTournament(int tournamentId, [Service] AppDbContext db)
    {
        var t = await db.Tournaments.FirstOrDefaultAsync(x => x.Id == tournamentId);
        if (t is null) throw new GraphQLException("Tournament not found.");

        t.Status = "Finished";
        await db.SaveChangesAsync();
        return t;
    }

    public async Task<Bracket> GenerateBracket(int tournamentId, [Service] AppDbContext db)
    {
        var t = await db.Tournaments
            .Include(x => x.Bracket)
            .FirstOrDefaultAsync(x => x.Id == tournamentId);

        if (t is null) throw new GraphQLException("Tournament not found.");

        var participants = await db.TournamentParticipants
            .Where(tp => tp.TournamentId == tournamentId)
            .Select(tp => tp.UserId)
            .ToListAsync();

        if (participants.Count < 2)
            throw new GraphQLException("At least 2 participants required.");

        Bracket bracket;
        if (t.Bracket is null)
        {
            bracket = new Bracket { TournamentId = tournamentId };
            db.Brackets.Add(bracket);
            await db.SaveChangesAsync();
        }
        else
        {
            bracket = t.Bracket;
            var old = db.Matches.Where(m => m.BracketId == bracket.Id);
            db.Matches.RemoveRange(old);
            await db.SaveChangesAsync();
        }


        var matches = new List<Match>();
        var round = 1;

        for (int i = 0; i < participants.Count; i += 2)
        {
            var p1 = participants[i];
            int? p2 = (i + 1 < participants.Count) ? participants[i + 1] : null;

            var m = new Match
            {
                BracketId = bracket.Id,
                Round = round,
                Player1Id = p1,
                Player2Id = p2,
                WinnerId = (p2 is null) ? p1 : null
            };

            matches.Add(m);
        }

        db.Matches.AddRange(matches);
        await db.SaveChangesAsync();

        return bracket;
    }

    public async Task<List<Match>> GetMatchesForRound(
        int tournamentId,
        int round,
        [Service] AppDbContext db)
    {
        var bracket = await db.Brackets.FirstOrDefaultAsync(b => b.TournamentId == tournamentId);
        if (bracket is null) throw new GraphQLException("Bracket not found.");

        return await db.Matches
            .Include(m => m.Player1)
            .Include(m => m.Player2)
            .Include(m => m.Winner)
            .Where(m => m.BracketId == bracket.Id && m.Round == round)
            .OrderBy(m => m.Id)
            .ToListAsync();
    }

    public async Task<Match> PlayMatch(
        int matchId,
        int winnerUserId,
        [Service] AppDbContext db)
    {
        var m = await db.Matches.FirstOrDefaultAsync(x => x.Id == matchId);
        if (m is null) throw new GraphQLException("Match not found.");

        if (m.Player1Id != winnerUserId && m.Player2Id != winnerUserId)
            throw new GraphQLException("Winner must be player1 or player2.");

        m.WinnerId = winnerUserId;
        await db.SaveChangesAsync();

        return await db.Matches
            .Include(x => x.Player1)
            .Include(x => x.Player2)
            .Include(x => x.Winner)
            .FirstAsync(x => x.Id == matchId);
    }
}
