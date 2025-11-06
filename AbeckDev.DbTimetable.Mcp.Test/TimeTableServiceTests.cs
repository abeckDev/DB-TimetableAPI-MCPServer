using System.Net;
using AbeckDev.DbTimetable.Mcp.Models;
using AbeckDev.DbTimetable.Mcp.Services;
using Microsoft.Extensions.Options;
using Moq;
using Moq.Protected;

namespace AbeckDev.DbTimetable.Mcp.Test;

public class TimeTableServiceTests
{
    private readonly Mock<IOptions<Configuration>> _mockOptions;
    private readonly Configuration _config;
    private readonly string _testXmlResponse = "<?xml version=\"1.0\" encoding=\"UTF-8\"?><timetable><station><name>Test Station</name></station></timetable>";

    public TimeTableServiceTests()
    {
        _config = new Configuration
        {
            BaseUrl = "https://test.api.com/",
            ClientId = "test-client-id",
            ApiKey = "test-api-key"
        };
        _mockOptions = new Mock<IOptions<Configuration>>();
        _mockOptions.Setup(o => o.Value).Returns(_config);
    }

    private HttpClient CreateMockHttpClient(HttpStatusCode statusCode, string content)
    {
        var mockHandler = new Mock<HttpMessageHandler>();
        mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = statusCode,
                Content = new StringContent(content)
            });

        return new HttpClient(mockHandler.Object)
        {
            BaseAddress = new Uri(_config.BaseUrl)
        };
    }

    private void VerifyHttpRequest(Mock<HttpMessageHandler> mockHandler, string expectedPath, string expectedClientId, string expectedApiKey)
    {
        mockHandler.Protected().Verify(
            "SendAsync",
            Times.Once(),
            ItExpr.Is<HttpRequestMessage>(req =>
                req.RequestUri!.PathAndQuery.Contains(expectedPath) &&
                req.Headers.Contains("DB-Client-Id") &&
                req.Headers.GetValues("DB-Client-Id").First() == expectedClientId &&
                req.Headers.Contains("DB-Api-Key") &&
                req.Headers.GetValues("DB-Api-Key").First() == expectedApiKey &&
                req.Headers.Accept.Any(h => h.MediaType == "application/xml")),
            ItExpr.IsAny<CancellationToken>());
    }

    [Fact]
    public async Task GetRecentTimetableChangesAsync_WithValidEventNo_ReturnsXmlContent()
    {
        // Arrange
        var httpClient = CreateMockHttpClient(HttpStatusCode.OK, _testXmlResponse);
        var service = new TimeTableService(httpClient, _mockOptions.Object);
        var eventNo = "12345";

        // Act
        var result = await service.GetRecentTimetableChangesAsync(eventNo);

        // Assert
        Assert.Equal(_testXmlResponse, result);
    }

    [Fact]
    public async Task GetRecentTimetableChangesAsync_WithHttpError_ThrowsHttpRequestException()
    {
        // Arrange
        var mockHandler = new Mock<HttpMessageHandler>();
        mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.NotFound,
                Content = new StringContent("Not Found")
            });

        var httpClient = new HttpClient(mockHandler.Object)
        {
            BaseAddress = new Uri(_config.BaseUrl)
        };
        var service = new TimeTableService(httpClient, _mockOptions.Object);
        var eventNo = "99999";

        // Act & Assert
        await Assert.ThrowsAsync<HttpRequestException>(() => 
            service.GetRecentTimetableChangesAsync(eventNo));
    }

    [Fact]
    public async Task GetStationBoardAsync_WithoutDate_UsesCurrentDate()
    {
        // Arrange
        var httpClient = CreateMockHttpClient(HttpStatusCode.OK, _testXmlResponse);
        var service = new TimeTableService(httpClient, _mockOptions.Object);
        var evaNo = "8000105";

        // Act
        var result = await service.GetStationBoardAsync(evaNo);

        // Assert
        Assert.Equal(_testXmlResponse, result);
    }

    [Fact]
    public async Task GetStationBoardAsync_WithDate_UsesProvidedDate()
    {
        // Arrange
        var mockHandler = new Mock<HttpMessageHandler>();
        mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(_testXmlResponse)
            });

        var httpClient = new HttpClient(mockHandler.Object)
        {
            BaseAddress = new Uri(_config.BaseUrl)
        };
        var service = new TimeTableService(httpClient, _mockOptions.Object);
        var evaNo = "8000105";
        var testDate = new DateTime(2025, 11, 5, 18, 30, 0);

        // Act
        var result = await service.GetStationBoardAsync(evaNo, testDate);

        // Assert
        Assert.Equal(_testXmlResponse, result);
        
        // Verify the request path contains the formatted date
        mockHandler.Protected().Verify(
            "SendAsync",
            Times.Once(),
            ItExpr.Is<HttpRequestMessage>(req =>
                req.RequestUri!.PathAndQuery.Contains("plan/8000105/251105/18")),
            ItExpr.IsAny<CancellationToken>());
    }

    [Fact]
    public async Task GetStationBoardAsync_WithHttpError_ThrowsHttpRequestException()
    {
        // Arrange
        var mockHandler = new Mock<HttpMessageHandler>();
        mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.InternalServerError,
                Content = new StringContent("Server Error")
            });

        var httpClient = new HttpClient(mockHandler.Object)
        {
            BaseAddress = new Uri(_config.BaseUrl)
        };
        var service = new TimeTableService(httpClient, _mockOptions.Object);
        var evaNo = "8000105";

        // Act & Assert
        await Assert.ThrowsAsync<HttpRequestException>(() => 
            service.GetStationBoardAsync(evaNo));
    }

    [Fact]
    public async Task GetFullChangesAsync_WithValidEvaNo_ReturnsXmlContent()
    {
        // Arrange
        var httpClient = CreateMockHttpClient(HttpStatusCode.OK, _testXmlResponse);
        var service = new TimeTableService(httpClient, _mockOptions.Object);
        var evaNo = "8000105";

        // Act
        var result = await service.GetFullChangesAsync(evaNo);

        // Assert
        Assert.Equal(_testXmlResponse, result);
    }

    [Fact]
    public async Task GetFullChangesAsync_WithHttpError_ThrowsHttpRequestException()
    {
        // Arrange
        var mockHandler = new Mock<HttpMessageHandler>();
        mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.Unauthorized,
                Content = new StringContent("Unauthorized")
            });

        var httpClient = new HttpClient(mockHandler.Object)
        {
            BaseAddress = new Uri(_config.BaseUrl)
        };
        var service = new TimeTableService(httpClient, _mockOptions.Object);
        var evaNo = "8000105";

        // Act & Assert
        await Assert.ThrowsAsync<HttpRequestException>(() => 
            service.GetFullChangesAsync(evaNo));
    }

    [Fact]
    public async Task GetStationInformation_WithValidPattern_ReturnsXmlContent()
    {
        // Arrange
        var httpClient = CreateMockHttpClient(HttpStatusCode.OK, _testXmlResponse);
        var service = new TimeTableService(httpClient, _mockOptions.Object);
        var pattern = "Frankfurt";

        // Act
        var result = await service.GetStationInformation(pattern);

        // Assert
        Assert.Equal(_testXmlResponse, result);
    }

    [Fact]
    public async Task GetStationInformation_WithHttpError_ThrowsHttpRequestException()
    {
        // Arrange
        var mockHandler = new Mock<HttpMessageHandler>();
        mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.BadRequest,
                Content = new StringContent("Bad Request")
            });

        var httpClient = new HttpClient(mockHandler.Object)
        {
            BaseAddress = new Uri(_config.BaseUrl)
        };
        var service = new TimeTableService(httpClient, _mockOptions.Object);
        var pattern = "InvalidPattern";

        // Act & Assert
        await Assert.ThrowsAsync<HttpRequestException>(() => 
            service.GetStationInformation(pattern));
    }

    [Fact]
    public async Task GetRecentTimetableChangesAsync_SetsCorrectHeaders()
    {
        // Arrange
        var mockHandler = new Mock<HttpMessageHandler>();
        mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(_testXmlResponse)
            });

        var httpClient = new HttpClient(mockHandler.Object)
        {
            BaseAddress = new Uri(_config.BaseUrl)
        };
        var service = new TimeTableService(httpClient, _mockOptions.Object);
        var eventNo = "12345";

        // Act
        await service.GetRecentTimetableChangesAsync(eventNo);

        // Assert
        VerifyHttpRequest(mockHandler, $"rchg/{eventNo}", _config.ClientId, _config.ApiKey);
    }

    [Fact]
    public async Task GetFullChangesAsync_SetsCorrectHeaders()
    {
        // Arrange
        var mockHandler = new Mock<HttpMessageHandler>();
        mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(_testXmlResponse)
            });

        var httpClient = new HttpClient(mockHandler.Object)
        {
            BaseAddress = new Uri(_config.BaseUrl)
        };
        var service = new TimeTableService(httpClient, _mockOptions.Object);
        var evaNo = "8000105";

        // Act
        await service.GetFullChangesAsync(evaNo);

        // Assert
        VerifyHttpRequest(mockHandler, $"fchg/{evaNo}", _config.ClientId, _config.ApiKey);
    }

    [Fact]
    public async Task GetStationInformation_SetsCorrectHeaders()
    {
        // Arrange
        var mockHandler = new Mock<HttpMessageHandler>();
        mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(_testXmlResponse)
            });

        var httpClient = new HttpClient(mockHandler.Object)
        {
            BaseAddress = new Uri(_config.BaseUrl)
        };
        var service = new TimeTableService(httpClient, _mockOptions.Object);
        var pattern = "Frankfurt";

        // Act
        await service.GetStationInformation(pattern);

        // Assert
        VerifyHttpRequest(mockHandler, $"station/{pattern}", _config.ClientId, _config.ApiKey);
    }

    [Fact]
    public async Task FindTrainConnectionsAsync_WithValidStations_ReturnsAnalysisReport()
    {
        // Arrange
        var stationXml = @"<?xml version=""1.0"" encoding=""UTF-8""?>
            <stations>
                <station name=""Frankfurt Hbf"" eva=""8000105"" ds100=""FF""/>
            </stations>";
        
        var timetableXml = @"<?xml version=""1.0"" encoding=""UTF-8""?>
            <timetable station=""Frankfurt Hbf"">
                <s id=""1"">
                    <tl c=""ICE"" n=""123"" f=""Berlin Hbf""/>
                    <dp pt=""2511061430"" pp=""7"" ppth=""Frankfurt Hbf|Mannheim|Heidelberg|Berlin Hbf""/>
                </s>
            </timetable>";

        var changesXml = @"<?xml version=""1.0"" encoding=""UTF-8""?><timetable/>";

        var mockHandler = new Mock<HttpMessageHandler>();
        var requestCount = 0;
        mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(() =>
            {
                requestCount++;
                // First two calls are station lookups, third is timetable, fourth is full changes
                if (requestCount <= 2)
                    return new HttpResponseMessage { StatusCode = HttpStatusCode.OK, Content = new StringContent(stationXml) };
                if (requestCount == 3)
                    return new HttpResponseMessage { StatusCode = HttpStatusCode.OK, Content = new StringContent(timetableXml) };
                return new HttpResponseMessage { StatusCode = HttpStatusCode.OK, Content = new StringContent(changesXml) };
            });

        var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri(_config.BaseUrl) };
        var service = new TimeTableService(httpClient, _mockOptions.Object);

        // Act
        var result = await service.FindTrainConnectionsAsync("Frankfurt", "Berlin");

        // Assert
        Assert.Contains("Train Connection Analysis", result);
        Assert.Contains("Frankfurt Hbf", result);
        Assert.Contains("EVA: 8000105", result);
    }

    [Fact]
    public async Task FindTrainConnectionsAsync_WithLiveDelayData_ReturnsDelayInformation()
    {
        // Arrange
        var stationXml = @"<?xml version=""1.0"" encoding=""UTF-8""?>
            <stations>
                <station name=""Frankfurt Hbf"" eva=""8000105"" ds100=""FF""/>
            </stations>";
        
        var timetableXml = @"<?xml version=""1.0"" encoding=""UTF-8""?>
            <timetable station=""Frankfurt Hbf"">
                <s id=""123456"">
                    <tl c=""ICE"" n=""123"" f=""Berlin Hbf""/>
                    <dp pt=""2511061430"" pp=""7"" ppth=""Frankfurt Hbf|Mannheim|Berlin Hbf""/>
                </s>
            </timetable>";

        // Changes XML with live delay data - train delayed by 10 minutes
        var changesXml = @"<?xml version=""1.0"" encoding=""UTF-8""?>
            <timetable>
                <s id=""123456"">
                    <tl c=""ICE"" n=""123"" f=""Berlin Hbf""/>
                    <dp pt=""2511061430"" ct=""2511061440"" pp=""7"" cp=""8""/>
                    <m t=""Train delayed due to technical issues""/>
                </s>
            </timetable>";

        var mockHandler = new Mock<HttpMessageHandler>();
        var requestCount = 0;
        mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(() =>
            {
                requestCount++;
                // First two calls are station lookups, third is timetable, fourth is full changes
                if (requestCount <= 2)
                    return new HttpResponseMessage { StatusCode = HttpStatusCode.OK, Content = new StringContent(stationXml) };
                if (requestCount == 3)
                    return new HttpResponseMessage { StatusCode = HttpStatusCode.OK, Content = new StringContent(timetableXml) };
                return new HttpResponseMessage { StatusCode = HttpStatusCode.OK, Content = new StringContent(changesXml) };
            });

        var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri(_config.BaseUrl) };
        var service = new TimeTableService(httpClient, _mockOptions.Object);

        // Act
        var result = await service.FindTrainConnectionsAsync("Frankfurt", "Berlin");

        // Assert
        Assert.Contains("Train Connection Analysis", result);
        Assert.Contains("Delay: +10 minutes", result);
        Assert.Contains("Train delayed due to technical issues", result);
        Assert.Contains("Platform: 8", result); // Changed platform
    }

    [Fact]
    public async Task FindTrainConnectionsAsync_WithInvalidStationA_ReturnsError()
    {
        // Arrange
        var mockHandler = new Mock<HttpMessageHandler>();
        mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.NotFound,
                Content = new StringContent("Not Found")
            });

        var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri(_config.BaseUrl) };
        var service = new TimeTableService(httpClient, _mockOptions.Object);

        // Act
        var result = await service.FindTrainConnectionsAsync("InvalidStation", "Berlin");

        // Assert
        Assert.Contains("Could not find station 'InvalidStation'", result);
    }

    [Fact]
    public async Task FindTrainConnectionsAsync_WithInvalidStationB_ReturnsError()
    {
        // Arrange
        var stationAXml = @"<?xml version=""1.0"" encoding=""UTF-8""?>
            <stations>
                <station name=""Frankfurt Hbf"" eva=""8000105"" ds100=""FF""/>
            </stations>";

        var mockHandler = new Mock<HttpMessageHandler>();
        var requestCount = 0;
        mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(() =>
            {
                requestCount++;
                if (requestCount == 1)
                    return new HttpResponseMessage { StatusCode = HttpStatusCode.OK, Content = new StringContent(stationAXml) };
                return new HttpResponseMessage { StatusCode = HttpStatusCode.NotFound, Content = new StringContent("Not Found") };
            });

        var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri(_config.BaseUrl) };
        var service = new TimeTableService(httpClient, _mockOptions.Object);

        // Act
        var result = await service.FindTrainConnectionsAsync("Frankfurt", "InvalidStation");

        // Assert
        Assert.Contains("Could not find station 'InvalidStation'", result);
    }
}
