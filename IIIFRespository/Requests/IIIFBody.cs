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
            JsonElement idElement;
            if(doc.RootElement.TryGetProperty("id", out idElement))
            {
                Id = idElement.GetString();
                if (!string.IsNullOrWhiteSpace(Id))
                {
                    var uri = new Uri(Id);
                    // does the path match the id?
                    var uriParts = uri.PathAndQuery.Split('?');
                    if (uriParts.Length > 1)
                    {
                        ProcessingError = "Resource id includes a query string";
                    }
                    else
                    {
                        var uriPathElements = uriParts[0].Split('/');
                        IdPathElement = uriPathElements.Last();

                        // if the last element differs it might indicate that we are renaming
                        // if the id is missing we're asking to mint one (?)
                        // these things depend on what the thing is and what the operation is, we juse need to assemble data for the controller to decide.

                        //   /iiif/some/path/
                        //   /iiif/some/path/manifest
                        //   /iiif/some/path
                        //   /iiif/some/path/manifest/  xx

                        // see if the start elements are the same

                        // see if resource already exists

                    }
                }
            }

            var pathElements = path.Split('/');
            TargetPathElement = pathElements.Last();

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
