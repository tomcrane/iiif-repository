using Microsoft.AspNetCore.Mvc.Testing;

namespace ProtocolTests
{
    public class BasicTests  : IClassFixture<ProtocolWebApplicationFactory<Program>>
    {
        private readonly HttpClient _client;
        private readonly ProtocolWebApplicationFactory<Program> _factory;

        public BasicTests(ProtocolWebApplicationFactory<Program> factory)
        {
            _factory = factory;
            _client = factory.CreateClient(new WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false
            });
        }

        [Fact]
        public async Task Get_Root_Returns_200()
        {
            // Arrange
            //var client = _factory.CreateClient();

            // Act
            var response = await _client.GetAsync("/iiif/");

            // Assert
            response.EnsureSuccessStatusCode(); // Status Code 200-299
        }
    }
}
