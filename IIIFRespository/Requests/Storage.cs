using IIIFRepository;
using IIIFRepository.Requests;
using Microsoft.Extensions.Options;
using System.IO;
using static System.Net.Mime.MediaTypeNames;

namespace IIIFRespository.Requests
{
    public class Storage
    {
        private readonly RepositorySettings settings;

        public Storage(IOptions<RepositorySettings> repoOptions) 
        {
            settings = repoOptions.Value;
        }


        public PathRequest GetPathRequest(string path)
        {
            return new PathRequest(settings.FileSystemRoot, path);
        }

        public bool Exists(PathRequest pathRequest, string resourcePathName)
        {
            var tests = new string[]
            {
                resourcePathName,
                resourcePathName + Constants.ManifestSuffix,
                resourcePathName + Constants.CollectionSuffix,
                Constants.StorageCollectionFile,
                Constants.StorageCollectionItemsFile
            };
            foreach (var test in tests) 
            { 
                var path = Path.Combine(pathRequest.ParentDirectory.FullName, test);
                if (Directory.Exists(path))
                {
                    return true;
                }
                if (File.Exists(path))
                {
                    return true;
                }
            }
            return false;
        }

        public void CreateStorageCollection(PathRequest pathRequest, string resourcePathName, IIIFBody iiifBody)
        {
            var path = Path.Combine(pathRequest.ParentDirectory.FullName, resourcePathName);
            Directory.CreateDirectory(path);
            var collFile = Path.Combine(path, Constants.StorageCollectionFile);
            File.WriteAllText(collFile, iiifBody.RawBody);
            var itemsFile = Path.Combine(path, Constants.StorageCollectionItemsFile);
            File.WriteAllText(itemsFile, "{\"items\":[]}");
        }

        public void CreateStoredResource(PathRequest pathRequest, string resourcePathName, IIIFBody iiifBody)
        {
            var path = Path.Combine(pathRequest.ParentDirectory.FullName, resourcePathName);
            if(iiifBody.IsManifest)
            {
                File.WriteAllText(path + Constants.ManifestSuffix, iiifBody.RawBody);
            } 
            else if (iiifBody.IsCollectionWithItems)
            {
                File.WriteAllText(path + Constants.CollectionSuffix, iiifBody.RawBody);
            }
            else
            {
                throw new InvalidDataException("Repository can only save a Manifest or a Collection");
            }
        }

        public void GenerateCollectionItems(string collectionPath)
        {
            // In a real impl you'd store the collection membership info in a DB of some sort.
            // This is wholly file system based, but the filenames alone aren't enough, we can't store language maps etc
            // So instead we can collect id, type, label and thumbnail from all the objects in the collection.

            var fsPath = Path.Combine(settings.FileSystemRoot, Constants.IIIFContainer, collectionPath);
            var di = new DirectoryInfo(fsPath);
            foreach(var fsi in di.EnumerateFileSystemInfos())
            {
                FileInfo resource;
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

                // generate the `items` property. Use iiif-net? Or just build JSON... latter.
                // Need to read each 
                // save the items property to the Constants.StorageCollectionItemsFile path.
            }

        }
    }
}
