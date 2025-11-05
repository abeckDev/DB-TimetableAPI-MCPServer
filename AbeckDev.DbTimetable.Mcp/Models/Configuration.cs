using System;

namespace AbeckDev.DbTimetable.Mcp.Models;

public class Configuration
{
    public const string SectionName = "DeutscheBahnApi";
    public string BaseUrl { get; set; } = "https://apis.deutschebahn.com/db-api-marketplace/apis/timetables/v1/";
    public string ClientId { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
}
