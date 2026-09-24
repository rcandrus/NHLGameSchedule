namespace NHLGameSchedule.Models;

public record NhlGame(
    string HomeTeam,
    string AwayTeam,
    DateTime GameDate,
    string Arena,
    int? HomeScore,
    int? AwayScore,
    bool IsFinal,
    int GameId,
    string GameCenterLink);
