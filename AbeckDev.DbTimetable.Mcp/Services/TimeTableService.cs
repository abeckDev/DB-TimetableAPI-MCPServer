using System;
using AbeckDev.DbTimetable.Mcp.Models;
using Microsoft.Extensions.Options;

namespace AbeckDev.DbTimetable.Mcp.Services;

public class TimeTableService : ITimeTableService
{

    private readonly HttpClient _httpClient;
    private readonly Configuration _config;

    public TimeTableService(HttpClient httpClient, IOptions<Configuration> config)
    {
        _httpClient = httpClient;
        _config = config.Value;
    }

    /// <summary>
    /// Get recent timetable changes for a specific event number. Recent changes are always a subset of the full changes. They may equal full changes but are typically much smaller. 
    /// Data includes only those changes that became known within the last 2 minutes.
    /// </summary>
    public async Task<string> GetRecentTimetableChangesAsync(
        string eventNo, 
        CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, $"rchg/{eventNo}");
        request.Headers.Add("DB-Client-Id", _config.ClientId);
        request.Headers.Add("DB-Api-Key", _config.ApiKey);
        request.Headers.Add("accept", "application/xml");
        
        var response = await _httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();
        
        var content = await response.Content.ReadAsStringAsync(cancellationToken);
        return content;
    }

    /// <summary>
    /// Get station board (departures/arrivals) for a specific station in hourly slices. 
    /// </summary>
    public async Task<string> GetStationBoardAsync(
        string evaNo,
        DateTime? date = null,
        CancellationToken cancellationToken = default)
    {
        // Format: yyMMddHHmm (e.g., 2511051830 for 2025-11-05 18:30)
        var dateParam = date?.ToString("yyMMdd") ?? DateTime.UtcNow.ToString("yyMMdd");
        var hourParam = date?.ToString("HH") ?? DateTime.UtcNow.ToString("HH");

        using var request = new HttpRequestMessage(HttpMethod.Get, $"plan/{evaNo}/{dateParam}/{hourParam}");
        request.Headers.Add("DB-Client-Id", _config.ClientId);
        request.Headers.Add("DB-Api-Key", _config.ApiKey);
        request.Headers.Add("accept", "application/xml");

        var response = await _httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadAsStringAsync(cancellationToken);
    }

    /// <summary>
    /// Returns a Timetable object (see Timetable) that contains all known changes for the station given by evaNo.
    /// The data includes all known changes from now on until ndefinitely into the future. Once changes become obsolete (because their trip departs from the station) they are removed from this resource
    /// </summary>
    public async Task<string> GetFullChangesAsync(
        string evaNo,
        CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, $"fchg/{evaNo}");
        request.Headers.Add("DB-Client-Id", _config.ClientId);
        request.Headers.Add("DB-Api-Key", _config.ApiKey);
        request.Headers.Add("accept", "application/xml");

        var response = await _httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadAsStringAsync(cancellationToken);
    }
    
    /// <summary>
    /// Get information about stations given either a station name (prefix), eva number, ds100/rl100 code, wildcard (*); doesn't seem to work with umlauten in station name (prefix)
    /// </summary>
    public async Task<string> GetStationInformation(
        string pattern,
        CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, $"station/{pattern}");
        request.Headers.Add("DB-Client-Id", _config.ClientId);
        request.Headers.Add("DB-Api-Key", _config.ApiKey);
        request.Headers.Add("accept", "application/xml");
        
        var response = await _httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();
        
        return await response.Content.ReadAsStringAsync(cancellationToken);
    }

}
