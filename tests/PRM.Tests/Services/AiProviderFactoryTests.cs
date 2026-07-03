using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Moq;
using PRM.Domain.Entities;
using PRM.Infrastructure.AI;
using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace PRM.Tests.Services;

public class AiProviderFactoryTests
{
    private readonly Mock<IHttpClientFactory> _httpFactoryMock;
    private readonly Mock<IConfiguration> _configurationMock;
    private readonly AiProviderFactory _factory;

    public AiProviderFactoryTests()
    {
        _httpFactoryMock = new Mock<IHttpClientFactory>();
        _configurationMock = new Mock<IConfiguration>();
        _factory = new AiProviderFactory(_httpFactoryMock.Object, _configurationMock.Object);
        AiProviderFactory.ResetCache();
    }

    [Fact]
    public void Create_DefaultProviderIsGemini_ReturnsGeminiProvider()
    {
        // Arrange
        var config = new SystemConfig { LlmProvider = "Gemini" };
        _configurationMock.Setup(c => c["AiSettings:GeminiApiKey"]).Returns("test-gemini-key");
        _configurationMock.Setup(c => c["AiSettings:GeminiModel"]).Returns("gemini-1.5-flash");
        _configurationMock.Setup(c => c["AiSettings:GeminiBaseUrl"]).Returns("https://generativelanguage.googleapis.com/");

        // Act
        var provider = _factory.Create(config);

        // Assert
        provider.Should().BeOfType<GeminiProvider>();
    }

    [Fact]
    public void Create_GemmaAvailable_ReturnsLocalGemmaProvider()
    {
        // Arrange
        var config = new SystemConfig { LlmProvider = "LocalGemma", LlmApiUrl = "http://localhost:11434" };
        var mockClient = CreateMockHttpClient(succeeds: true);
        _httpFactoryMock.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(mockClient);

        // Act
        var provider = _factory.Create(config);

        // Assert
        provider.Should().BeOfType<LocalGemmaProvider>();
    }

    [Fact]
    public void Create_GemmaUnavailable_FallsBackToGeminiProvider()
    {
        // Arrange
        var config = new SystemConfig { LlmProvider = "LocalGemma", LlmApiUrl = "http://localhost:11434" };
        var mockClient = CreateMockHttpClient(succeeds: false);
        _httpFactoryMock.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(mockClient);

        // Act
        var provider = _factory.Create(config);

        // Assert
        provider.Should().BeOfType<GeminiProvider>();
    }

    private HttpClient CreateMockHttpClient(bool succeeds)
    {
        var handler = new MockHttpMessageHandler(succeeds);
        return new HttpClient(handler);
    }

    private class MockHttpMessageHandler : HttpMessageHandler
    {
        private readonly bool _succeeds;
        public MockHttpMessageHandler(bool succeeds) => _succeeds = succeeds;

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (_succeeds)
            {
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
            }
            throw new HttpRequestException("Server down");
        }
    }
}
