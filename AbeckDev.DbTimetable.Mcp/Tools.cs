using System;
using System.ComponentModel;
using AbeckDev.DbTimetable.Mcp.Services;
using ModelContextProtocol.Server;


namespace AbeckDev.DbTimetable.Mcp;

[McpServerToolType]
public static class Tools
{
    /// <summary>
    /// MCP Server tools for accessing Deutsche Bahn Timetable API
    /// </summary>
    [McpServerToolType]
    public class TimetableTools
    {
        private readonly TimeTableService _timeTableService;

        public TimetableTools(TimeTableService timeTableService)
        {
            _timeTableService = timeTableService;
        }

        [McpServerTool]
        [Description("Get full timetable changes for a specific train event. The data includes all known changes from now on until indefinitely into the future. Once changes become obsolete (because their trip departs from the station) they are removed.")]
        public async Task<string> GetFullTimetableChanges(
            [Description("Event number (EVA number) of the train event")] string eventNo)
        {
            try
            {
                var result = await _timeTableService.GetFullChangesAsync(eventNo);
                return result;
            }
            catch (HttpRequestException ex)
            {
                return $"Error fetching timetable changes: {ex.Message}";
            }
            catch (Exception ex)
            {
                return $"Unexpected error: {ex.Message}";
            }
        }

        [McpServerTool]
        [Description("Get station board (departures and arrivals) for a specific station. Returns XML data with train schedules.")]
        public async Task<string> GetStationBoard(
            [Description("EVA station number (e.g., 8000105 for Frankfurt Hauptbahnhof)")] string evaNo,
            [Description("Date and time in format 'yyyy-MM-dd HH:mm' (UTC). Leave empty for current time.")] string? dateTime = null)
        {
            try
            {
                DateTime? parsedDate = null;
                if (!string.IsNullOrEmpty(dateTime))
                {
                    if (DateTime.TryParse(dateTime, out var dt))
                    {
                        parsedDate = dt;
                    }
                    else
                    {
                        return "Error: Invalid date format. Please use 'yyyy-MM-dd HH:mm' format.";
                    }
                }

                var result = await _timeTableService.GetStationBoardAsync(evaNo, parsedDate);
                return result;
            }
            catch (HttpRequestException ex)
            {
                return $"Error fetching station board: {ex.Message}";
            }
            catch (Exception ex)
            {
                return $"Unexpected error: {ex.Message}";
            }
        }

        [McpServerTool]
        [Description("Get all current changes (delays, cancellations, platform changes) for a specific station. Recent changes are always a subset of the full changes. They may equal full changes but are typically much smaller. Data includes only those changes that became known within the last 2 minutes.")]
        public async Task<string> GetStationChanges([Description("EVA station number (e.g., 8000105 for Frankfurt Hauptbahnhof)")] string evaNo)
        {
            try
            {
                var result = await _timeTableService.GetRecentTimetableChangesAsync(evaNo);
                return result;
            }
            catch (HttpRequestException ex)
            {
                return $"Error fetching station changes: {ex.Message}";
            }
            catch (Exception ex)
            {
                return $"Unexpected error: {ex.Message}";
            }
        }

        [McpServerTool]
        [Description("Get information about stations.")]
        public async Task<string> GetStationDetails([Description("Either a station name (prefix), eva number, ds100/rl100 code, wildcard (*); doesn't seem to work with umlauten in station name (prefix). If unsure use the Station Name e.g. \"Dresden Hbf\" ")] string pattern)
        {
            try
            {
                var result = await _timeTableService.GetStationInformation(pattern);
                return result;
            }
            catch (HttpRequestException ex)
            {
                return $"Error fetching station Details: {ex.Message}";
            }
            catch (Exception ex)
            {
                return $"Unexpected error: {ex.Message}";
            }
        }
    }
}
