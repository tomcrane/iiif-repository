using IIIFRepository;
using IIIFRepository.Requests;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.Extensions.Options;
using System.IO;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace IIIFRespository.Requests;

public class Storage
{
    private readonly RepositorySettings settings;
    private readonly ILogger<Storage> logger;

    private static object syncObject = new object();

    public Storage(
        IOptions<RepositorySettings> repoOptions,
        ILogger<Storage> logger,
        IHttpContextAccessor httpContextAccessor
    ) 
    {
        settings = repoOptions.Value;
        this.logger = logger;
        var iiifContainer = Path.Combine(settings!.FileSystemRoot, Constants.IIIFContainer);
        if (!Directory.Exists(iiifContainer))
        {
            Directory.CreateDirectory(iiifContainer);
            var collFile = Path.Combine(iiifContainer, Constants.StorageCollectionFile);
            var containerPart = $"/{Constants.IIIFContainer}/";
            string id = containerPart;
            if(httpContextAccessor.HttpContext != null)
            {
                id = httpContextAccessor.HttpContext.Request.GetDisplayUrl().Split(containerPart)[0] + containerPart;
            }
            File.WriteAllText(collFile, GetCollectionJson(id));
            var itemsFile = Path.Combine(iiifContainer, Constants.StorageCollectionItemsFile);
            File.WriteAllText(itemsFile, "{\"items\":[]}");
        }
    }


    public PathRequest GetPathRequest(string path)
    {
        return new PathRequest(settings.FileSystemRoot, path);
    }

    public bool Exists(PathRequest pathRequest, string resourcePathName)
    {
        var conflicts = new string[]
        {
            resourcePathName,
            resourcePathName + Constants.ManifestSuffix,
            resourcePathName + Constants.CollectionSuffix
        };
        foreach (var conflict in conflicts) 
        { 
            var path = Path.Combine(pathRequest.StorageDirectory!.FullName, conflict);
            if (Directory.Exists(path))
            {
                return true;
            }
            if (File.Exists(path))
            {
                return true;
            }
        }
        if (resourcePathName == Constants.StorageCollectionFile)
        {
            return true;
        }
        if (resourcePathName == Constants.StorageCollectionItemsFile)
        {
            return true;
        }
        return false;
    }

    public void CreateStorageCollection(PathRequest pathRequest, string resourcePathName, IIIFBody iiifBody)
    {
        var options = new JsonSerializerOptions { WriteIndented = true };
        var path = Path.Combine(pathRequest.StorageDirectory.FullName, resourcePathName);
        Directory.CreateDirectory(path);
        var collFile = Path.Combine(path, Constants.StorageCollectionFile);
        File.WriteAllText(collFile, JsonSerializer.Serialize(iiifBody.JsonBody, options));
        var itemsFile = Path.Combine(path, Constants.StorageCollectionItemsFile);
        File.WriteAllText(itemsFile, "{\"items\":[]}");
        GenerateCollectionItems(pathRequest.StorageDirectory.FullName);
    }

    public void CreateStoredResource(PathRequest pathRequest, string resourcePathName, IIIFBody iiifBody)
    {
        var options = new JsonSerializerOptions { WriteIndented = true };
        var path = Path.Combine(pathRequest.StorageDirectory.FullName, resourcePathName);
        if(iiifBody.IsManifest)
        {
            File.WriteAllText(path + Constants.ManifestSuffix, JsonSerializer.Serialize(iiifBody.JsonBody, options));
        } 
        else if (iiifBody.IsCollectionWithItems)
        {
            File.WriteAllText(path + Constants.CollectionSuffix, JsonSerializer.Serialize(iiifBody.JsonBody, options));
        }
        else
        {
            throw new InvalidDataException("Repository can only save a Manifest or a Collection");
        }
        GenerateCollectionItems(pathRequest.StorageDirectory.FullName);
    }

    private void GenerateCollectionItems(string collectionDirectory)
    {
        // In a real impl you'd store the collection membership info in a DB of some sort.
        // This is wholly file system based, but the filenames alone aren't enough, we can't store language maps etc
        // So instead we can collect id, type, label and thumbnail from all the objects in the collection.

        lock (syncObject)  // this is what makes this non-prod!
        {
            var di = new DirectoryInfo(collectionDirectory);
            var items = new JsonArray();
            foreach(var fsi in di.EnumerateFileSystemInfos())
            {
                FileInfo? resource = null;
                if(fsi.Attributes.HasFlag(FileAttributes.Directory))
                {
                    var childCollectionFile = Path.Combine(fsi.FullName, Constants.StorageCollectionFile);
                    resource = new FileInfo(childCollectionFile);

                }
                else if (fsi.Name.EndsWith(Constants.ManifestSuffix) || fsi.Name.EndsWith(Constants.CollectionSuffix))
                {
                    resource = (FileInfo) fsi;
                }
                // other files, skip

                if(resource != null) 
                {
                    try
                    {
                        using var stream = resource.OpenRead();
                        using var doc = JsonDocument.Parse(stream);
                        string id = doc.RootElement.GetProperty("id").GetString()!;
                        string type = doc.RootElement.GetProperty("type").GetString()!;
                        JsonElement label = doc.RootElement.GetProperty("label");
                        var item = new JsonObject()
                        {
                            ["id"] = id,
                            ["type"] = type,
                            ["label"] = label.Deserialize<JsonNode>()
                        };
                        JsonElement thumbnail;
                        if(doc.RootElement.TryGetProperty("thumbnail", out thumbnail))
                        {
                            item["thumbnail"] = thumbnail.Deserialize<JsonNode>();
                        }
                        items.Add(item);
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "Could not generate stub item for {file}", fsi.FullName);
                    }
                }
            }

            JsonNode itemsObj = new JsonObject()
            {
                ["items"] = items
            };
            var options = new JsonSerializerOptions { WriteIndented = true };
            var collectionFilePath = Path.Combine(di.FullName, Constants.StorageCollectionItemsFile);
            File.WriteAllText(collectionFilePath, itemsObj.ToJsonString(options));
        }
    }

    public string GetCollectionJson(string collectionId)
    {
        return $$"""
            {
                "@context": "{{Constants.PresentationContext}}",
                "id": "{{collectionId}}",
                "type": "Collection",
                "label": { "en": [ "IIIF Repository root" ] }
            }
            """;
}

    

}
