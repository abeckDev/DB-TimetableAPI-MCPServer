namespace AbeckDev.DbTimetable.Mcp.Services;

public interface ITimeTableService
{
    /// <summary>
    /// Get recent timetable changes for a specific event number
    /// </summary>
    Task<string> GetRecentTimetableChangesAsync(string eventNo, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get station board (departures/arrivals) for a specific station
    /// </summary>
    Task<string> GetStationBoardAsync(string evaNo, DateTime? date = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get full changes for a station at a specific time
    /// </summary>
    Task<string> GetFullChangesAsync(string evaNo, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get information about stations given either a station name (prefix), eva number, ds100/rl100 code, wildcard (*); doesn't seem to work with umlauten in station name (prefix)
    /// </summary>
    Task<string> GetStationInformation(string pattern, CancellationToken cancellationToken = default);
}
