using System.Text.Json;

namespace IIIFRepository.Requests;

public class IIIFBody
{
    public IIIFBody(JsonDocument jsonBody, string path)
    {
        JsonBody = jsonBody;

        try
        {
            //using var doc = JsonDocument.Parse(rawBody);
            Type = jsonBody.RootElement.GetProperty("type").GetString();
            JsonElement idElement;
            if(jsonBody.RootElement.TryGetProperty("id", out idElement))
            {
                Id = idElement.GetString();
                if (!string.IsNullOrWhiteSpace(Id))
                {
                    HasId = true;
                    var uri = new Uri(Id);
                    // does the path match the id?
                    var uriParts = uri.PathAndQuery.Split('?');
                    if (uriParts.Length > 1)
                    {
                        ProcessingError = "Resource id includes a query string";
                    }
                    else
                    {
                        var idPath = uriParts[0];
                        var idPathElements = idPath.Split('/', StringSplitOptions.RemoveEmptyEntries);
                        LastIdElement = idPathElements.Last();
                        int endLength = LastIdElement.Length;
                        if (idPath.EndsWith('/'))
                        {
                            endLength++;
                        }
                        ParentId = idPath.Substring(0, idPath.Length - endLength); // will end with /

                        if(idPath.TrimEnd('/') == path.TrimEnd('/'))
                        {
                            IdAndPathAreTheSame = true;
                        }

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

            var pathElements = path.Split('/', StringSplitOptions.RemoveEmptyEntries);
            LastPathElement = pathElements.Last();

            JsonElement items;
            int itemCount = -1;
            if (jsonBody.RootElement.TryGetProperty("items", out items))
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

    //public string RawBody { get; internal set; }
    public JsonDocument JsonBody { get; internal set; }
    public string? Id { get; internal set; }
    public bool HasId { get; internal set; }
    public string? LastPathElement { get; internal set; }
    public string? LastIdElement { get; internal set; }
    public string? ParentId { get; internal set; }
    public bool IdAndPathAreTheSame { get; internal set; }

    public string? Type { get; internal set; }

    public string? ProcessingError { get; internal set; }


    public bool IsCollectionWithNoItems { get; internal set; }
    public bool IsCollectionWithItems { get; internal set; }
    public bool IsManifest { get; internal set; }

    public void SetId(string id)
    {
        throw new NotImplementedException("Need to manipulate the body JSON to set the ID");
        // then update RawBody
    }
}
