using FluentAssertions;
using IIIF.Presentation.V3;
using IIIF.Presentation.V3.Strings;
using Microsoft.AspNetCore.Mvc.Testing;
using System.Net.Http.Json;
using System.Text.Json.Nodes;

namespace ProtocolTests
{
    public class PostTests : IClassFixture<ProtocolWebApplicationFactory<Program>>
    {
        private readonly HttpClient client;
        private readonly ProtocolWebApplicationFactory<Program> factory;

        public PostTests(ProtocolWebApplicationFactory<Program> factory)
        {
            this.factory = factory;
            client = factory.CreateClient(new WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false
            });
        }

        private string GetFullId(string path)
        {
            return client.BaseAddress + path; 
        }

        [Fact]
        public async Task Can_Post_Manifest()
        {
            // Arrange
            var manifest = new Manifest
            {
                Id = GetFullId("iiif/test-post-1"),
                Label = new LanguageMap("en", "Test Post 1 - " + nameof(Can_Post_Manifest))
            };

            // Act
            var response = await client.PostAsync("/iiif/", manifest.ToHttpContent());

            // Assert
            response.StatusCode.Should().Be(System.Net.HttpStatusCode.Created);
        }


        [Fact]
        public async Task Cannot_Post_To_Manifest_Id()
        {
            // Arrange
            var manifest = new Manifest
            {
                Id = GetFullId("test-post-fail-1"),
                Label = new LanguageMap("en", "Test Post 1")
            };

            // Act
            var response = await client.PostAsync("/iiif/test-post-fail-1", manifest.ToHttpContent());

            // Assert
            response.StatusCode.Should().Be(System.Net.HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task Cannot_Post_Manifest_with_Same_Id()
        {
            // Arrange
            var manifest1 = new Manifest
            {
                Id = GetFullId("iiif/test-post-2"),
                Label = new LanguageMap("en", "Test Post 2 - " + nameof(Cannot_Post_Manifest_with_Same_Id))
            };
            var manifest2 = new Manifest
            {
                Id = GetFullId("iiif/test-post-2"),
                Label = new LanguageMap("en", "Test Post 2 - " + nameof(Cannot_Post_Manifest_with_Same_Id))
            };

            // Act
            var response1 = await client.PostAsync("/iiif/", manifest1.ToHttpContent());
            var response2 = await client.PostAsync("/iiif/", manifest2.ToHttpContent());

            // Assert
            response1.StatusCode.Should().Be(System.Net.HttpStatusCode.Created);
            response2.StatusCode.Should().Be(System.Net.HttpStatusCode.Conflict);
        }


        [Fact]
        public async Task Can_See_Manifest_In_Collection()
        {
            // Arrange
            var manifest = new Manifest
            {
                Id = GetFullId("iiif/test-post-3"),
                Label = new LanguageMap("en", "Test Post 3 - " + nameof(Can_See_Manifest_In_Collection))
            };

            // Act
            var response1 = await client.PostAsync("/iiif/", manifest.ToHttpContent());
            var collection = await client.GetFromJsonAsync<JsonNode>("/iiif/");


            // Assert
            response1.StatusCode.Should().Be(System.Net.HttpStatusCode.Created);
            collection["items"].AsArray().Should().Contain(item => item["id"].ToString() == manifest.Id); 
        }


        [Fact]
        public async Task Can_Create_Stored_Collection()
        {
            // Arrange
            var collection = new Collection
            {
                Id = GetFullId("iiif/test-post-4"),
                Label = new LanguageMap("en", "Test Post 4 - " + nameof(Can_Create_Stored_Collection)),
                Items = new List<ICollectionItem>
                {
                    new Manifest
                    {
                        Id = "https://iiif.wellcomecollection.org/presentation/b29269830",
                        Label = new LanguageMap("en", "Report 1974")
                    },
                    new Manifest
                    {
                        Id = "https://iiif.wellcomecollection.org/presentation/b21463955",
                        Label = new LanguageMap("en", "Alphitaa medico-botanical glossary from the Bodleian manuscript, Selden B.35")
                    }
                }
            };

            // Act
            var response1 = await client.PostAsync("/iiif/", collection.ToHttpContent());
            var collectionBack = await client.GetFromJsonAsync<JsonNode>("/iiif/test-post-4");


            // Assert
            response1.StatusCode.Should().Be(System.Net.HttpStatusCode.Created);
            collectionBack["items"].AsArray().Should().Contain(
                item => item["id"].ToString() == "https://iiif.wellcomecollection.org/presentation/b29269830");
        }

        [Fact]
        public async Task Cannot_Post_To_Stored_Collection()
        {
            // Arrange
            var collection = new Collection
            {
                Id = GetFullId("iiif/test-post-5"),
                Label = new LanguageMap("en", "Test Post 5 - " + nameof(Cannot_Post_To_Stored_Collection)),
                Items = new List<ICollectionItem>
                {
                    new Manifest
                    {
                        Id = "https://iiif.wellcomecollection.org/presentation/b29269830",
                        Label = new LanguageMap("en", "Report 1974")
                    }
                }
            };
            var manifest = new Manifest
            {
                Id = GetFullId("iiif/test-post-5/child-manifest"),
                Label = new LanguageMap("en", "This should fail")
            };

            // Act
            var response1 = await client.PostAsync("/iiif/", collection.ToHttpContent());
            var response2 = await client.PostAsync("/iiif/test-post-5", manifest.ToHttpContent());

            // Assert
            response1.StatusCode.Should().Be(System.Net.HttpStatusCode.Created);
            response2.StatusCode.Should().Be(System.Net.HttpStatusCode.BadRequest);
        }


        [Fact]
        public async Task Can_Create_Storage_Collection()
        {
            // Arrange
            var collection = new Collection
            {
                Id = GetFullId("iiif/test-post-6/"),
                Label = new LanguageMap("en", "Test Post 6 - " + nameof(Can_Create_Storage_Collection))
            };

            // Act
            var response1 = await client.PostAsync("/iiif/", collection.ToHttpContent());

            // Assert
            response1.StatusCode.Should().Be(System.Net.HttpStatusCode.Created);
        }


        [Fact]
        public async Task Storage_Collection_Appears_In_Parent_Collection_Items()
        {
            // Arrange
            var collection = new Collection
            {
                Id = GetFullId("iiif/test-post-7/"),
                Label = new LanguageMap("en", "Test Post 7 - " + nameof(Storage_Collection_Appears_In_Parent_Collection_Items))
            };

            // Act
            var response1 = await client.PostAsync("/iiif/", collection.ToHttpContent());
            var parentStorageCollection = await client.GetFromJsonAsync<JsonNode>("/iiif/");


            // Assert
            response1.StatusCode.Should().Be(System.Net.HttpStatusCode.Created);
            parentStorageCollection["items"].AsArray().Should().Contain(item => item["id"].ToString() == collection.Id);
        }



        [Fact]
        public async Task Can_Post_Manifest_Into_Newly_Created_Storage_Collection()
        {
            // Arrange
            var collection = new Collection
            {
                Id = GetFullId("iiif/test-post-8/"),
                Label = new LanguageMap("en", "Test Post 8 - " + nameof(Can_Post_Manifest_Into_Newly_Created_Storage_Collection))
            };
            var manifest = new Manifest
            {
                Id = GetFullId("iiif/test-post-8/posted-manifest"),
                Label = new LanguageMap("en", "Test Post 8 CHILD MANIFEST")
            };

            // Act
            var response1 = await client.PostAsync("/iiif/", collection.ToHttpContent());
            var response2 = await client.PostAsync("/iiif/test-post-8/", manifest.ToHttpContent());


            // Assert
            response1.StatusCode.Should().Be(System.Net.HttpStatusCode.Created);
            response2.StatusCode.Should().Be(System.Net.HttpStatusCode.Created);
        }


        // Need to decide - is the the presence of the trailing slash that makes it a storage collection?
        // Or is it the absence of items?
        // Or both?
        // Going to assume that a storage collection must be created without items (like mkdir) and have a trailing slash.
        [Fact]
        public async Task Storage_Collection_Must_Have_Trailing_Slash()
        {
            // Arrange
            var collection = new Collection
            {
                Id = GetFullId("iiif/test-post-9"),
                Label = new LanguageMap("en", "Test Post 9 - " + nameof(Storage_Collection_Must_Have_Trailing_Slash))
            };  // no items

            // Act
            var response1 = await client.PostAsync("/iiif/", collection.ToHttpContent());

            // Assert
            response1.StatusCode.Should().Be(System.Net.HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task Stored_Collection_Must_NOT_Have_Trailing_Slash()
        {
            // Arrange
            var collection = new Collection
            {
                Id = GetFullId("iiif/test-post-10/"),
                Label = new LanguageMap("en", "Test Post 10 - " + nameof(Storage_Collection_Must_Have_Trailing_Slash)),
                Items = new List<ICollectionItem>
                {
                    new Manifest
                    {
                        Id = "https://iiif.wellcomecollection.org/presentation/b29269830",
                        Label = new LanguageMap("en", "Report 1974")
                    },
                    new Manifest
                    {
                        Id = "https://iiif.wellcomecollection.org/presentation/b21463955",
                        Label = new LanguageMap("en", "Alphitaa medico-botanical glossary from the Bodleian manuscript, Selden B.35")
                    }
                }
            };

            // Act
            var response1 = await client.PostAsync("/iiif/", collection.ToHttpContent());

            // Assert
            response1.StatusCode.Should().Be(System.Net.HttpStatusCode.BadRequest);
        }


        // Need to decide - is the the presence of the trailing slash that makes it a storage collection?
        // Or is it the absence of items?
        // Or both?
        // Going to assume that a storage collection must be created without items (like mkdir) and have a trailing slash.
        [Fact]
        public async Task Storage_Collection_Will_Redirect()
        {
            // Arrange
            var collection = new Collection
            {
                Id = GetFullId("iiif/test-post-11/"),
                Label = new LanguageMap("en", "Test Post 11 - " + nameof(Storage_Collection_Will_Redirect))
            };  // no items

            // Act
            var response1 = await client.PostAsync("/iiif/", collection.ToHttpContent());
            var response2 = await client.GetAsync("/iiif/test-post-11");

            // Assert
            response1.StatusCode.Should().Be(System.Net.HttpStatusCode.Created);
            response2.StatusCode.Should().Be(System.Net.HttpStatusCode.Found);
            response2.Headers.Location!.OriginalString.Should().EndWith("/iiif/test-post-11/");
        }

        // See https://github.com/apache/couchdb/issues/620
        // The initial POST that creates a resource should not return an ETag.
        [Fact]
        public async Task IIIF_Resources_Have_Etags()
        {
            // Arrange
            var storageCollection = new Collection
            {
                Id = GetFullId("iiif/test-post-12a/"),
                Label = new LanguageMap("en", "Test Post 12a - " + nameof(IIIF_Resources_Have_Etags))
            };  // no items
            var storedCollection = new Collection
            {
                Id = GetFullId("iiif/test-post-12b"),
                Label = new LanguageMap("en", "Test Post 12b - " + nameof(IIIF_Resources_Have_Etags)),
                Items = new List<ICollectionItem>
                {
                    new Manifest
                    {
                        Id = "https://iiif.wellcomecollection.org/presentation/b29269830",
                        Label = new LanguageMap("en", "Report 1974")
                    }
                }
            };
            var manifest = new Manifest
            {
                Id = GetFullId("iiif/test-post-12c"),
                Label = new LanguageMap("en", "Test Post 12c - " + nameof(IIIF_Resources_Have_Etags))
            };

            // Act
            var response1a = await client.PostAsync("/iiif/", storageCollection.ToHttpContent());
            var response1b = await client.GetAsync("/iiif/test-post-12a/");
            var response2a = await client.PostAsync("/iiif/", storedCollection.ToHttpContent());
            var response2b = await client.GetAsync("/iiif/test-post-12b");
            var response3a = await client.PostAsync("/iiif/", manifest.ToHttpContent());
            var response3b = await client.GetAsync("/iiif/test-post-12c");

            // Assert
            response1a.Headers.ETag.Should().BeNull();
            response1b.Headers.ETag.Should().NotBeNull();
            response2a.Headers.ETag.Should().BeNull();
            response2b.Headers.ETag.Should().NotBeNull();
            response3a.Headers.ETag.Should().BeNull();
            response3b.Headers.ETag.Should().NotBeNull();
        }

        // Put file writing in a critical section; only one thread at a time may enter.


        // PUTs
        // Update a manifest in place
        // Show required if-match behaviour
        // PUT a storage collection (without items) to update all but items

        // PATCH
        // Rename a manifest (patch ID)

        // DELETE

        // OPTIONS
        // Demonstrate OPTIONS results are different for storage collections vs stored collecitons and manifests 

        // CORS
        // Ensure CORS and ensure no conflict with OPTIONS

    }
}
