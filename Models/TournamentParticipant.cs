namespace TournamentGraphQLApi.Models;

public class TournamentParticipant
{
    public int TournamentId { get; set; }
    public Tournament? Tournament { get; set; }

    public int UserId { get; set; }
    public User? User { get; set; }
}
