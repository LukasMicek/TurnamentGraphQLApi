namespace TournamentGraphQLApi.Models;

public class Tournament
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public DateTime StartDate { get; set; }
    public string Status { get; set; } = "Draft";

    public Bracket? Bracket { get; set; }
    public List<TournamentParticipant> Participants { get; set; } = new();
}
