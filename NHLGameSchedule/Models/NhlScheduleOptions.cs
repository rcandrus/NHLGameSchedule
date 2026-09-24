namespace NHLGameSchedule.Models;

public sealed class NhlScheduleOptions
{
    public List<NhlTeamOptions> Teams { get; set; } = [];
}

public sealed class NhlTeamOptions
{
    public string Abbreviation { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
}
