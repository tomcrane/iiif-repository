using System.Security.Cryptography;

namespace IIIFRespository.Requests;

public class PathRequest
{
    public PathRequest(string root, string path)
    {
        var fsPath = Path.Combine(root, "iiif", path);
        if (Directory.Exists(fsPath))
        {
            ResourceType = ResourceType.StorageCollection;
            // The ETag is based on this file, not the items:
            BaseFile = new FileInfo(Path.Combine(fsPath, Constants.StorageCollectionFile));
            ItemsFile = new FileInfo(Path.Combine(fsPath, Constants.StorageCollectionItemsFile));
        } 
        else if (File.Exists(fsPath + Constants.ManifestSuffix))
        {
            ResourceType = ResourceType.Manifest;
            BaseFile = new FileInfo(Path.Combine(fsPath, Constants.ManifestSuffix));
        }
        else if (File.Exists(fsPath + Constants.CollectionSuffix))
        {
            ResourceType = ResourceType.StoredCollection;
            BaseFile = new FileInfo(Path.Combine(fsPath, Constants.CollectionSuffix));
        }
        else
        {
            ResourceType = ResourceType.Unknown;
            BaseFile = new FileInfo(fsPath);
        }
    }

    public ResourceType ResourceType { get; private set; }
    public FileInfo BaseFile { get; private set; }
    public FileInfo? ItemsFile { get; private set; }

    public string GetETag()
    {
        return CalculateMD5(BaseFile);
    }


    protected static string CalculateMD5(FileInfo file)
    {
        using (var md5 = MD5.Create())
        {
            using (var stream = file.OpenRead())
            {
                var hash = md5.ComputeHash(stream);
                return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
            }
        }
    }
}

public enum ResourceType
{
    StorageCollection,
    StoredCollection,
    Manifest,
    Unknown
}