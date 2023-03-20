using IIIFRepository.Requests;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace IIIFRepository.Responses;

public class StorageCollectionBuilder
{
    private PathRequest pathRequest;

    public StorageCollectionBuilder(PathRequest pathRequest)
    {
        this.pathRequest = pathRequest;
    }

    public string Read()
    {
        JsonDocument itemsDocument;
        JsonDocument collection;
        using (var stream = pathRequest.ItemsFile.OpenRead())
        {
            itemsDocument = JsonDocument.Parse(stream)!;
        }

        using (var stream = pathRequest.BaseFile.OpenRead())
        {
            collection = JsonDocument.Parse(stream)!;
        }

        var mutable = collection.Deserialize<JsonNode>();
        mutable!["items"] = itemsDocument.RootElement.GetProperty("items").Deserialize<JsonArray>();
        var options = new JsonSerializerOptions { WriteIndented = true };

        return mutable.ToJsonString(options);
    }
}
