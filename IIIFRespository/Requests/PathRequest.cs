using System.Security.Cryptography;

namespace IIIFRepository.Requests;

public class PathRequest
{
    public PathRequest(string root, string pathFromUrl)
    {
        var localPath = pathFromUrl.Replace('/', Path.DirectorySeparatorChar);
        if (localPath.StartsWith(Path.DirectorySeparatorChar))
        {
            localPath = localPath.Substring(1);
        }

        var fsPath = Path.Combine(root, localPath);

        if (Directory.Exists(fsPath))
        {
            ResourceType = ResourceType.StorageCollection;
            // The ETag is based on this file, not the items:
            BaseFile = new FileInfo(Path.Combine(fsPath, Constants.StorageCollectionFile));
            ItemsFile = new FileInfo(Path.Combine(fsPath, Constants.StorageCollectionItemsFile));
            if (pathFromUrl.EndsWith('/'))
            {
                CanonicalStorageCollectionPath = pathFromUrl;
            } 
            else
            {
                IsStorageCollectionWithoutTrailingSlash = true;
                CanonicalStorageCollectionPath = pathFromUrl + "/";
            }
            ParentDirectory = BaseFile.Directory!.Parent!;
        } 
        else if (File.Exists(fsPath + Constants.ManifestSuffix))
        {
            ResourceType = ResourceType.Manifest;
            BaseFile = new FileInfo(fsPath + Constants.ManifestSuffix);
            ParentDirectory = BaseFile.Directory!;
        }
        else if (File.Exists(fsPath + Constants.CollectionSuffix))
        {
            ResourceType = ResourceType.StoredCollection;
            BaseFile = new FileInfo(fsPath + Constants.CollectionSuffix);
            ParentDirectory = BaseFile.Directory!;
        }
        else
        {
            ResourceType = ResourceType.Unknown;
            BaseFile = new FileInfo(fsPath);
            ParentDirectory = BaseFile.Directory!;
        }
        StorageDirectory = BaseFile.Directory!;
    }

    public ResourceType ResourceType { get; private set; }
    public FileInfo BaseFile { get; private set; }
    public FileInfo? ItemsFile { get; private set; }
    public DirectoryInfo ParentDirectory { get; private set; }
    public bool IsStorageCollectionWithoutTrailingSlash { get; private set; }
    public DirectoryInfo StorageDirectory { get; private set; }
    public string? CanonicalStorageCollectionPath { get; private set; }

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