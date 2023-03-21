using FluentAssertions;
using IIIF.Presentation.V3;
using IIIF.Presentation.V3.Strings;
using Microsoft.AspNetCore.Mvc.Testing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace ProtocolTests
{
    public class PutTests : IClassFixture<ProtocolWebApplicationFactory<Program>>
    {
        private readonly HttpClient client;
        private readonly ProtocolWebApplicationFactory<Program> factory;

        private readonly string putContainer = "/iiif/put-tests/";

        public PutTests(ProtocolWebApplicationFactory<Program> factory)
        {
            this.factory = factory;
            client = factory.CreateClient(new WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false
            });

            // make a collection to hold the PutTests to avoid collisions

            var collection = new Collection
            {
                Id = GetFullId(putContainer),
                Label = new LanguageMap("en", "PUT tests")
            };

            // Act
            client.PostAsync("/iiif/", collection.ToHttpContent()).Wait();
        }

        private string GetFullId(string path)
        {
            if (path.StartsWith('/'))
            {
                path = path.Substring(1);
            }
            return client.BaseAddress + path;
        }

        [Fact]
        public async Task Cannot_Update_Manifest_without_ETag()
        {
            // Arrange
            var manifestPath = putContainer + "manifest-1";
            var manifest = new Manifest
            {
                Id = GetFullId(manifestPath),
                Label = new LanguageMap("en", "Manifest 1")
            };

            // Act
            var response1 = await client.PostAsync(putContainer, manifest.ToHttpContent());
            manifest.Label = new LanguageMap("en", "Manifest 1 EDITED");
            var response2 = await client.PutAsync(manifestPath, manifest.ToHttpContent());

            // Assert
            response2.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }


        [Fact]
        public async Task Can_Update_Manifest_with_ETag()
        {
            // Arrange
            var manifestPath = putContainer + "manifest-2";
            var manifest = new Manifest
            {
                Id = GetFullId(manifestPath),
                Label = new LanguageMap("en", "Manifest 2")
            };

            // Act
            var response1 = await client.PostAsync(putContainer, manifest.ToHttpContent());
            var eTag = response1.Headers.ETag!.Tag;
            manifest.Label = new LanguageMap("en", "Manifest 2 EDITED");
            var response2 = await client.PutAsyncWithETag(manifestPath, manifest.ToHttpContent(), eTag);

            // Assert
            response2.StatusCode.Should().Be(System.Net.HttpStatusCode.OK); // not 204
            response2.Headers.ETag.Should().NotBe(eTag); // different eTag
        }


        [Fact]
        public async Task Cannot_Update_Manifest_with_Stage_ETag()
        {
            // Arrange
            var manifestPath = putContainer + "manifest-3";
            var manifest = new Manifest
            {
                Id = GetFullId(manifestPath),
                Label = new LanguageMap("en", "Manifest 3")
            };

            // Act
            var response1 = await client.PostAsync(putContainer, manifest.ToHttpContent());
            var eTag = response1.Headers.ETag!.Tag;

            // someone else edits...
            manifest.Label = new LanguageMap("en", "Manifest 3 EDITED by someone else");
            var response2 = await client.PutAsyncWithETag(manifestPath, manifest.ToHttpContent(), eTag);

            // now I edit with the original eTag:
            manifest.Label = new LanguageMap("en", "Manifest 3 EDITED by me");
            var response3 = await client.PutAsyncWithETag(manifestPath, manifest.ToHttpContent(), eTag);

            // Assert
            response2.StatusCode.Should().Be(HttpStatusCode.OK); // not 204
            response3.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }
    }
}
