using IIIFRespository.Requests;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace IIIFRespository.Responses;

public class StorageCollectionBuilder
{
    private PathRequest pathRequest;

    public StorageCollectionBuilder(PathRequest pathRequest)
    {
        this.pathRequest = pathRequest;
    }

    public string Read()
    {
        JsonNode items;
        JsonNode collection;
        using (var stream = pathRequest.ItemsFile.OpenRead())
        {
            items = JsonNode.Parse(stream)!;
        }

        using (var stream = pathRequest.BaseFile.OpenRead())
        {
            collection = JsonNode.Parse(stream)!;
        }

        collection!["items"] = items;
        var options = new JsonSerializerOptions
        {
            WriteIndented = true
        };

        return collection.ToJsonString(options);
    }
}
