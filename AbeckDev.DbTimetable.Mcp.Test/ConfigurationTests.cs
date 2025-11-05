using AbeckDev.DbTimetable.Mcp.Models;

namespace AbeckDev.DbTimetable.Mcp.Test;

public class ConfigurationTests
{
    [Fact]
    public void Configuration_SectionName_HasCorrectValue()
    {
        // Arrange & Act & Assert
        Assert.Equal("DeutscheBahnApi", Configuration.SectionName);
    }

    [Fact]
    public void Configuration_DefaultValues_AreCorrect()
    {
        // Arrange & Act
        var config = new Configuration();

        // Assert
        Assert.Equal("https://apis.deutschebahn.com/db-api-marketplace/apis/timetables/v1/", config.BaseUrl);
        Assert.Equal(string.Empty, config.ClientId);
        Assert.Equal(string.Empty, config.ApiKey);
    }

    [Fact]
    public void Configuration_Properties_CanBeSet()
    {
        // Arrange
        var config = new Configuration();
        var expectedBaseUrl = "https://test.api.com/";
        var expectedClientId = "test-client-id";
        var expectedApiKey = "test-api-key";

        // Act
        config.BaseUrl = expectedBaseUrl;
        config.ClientId = expectedClientId;
        config.ApiKey = expectedApiKey;

        // Assert
        Assert.Equal(expectedBaseUrl, config.BaseUrl);
        Assert.Equal(expectedClientId, config.ClientId);
        Assert.Equal(expectedApiKey, config.ApiKey);
    }
}
