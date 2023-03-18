using System.Text.Json;

namespace IIIFRespository.Requests;

public class IIIFBody
{
    public IIIFBody(string body, string path)
    {
        try
        {
            using var doc = JsonDocument.Parse(body);
            Type = doc.RootElement.GetProperty("type").GetString();
            Id = doc.RootElement.GetProperty("id").GetString();

            var pathElements = path.Split('/');
            var IdElements = Id.Split('/');

            TargetPathElement = pathElements.Last();
            IdPathElement = IdElements.Last();

            JsonElement items;
            int itemCount = -1;
            if (doc.RootElement.TryGetProperty("items", out items))
            {
                itemCount = items.GetArrayLength();
            }

            IsCollectionWithItems = Type == Constants.Collection && itemCount > 0;
            IsCollectionWithNoItems = Type == Constants.Collection && itemCount <= 0;
            IsManifest = Type == Constants.Manifest;
        }
        catch(Exception ex) 
        {
            ProcessingError = ex.Message; 
        }
    }

    public string? Id { get; internal set; }

    public string? TargetPathElement { get; internal set; }
    public string? IdPathElement { get; internal set; }

    public string? Type { get; internal set; }

    public string? ProcessingError { get; internal set; }


    public bool IsCollectionWithNoItems { get; internal set; }
    public bool IsCollectionWithItems { get; internal set; }
    public bool IsManifest { get; internal set; }
}
