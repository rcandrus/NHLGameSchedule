using NHLGameSchedule.Models;

namespace NHLGameSchedule.Services;

public sealed class TeamSelectionState
{
    private readonly HashSet<string> selectedTeams = new(StringComparer.OrdinalIgnoreCase);

    public event Action? Changed;

    public IReadOnlySet<string> SelectedTeams => selectedTeams;

    public void Initialize(IEnumerable<NhlTeamOptions> teams)
    {
        if (selectedTeams.Count > 0)
        {
            return;
        }

        selectedTeams.UnionWith(teams.Select(team => team.Abbreviation));
    }

    public bool IsSelected(string abbreviation) => selectedTeams.Contains(abbreviation);

    public void Toggle(string abbreviation)
    {
        if (!selectedTeams.Remove(abbreviation))
        {
            selectedTeams.Add(abbreviation);
        }

        Changed?.Invoke();
    }
}
