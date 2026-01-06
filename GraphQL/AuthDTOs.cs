using TournamentGraphQLApi.Models;

namespace TournamentGraphQLApi.GraphQL;

public record RegisterInput(string FirstName, string LastName, string Email, string Password);
public record LoginInput(string Email, string Password);

public record AuthPayload(string Token, User User);
