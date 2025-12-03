namespace Bogar.DAL
{
    public enum MatchStatus
    {
        Pending,
        InProgress,
        Completed,
        Failure
    }

    public class Match
    {
        public int Id { get; set; }

        public int LobbyId { get; set; }

        public int WhiteBotId { get; set; }

        public int BlackBotId { get; set; }

        public int? WinnerId { get; set; }

        public long StartTime { get; set; }
        public long? FinishTime { get; set; }

        public bool IsAutoWin { get; set; }

        public int ScoreWhite { get; set; }
        public int ScoreBlack { get; set; }

        public string? Moves { get; set; }

        public MatchStatus Status { get; set; }

        public virtual Lobby? Lobby { get; set; }
        public virtual User? WhiteBot { get; set; }
        public virtual User? BlackBot { get; set; }
        public virtual User? Winner { get; set; }
    }
}
