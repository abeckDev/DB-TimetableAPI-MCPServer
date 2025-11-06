using AbeckDev.DbTimetable.Mcp.Services;
using Moq;

namespace AbeckDev.DbTimetable.Mcp.Test;

public class TimetableToolsTests
{
    private readonly Mock<ITimeTableService> _mockService;
    private readonly string _testXmlResponse = "<?xml version=\"1.0\" encoding=\"UTF-8\"?><timetable><station><name>Test Station</name></station></timetable>";

    public TimetableToolsTests()
    {
        _mockService = new Mock<ITimeTableService>(MockBehavior.Strict);
    }

    [Fact]
    public async Task GetFullTimetableChanges_WithValidEventNo_ReturnsXmlContent()
    {
        // Arrange
        var eventNo = "12345";
        _mockService.Setup(s => s.GetFullChangesAsync(eventNo, default))
            .ReturnsAsync(_testXmlResponse);

        var tools = new Tools.TimetableTools(_mockService.Object);

        // Act
        var result = await tools.GetFullStationChanges(eventNo);

        // Assert
        Assert.Equal(_testXmlResponse, result);
        _mockService.Verify(s => s.GetFullChangesAsync(eventNo, default), Times.Once);
    }

    [Fact]
    public async Task GetFullTimetableChanges_WithHttpRequestException_ReturnsErrorMessage()
    {
        // Arrange
        var eventNo = "12345";
        var exceptionMessage = "Network error occurred";
        _mockService.Setup(s => s.GetFullChangesAsync(eventNo, default))
            .ThrowsAsync(new HttpRequestException(exceptionMessage));

        var tools = new Tools.TimetableTools(_mockService.Object);

        // Act
        var result = await tools.GetFullStationChanges(eventNo);

        // Assert
        Assert.Contains("Error fetching timetable changes:", result);
        Assert.Contains(exceptionMessage, result);
    }

    [Fact]
    public async Task GetFullTimetableChanges_WithGeneralException_ReturnsUnexpectedErrorMessage()
    {
        // Arrange
        var eventNo = "12345";
        var exceptionMessage = "Unexpected error";
        _mockService.Setup(s => s.GetFullChangesAsync(eventNo, default))
            .ThrowsAsync(new InvalidOperationException(exceptionMessage));

        var tools = new Tools.TimetableTools(_mockService.Object);

        // Act
        var result = await tools.GetFullStationChanges(eventNo);

        // Assert
        Assert.Contains("Unexpected error:", result);
        Assert.Contains(exceptionMessage, result);
    }

    [Fact]
    public async Task GetStationBoard_WithValidEvaNoAndNoDateTime_ReturnsXmlContent()
    {
        // Arrange
        var evaNo = "8000105";
        _mockService.Setup(s => s.GetStationBoardAsync(evaNo, null, default))
            .ReturnsAsync(_testXmlResponse);

        var tools = new Tools.TimetableTools(_mockService.Object);

        // Act
        var result = await tools.GetStationBoard(evaNo, null);

        // Assert
        Assert.Equal(_testXmlResponse, result);
        _mockService.Verify(s => s.GetStationBoardAsync(evaNo, null, default), Times.Once);
    }

    [Fact]
    public async Task GetStationBoard_WithValidEvaNoAndDateTime_ParsesDateTimeAndReturnsXmlContent()
    {
        // Arrange
        var evaNo = "8000105";
        var dateTimeString = "2025-11-05 18:30";
        var parsedDateTime = DateTime.Parse(dateTimeString);
        
        _mockService.Setup(s => s.GetStationBoardAsync(evaNo, parsedDateTime, default))
            .ReturnsAsync(_testXmlResponse);

        var tools = new Tools.TimetableTools(_mockService.Object);

        // Act
        var result = await tools.GetStationBoard(evaNo, dateTimeString);

        // Assert
        Assert.Equal(_testXmlResponse, result);
        _mockService.Verify(s => s.GetStationBoardAsync(evaNo, parsedDateTime, default), Times.Once);
    }

    [Fact]
    public async Task GetStationBoard_WithInvalidDateTimeFormat_ReturnsErrorMessage()
    {
        // Arrange
        var evaNo = "8000105";
        var invalidDateTime = "invalid-date-format";

        var tools = new Tools.TimetableTools(_mockService.Object);

        // Act
        var result = await tools.GetStationBoard(evaNo, invalidDateTime);

        // Assert
        Assert.Contains("Error: Invalid date format", result);
        Assert.Contains("yyyy-MM-dd HH:mm", result);
    }

    [Fact]
    public async Task GetStationBoard_WithEmptyDateTime_CallsServiceWithNull()
    {
        // Arrange
        var evaNo = "8000105";
        _mockService.Setup(s => s.GetStationBoardAsync(evaNo, null, default))
            .ReturnsAsync(_testXmlResponse);

        var tools = new Tools.TimetableTools(_mockService.Object);

        // Act
        var result = await tools.GetStationBoard(evaNo, "");

        // Assert
        Assert.Equal(_testXmlResponse, result);
        _mockService.Verify(s => s.GetStationBoardAsync(evaNo, null, default), Times.Once);
    }

    [Fact]
    public async Task GetStationBoard_WithHttpRequestException_ReturnsErrorMessage()
    {
        // Arrange
        var evaNo = "8000105";
        var exceptionMessage = "Station not found";
        _mockService.Setup(s => s.GetStationBoardAsync(evaNo, null, default))
            .ThrowsAsync(new HttpRequestException(exceptionMessage));

        var tools = new Tools.TimetableTools(_mockService.Object);

        // Act
        var result = await tools.GetStationBoard(evaNo, null);

        // Assert
        Assert.Contains("Error fetching station board:", result);
        Assert.Contains(exceptionMessage, result);
    }

    [Fact]
    public async Task GetStationBoard_WithGeneralException_ReturnsUnexpectedErrorMessage()
    {
        // Arrange
        var evaNo = "8000105";
        var exceptionMessage = "Unexpected error";
        _mockService.Setup(s => s.GetStationBoardAsync(evaNo, null, default))
            .ThrowsAsync(new InvalidOperationException(exceptionMessage));

        var tools = new Tools.TimetableTools(_mockService.Object);

        // Act
        var result = await tools.GetStationBoard(evaNo, null);

        // Assert
        Assert.Contains("Unexpected error:", result);
        Assert.Contains(exceptionMessage, result);
    }

    [Fact]
    public async Task GetRecentChanges_WithValidEvaNo_ReturnsXmlContent()
    {
        // Arrange
        var evaNo = "8000105";
        _mockService.Setup(s => s.GetRecentTimetableChangesAsync(evaNo, default))
            .ReturnsAsync(_testXmlResponse);

        var tools = new Tools.TimetableTools(_mockService.Object);

        // Act
        var result = await tools.GetRecentStationChanges(evaNo);

        // Assert
        Assert.Equal(_testXmlResponse, result);
        _mockService.Verify(s => s.GetRecentTimetableChangesAsync(evaNo, default), Times.Once);
    }

    [Fact]
    public async Task GetRecentChanges_WithHttpRequestException_ReturnsErrorMessage()
    {
        // Arrange
        var evaNo = "8000105";
        var exceptionMessage = "API rate limit exceeded";
        _mockService.Setup(s => s.GetRecentTimetableChangesAsync(evaNo, default))
            .ThrowsAsync(new HttpRequestException(exceptionMessage));

        var tools = new Tools.TimetableTools(_mockService.Object);

        // Act
        var result = await tools.GetRecentStationChanges(evaNo);

        // Assert
        Assert.Contains("Error fetching station changes:", result);
        Assert.Contains(exceptionMessage, result);
    }

    [Fact]
    public async Task GetRecentChanges_WithGeneralException_ReturnsUnexpectedErrorMessage()
    {
        // Arrange
        var evaNo = "8000105";
        var exceptionMessage = "Unexpected error";
        _mockService.Setup(s => s.GetRecentTimetableChangesAsync(evaNo, default))
            .ThrowsAsync(new InvalidOperationException(exceptionMessage));

        var tools = new Tools.TimetableTools(_mockService.Object);

        // Act
        var result = await tools.GetRecentStationChanges(evaNo);

        // Assert
        Assert.Contains("Unexpected error:", result);
        Assert.Contains(exceptionMessage, result);
    }

    [Fact]
    public async Task GetStationDetails_WithValidPattern_ReturnsXmlContent()
    {
        // Arrange
        var pattern = "Frankfurt";
        _mockService.Setup(s => s.GetStationInformation(pattern, default))
            .ReturnsAsync(_testXmlResponse);

        var tools = new Tools.TimetableTools(_mockService.Object);

        // Act
        var result = await tools.GetStationInformation(pattern);

        // Assert
        Assert.Equal(_testXmlResponse, result);
        _mockService.Verify(s => s.GetStationInformation(pattern, default), Times.Once);
    }

    [Fact]
    public async Task GetStationDetails_WithHttpRequestException_ReturnsErrorMessage()
    {
        // Arrange
        var pattern = "Frankfurt";
        var exceptionMessage = "Service unavailable";
        _mockService.Setup(s => s.GetStationInformation(pattern, default))
            .ThrowsAsync(new HttpRequestException(exceptionMessage));

        var tools = new Tools.TimetableTools(_mockService.Object);

        // Act
        var result = await tools.GetStationInformation(pattern);

        // Assert
        Assert.Contains("Error fetching station details:", result);
        Assert.Contains(exceptionMessage, result);
    }

    [Fact]
    public async Task GetStationDetails_WithGeneralException_ReturnsUnexpectedErrorMessage()
    {
        // Arrange
        var pattern = "Frankfurt";
        var exceptionMessage = "Unexpected error";
        _mockService.Setup(s => s.GetStationInformation(pattern, default))
            .ThrowsAsync(new InvalidOperationException(exceptionMessage));

        var tools = new Tools.TimetableTools(_mockService.Object);

        // Act
        var result = await tools.GetStationInformation(pattern);

        // Assert
        Assert.Contains("Unexpected error:", result);
        Assert.Contains(exceptionMessage, result);
    }

    [Fact]
    public async Task FindTrainConnections_WithValidStations_ReturnsConnectionAnalysis()
    {
        // Arrange
        var stationA = "Frankfurt";
        var stationB = "Berlin";
        var expectedResult = "=== Train Connection Analysis ===\nConnections found";
        
        _mockService.Setup(s => s.FindTrainConnectionsAsync(stationA, stationB, null, default))
            .ReturnsAsync(expectedResult);

        var tools = new Tools.TimetableTools(_mockService.Object);

        // Act
        var result = await tools.FindTrainConnections(stationA, stationB, null);

        // Assert
        Assert.Equal(expectedResult, result);
        _mockService.Verify(s => s.FindTrainConnectionsAsync(stationA, stationB, null, default), Times.Once);
    }

    [Fact]
    public async Task FindTrainConnections_WithValidDateTime_ParsesAndCallsService()
    {
        // Arrange
        var stationA = "Frankfurt";
        var stationB = "Berlin";
        var dateTimeString = "2025-11-06 14:30";
        var parsedDateTime = DateTime.Parse(dateTimeString);
        var expectedResult = "Connections found";
        
        _mockService.Setup(s => s.FindTrainConnectionsAsync(stationA, stationB, parsedDateTime, default))
            .ReturnsAsync(expectedResult);

        var tools = new Tools.TimetableTools(_mockService.Object);

        // Act
        var result = await tools.FindTrainConnections(stationA, stationB, dateTimeString);

        // Assert
        Assert.Equal(expectedResult, result);
        _mockService.Verify(s => s.FindTrainConnectionsAsync(stationA, stationB, parsedDateTime, default), Times.Once);
    }

    [Fact]
    public async Task FindTrainConnections_WithInvalidDateTimeFormat_ReturnsErrorMessage()
    {
        // Arrange
        var stationA = "Frankfurt";
        var stationB = "Berlin";
        var invalidDateTime = "invalid-date-format";

        var tools = new Tools.TimetableTools(_mockService.Object);

        // Act
        var result = await tools.FindTrainConnections(stationA, stationB, invalidDateTime);

        // Assert
        Assert.Contains("Error: Invalid date format", result);
        Assert.Contains("yyyy-MM-dd HH:mm", result);
    }

    [Fact]
    public async Task FindTrainConnections_WithEmptyDateTime_CallsServiceWithNull()
    {
        // Arrange
        var stationA = "Frankfurt";
        var stationB = "Berlin";
        var expectedResult = "Connections found";
        
        _mockService.Setup(s => s.FindTrainConnectionsAsync(stationA, stationB, null, default))
            .ReturnsAsync(expectedResult);

        var tools = new Tools.TimetableTools(_mockService.Object);

        // Act
        var result = await tools.FindTrainConnections(stationA, stationB, "");

        // Assert
        Assert.Equal(expectedResult, result);
        _mockService.Verify(s => s.FindTrainConnectionsAsync(stationA, stationB, null, default), Times.Once);
    }

    [Fact]
    public async Task FindTrainConnections_WithHttpRequestException_ReturnsErrorMessage()
    {
        // Arrange
        var stationA = "Frankfurt";
        var stationB = "Berlin";
        var exceptionMessage = "Network error";
        
        _mockService.Setup(s => s.FindTrainConnectionsAsync(stationA, stationB, null, default))
            .ThrowsAsync(new HttpRequestException(exceptionMessage));

        var tools = new Tools.TimetableTools(_mockService.Object);

        // Act
        var result = await tools.FindTrainConnections(stationA, stationB, null);

        // Assert
        Assert.Contains("Error finding train connections:", result);
        Assert.Contains(exceptionMessage, result);
    }

    [Fact]
    public async Task FindTrainConnections_WithGeneralException_ReturnsUnexpectedErrorMessage()
    {
        // Arrange
        var stationA = "Frankfurt";
        var stationB = "Berlin";
        var exceptionMessage = "Unexpected error";
        
        _mockService.Setup(s => s.FindTrainConnectionsAsync(stationA, stationB, null, default))
            .ThrowsAsync(new InvalidOperationException(exceptionMessage));

        var tools = new Tools.TimetableTools(_mockService.Object);

        // Act
        var result = await tools.FindTrainConnections(stationA, stationB, null);

        // Assert
        Assert.Contains("Unexpected error:", result);
        Assert.Contains(exceptionMessage, result);
    }
}
