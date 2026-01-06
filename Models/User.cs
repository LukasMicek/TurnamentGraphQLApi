namespace TournamentGraphQLApi.Models;

public class User
{
    public int Id { get; set; }
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";
    public string Email { get; set; } = "";

    // hash hasła
    public string PasswordHash { get; set; } = "";

    public List<TournamentParticipant> TournamentParticipants { get; set; } = new();
}
