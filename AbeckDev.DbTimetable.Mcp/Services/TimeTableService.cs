using System;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;
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
        // If date is provided by user, use it directly (user is expected to provide German time)
        // If not provided, get current time in German timezone
        DateTime effectiveTime;
        if (date.HasValue)
        {
            effectiveTime = date.Value;
        }
        else
        {
            try
            {
                var tz = TimeZoneInfo.FindSystemTimeZoneById("Europe/Berlin");
                effectiveTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, tz);
            }
            catch (TimeZoneNotFoundException)
            {
                effectiveTime = DateTime.Now; // fallback to local time if timezone not found
            }
            catch (InvalidTimeZoneException)
            {
                effectiveTime = DateTime.Now; // fallback to local time if timezone invalid
            }
        }
        
        var dateParam = effectiveTime.ToString("yyMMdd");
        var hourParam = effectiveTime.ToString("HH");

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

    /// <summary>
    /// Find train connections between two stations and assess their current status including delays and disruptions.
    /// This method orchestrates multiple API calls to:
    /// 1. Resolve station names to EVA IDs
    /// 2. Get timetables for both stations
    /// 3. Find trains that stop at both stations
    /// 4. Check for delays and disruptions
    /// 5. Return ranked connection options
    /// </summary>
    public async Task<string> FindTrainConnectionsAsync(
        string stationA,
        string stationB,
        DateTime? dateTime = null,
        CancellationToken cancellationToken = default)
    {
        var result = new StringBuilder();
        result.AppendLine("=== Train Connection Analysis ===");
        result.AppendLine();

        try
        {
            // Step 1: Resolve station A
            result.AppendLine($"Step 1: Resolving station '{stationA}'...");
            var stationAInfo = await ResolveStationAsync(stationA, cancellationToken);
            if (stationAInfo == null)
            {
                return $"Error: Could not find station '{stationA}'. Please check the station name.";
            }
            result.AppendLine($"  ✓ Found: {stationAInfo.Name} (EVA: {stationAInfo.EvaNo})");
            result.AppendLine();

            // Step 2: Resolve station B
            result.AppendLine($"Step 2: Resolving station '{stationB}'...");
            var stationBInfo = await ResolveStationAsync(stationB, cancellationToken);
            if (stationBInfo == null)
            {
                return $"Error: Could not find station '{stationB}'. Please check the station name.";
            }
            result.AppendLine($"  ✓ Found: {stationBInfo.Name} (EVA: {stationBInfo.EvaNo})");
            result.AppendLine();

            // Step 3: Get timetable for station A
            result.AppendLine($"Step 3: Fetching departures from {stationAInfo.Name}...");
            var timetableA = await GetStationBoardAsync(stationAInfo.EvaNo, dateTime, cancellationToken);
            result.AppendLine("  ✓ Timetable retrieved");
            result.AppendLine();

            // Step 4: Get full changes for station A to check for delays/disruptions
            result.AppendLine($"Step 4: Checking for delays and disruptions at {stationAInfo.Name}...");
            string changesA;
            try
            {
                changesA = await GetFullChangesAsync(stationAInfo.EvaNo, cancellationToken);
                result.AppendLine("  ✓ Full changes retrieved");
            }
            catch
            {
                changesA = string.Empty;
                result.AppendLine("  ⚠ No changes available");
            }
            result.AppendLine();

            // Step 5: Analyze connections
            result.AppendLine($"Step 5: Finding trains from {stationAInfo.Name} to {stationBInfo.Name}...");
            var connections = FindConnectionsInTimetable(timetableA, changesA, stationAInfo, stationBInfo);
            
            if (connections.Count == 0)
            {
                result.AppendLine("  ⚠ No direct connections found in the current timetable.");
                result.AppendLine();
                result.AppendLine("This could mean:");
                result.AppendLine("- No direct trains operate between these stations");
                result.AppendLine("- Trains may require a transfer");
                result.AppendLine("- Try a different time or date");
            }
            else
            {
                result.AppendLine($"  ✓ Found {connections.Count} connection(s)");
                result.AppendLine();
                result.AppendLine("=== Available Connections ===");
                result.AppendLine();

                int rank = 1;
                foreach (var conn in connections.OrderBy(c => c.TotalDelay).ThenBy(c => c.DepartureTime))
                {
                    result.AppendLine($"Option {rank}: {conn.TrainType} {conn.TrainNumber}");
                    result.AppendLine($"  Departure: {conn.DepartureTime:HH:mm} from {stationAInfo.Name}");
                    result.AppendLine($"  Platform: {conn.DeparturePlatform ?? "TBA"}");
                    
                    if (conn.ScheduledDepartureTime.HasValue && conn.DepartureTime != conn.ScheduledDepartureTime)
                    {
                        result.AppendLine($"  ⚠ Originally scheduled: {conn.ScheduledDepartureTime:HH:mm}");
                    }

                    if (conn.TotalDelay > 0)
                    {
                        result.AppendLine($"  ⚠ Delay: +{conn.TotalDelay} minutes");
                    }
                    else
                    {
                        result.AppendLine($"  ✓ On time");
                    }

                    if (!string.IsNullOrEmpty(conn.Messages))
                    {
                        result.AppendLine($"  Messages: {conn.Messages}");
                    }

                    if (conn.IsCancelled)
                    {
                        result.AppendLine($"  ❌ CANCELLED");
                    }

                    result.AppendLine($"  Destination: {conn.FinalDestination}");
                    result.AppendLine();
                    rank++;
                }

                // Recommendation
                result.AppendLine("=== Recommendation ===");
                var bestConnection = connections.OrderBy(c => c.IsCancelled ? 1 : 0)
                    .ThenBy(c => c.TotalDelay)
                    .ThenBy(c => c.DepartureTime)
                    .First();

                if (bestConnection.IsCancelled)
                {
                    result.AppendLine("⚠ The earliest connection is cancelled. Check alternative options above.");
                }
                else if (bestConnection.TotalDelay == 0)
                {
                    result.AppendLine($"✓ Best option: {bestConnection.TrainType} {bestConnection.TrainNumber} at {bestConnection.DepartureTime:HH:mm} - On time");
                }
                else
                {
                    result.AppendLine($"⚠ Best option: {bestConnection.TrainType} {bestConnection.TrainNumber} at {bestConnection.DepartureTime:HH:mm} - Delayed by {bestConnection.TotalDelay} minutes");
                }
            }

            return result.ToString();
        }
        catch (Exception ex)
        {
            result.AppendLine();
            result.AppendLine($"Error during connection analysis: {ex.Message}");
            return result.ToString();
        }
    }

    /// <summary>
    /// Helper to resolve a station name or EVA number to station information
    /// </summary>
    private async Task<StationInfo?> ResolveStationAsync(string pattern, CancellationToken cancellationToken)
    {
        try
        {
            var stationXml = await GetStationInformation(pattern, cancellationToken);
            return ParseFirstStation(stationXml);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Parse the first station from the station information XML
    /// </summary>
    private StationInfo? ParseFirstStation(string xml)
    {
        try
        {
            var doc = XDocument.Parse(xml);
            var station = doc.Descendants("station").FirstOrDefault();
            if (station == null) return null;

            return new StationInfo
            {
                Name = station.Attribute("name")?.Value ?? "",
                EvaNo = station.Attribute("eva")?.Value ?? ""
            };
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Find connections in the timetable that potentially go through both stations
    /// </summary>
    private List<ConnectionInfo> FindConnectionsInTimetable(string timetableXml, string changesXml, StationInfo stationA, StationInfo stationB)
    {
        var connections = new List<ConnectionInfo>();

        try
        {
            var timetableDoc = XDocument.Parse(timetableXml);
            XDocument? changesDoc = null;
            try
            {
                if (!string.IsNullOrEmpty(changesXml))
                {
                    changesDoc = XDocument.Parse(changesXml);
                }
            }
            catch { }

            // Parse all train events (stops) from the timetable
            var stops = timetableDoc.Descendants("s").ToList();

            foreach (var stop in stops)
            {
                // Check if this train goes to the destination by looking at the path (ppth) or final destination
                // The ppth attribute is on the dp (departure) element, not on the s (stop) element
                var departureElement = stop.Element("dp");
                var path = departureElement?.Attribute("ppth")?.Value ?? "";
                var destination = stop.Element("tl")?.Attribute("f")?.Value ?? 
                                 path.Split('|').LastOrDefault() ?? "";

                // Check if the destination station or path contains our target station
                // This is a heuristic - the actual route might not be fully represented
                // We use the first word of the station name for matching to handle complex station names
                var pathStations = path.Split('|').Select(s => s.Trim()).ToList();
                var stationBFirstWord = GetFirstWord(stationB.Name);
                
                var goesToDestination = pathStations.Any(ps => 
                    ps.Equals(stationB.Name, StringComparison.OrdinalIgnoreCase) ||
                    (!string.IsNullOrEmpty(stationBFirstWord) && ps.Contains(stationBFirstWord, StringComparison.OrdinalIgnoreCase)));

                if (!goesToDestination && 
                    (string.IsNullOrEmpty(stationBFirstWord) || !destination.Contains(stationBFirstWord, StringComparison.OrdinalIgnoreCase)))
                {
                    continue; // Skip trains that don't go to our destination
                }

                var trainId = stop.Attribute("id")?.Value ?? "";
                var trainElement = stop.Element("tl");
                var trainType = trainElement?.Attribute("c")?.Value ?? "";
                var trainNumber = trainElement?.Attribute("n")?.Value ?? "";

                if (departureElement == null) continue; // Only interested in departures

                var scheduledDeparture = departureElement.Attribute("pt")?.Value;
                var actualDeparture = departureElement.Attribute("ct")?.Value ?? scheduledDeparture;
                var platform = departureElement.Attribute("pp")?.Value;
                var changedPlatform = departureElement.Attribute("cp")?.Value;

                if (string.IsNullOrEmpty(actualDeparture)) continue;

                // Parse departure times
                var departureTime = ParseTimetableDateTime(actualDeparture);
                var scheduledDepartureTime = ParseTimetableDateTime(scheduledDeparture);

                // Look up live changes for this specific train from the full changes data
                int delay = 0;
                bool isCancelled = departureElement.Attribute("cs")?.Value == "c";
                string messages = string.Join("; ", stop.Elements("m")
                    .Select(m => m.Attribute("t")?.Value)
                    .Where(m => !string.IsNullOrEmpty(m)));

                // If we have changes data, look for this specific train and extract live information
                if (changesDoc != null)
                {
                    var changeStop = changesDoc.Descendants("s")
                        .FirstOrDefault(s => s.Attribute("id")?.Value == trainId);

                    if (changeStop != null)
                    {
                        var changeDp = changeStop.Element("dp");
                        if (changeDp != null)
                        {
                            // Get live departure time from changes
                            var liveActualDeparture = changeDp.Attribute("ct")?.Value;
                            var liveScheduledDeparture = changeDp.Attribute("pt")?.Value;
                            
                            if (!string.IsNullOrEmpty(liveActualDeparture) && !string.IsNullOrEmpty(liveScheduledDeparture))
                            {
                                var liveDepartureTime = ParseTimetableDateTime(liveActualDeparture);
                                var liveScheduledTime = ParseTimetableDateTime(liveScheduledDeparture);
                                
                                if (liveDepartureTime.HasValue && liveScheduledTime.HasValue)
                                {
                                    delay = (int)(liveDepartureTime.Value - liveScheduledTime.Value).TotalMinutes;
                                    departureTime = liveDepartureTime; // Use live time
                                }
                            }

                            // Check for cancellation status in changes
                            var liveStatus = changeDp.Attribute("cs")?.Value;
                            if (liveStatus == "c")
                            {
                                isCancelled = true;
                            }

                            // Get changed platform if available
                            var liveChangedPlatform = changeDp.Attribute("cp")?.Value;
                            if (!string.IsNullOrEmpty(liveChangedPlatform))
                            {
                                changedPlatform = liveChangedPlatform;
                            }
                        }

                        // Get messages from changes (these are more up-to-date)
                        var changeMessages = string.Join("; ", changeStop.Elements("m")
                            .Select(m => m.Attribute("t")?.Value)
                            .Where(m => !string.IsNullOrEmpty(m)));
                        
                        if (!string.IsNullOrEmpty(changeMessages))
                        {
                            messages = changeMessages;
                        }
                    }
                }
                // Fallback: if no changes data, calculate delay from timetable data
                else if (scheduledDepartureTime.HasValue && departureTime.HasValue)
                {
                    delay = (int)(departureTime.Value - scheduledDepartureTime.Value).TotalMinutes;
                }

                connections.Add(new ConnectionInfo
                {
                    TrainId = trainId,
                    TrainType = trainType,
                    TrainNumber = trainNumber,
                    DepartureTime = departureTime ?? DateTime.MinValue,
                    ScheduledDepartureTime = scheduledDepartureTime,
                    DeparturePlatform = changedPlatform ?? platform,
                    TotalDelay = delay,
                    IsCancelled = isCancelled,
                    Messages = messages,
                    FinalDestination = destination
                });
            }
        }
        catch
        {
            // If parsing fails, return empty list
        }

        return connections;
    }

    /// <summary>
    /// Parse Deutsche Bahn timetable date format (YYMMddHHmm)
    /// Example: "2511061430" represents 2025-11-06 14:30 (YY=25, MM=11, dd=06, HH=14, mm=30)
    /// </summary>
    private DateTime? ParseTimetableDateTime(string? dateTimeStr)
    {
        if (string.IsNullOrEmpty(dateTimeStr)) return null;

        try
        {
            // Format: YYMMddHHmm (e.g., "2511061430" for 2025-11-06 14:30)
            if (dateTimeStr.Length == 10)
            {
                var year = 2000 + int.Parse(dateTimeStr.Substring(0, 2));
                var month = int.Parse(dateTimeStr.Substring(2, 2));
                var day = int.Parse(dateTimeStr.Substring(4, 2));
                var hour = int.Parse(dateTimeStr.Substring(6, 2));
                var minute = int.Parse(dateTimeStr.Substring(8, 2));

                return new DateTime(year, month, day, hour, minute, 0);
            }
        }
        catch { }

        return null;
    }

    /// <summary>
    /// Helper method to safely get the first word from a station name
    /// Returns empty string if the input is null or empty
    /// </summary>
    private string GetFirstWord(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return "";
        
        var words = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return words.Length > 0 ? words[0] : "";
    }

    /// <summary>
    /// Station information
    /// </summary>
    private class StationInfo
    {
        public string Name { get; set; } = "";
        public string EvaNo { get; set; } = "";
    }

    /// <summary>
    /// Connection information
    /// </summary>
    private class ConnectionInfo
    {
        public string TrainId { get; set; } = "";
        public string TrainType { get; set; } = "";
        public string TrainNumber { get; set; } = "";
        public DateTime DepartureTime { get; set; }
        public DateTime? ScheduledDepartureTime { get; set; }
        public string? DeparturePlatform { get; set; }
        public int TotalDelay { get; set; }
        public bool IsCancelled { get; set; }
        public string Messages { get; set; } = "";
        public string FinalDestination { get; set; } = "";
    }

}
