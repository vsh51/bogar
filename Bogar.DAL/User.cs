namespace Bogar.DAL
{
    public class User
    {
        public int Id { get; set; }
        public string? Username { get; set; }
        public string? BotName { get; set; }
        public string? BotFileHash { get; set; }

        public int LobbyId { get; set; }

        public virtual Lobby? Lobby { get; set; }

        public virtual ICollection<Match> MatchesAsWhite { get; set; } = new List<Match>();
        public virtual ICollection<Match> MatchesAsBlack { get; set; } = new List<Match>();
        public virtual ICollection<Match> WonMatches { get; set; } = new List<Match>();
    }
}
