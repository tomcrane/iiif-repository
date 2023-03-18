using IIIFRepository;
using IIIFRespository.Requests;
using IIIFRespository.Responses;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace IIIFRespository.Controllers;


[Route("iiif")]
[ApiController]
public class IIIFController : ControllerBase
{
    private RepositorySettings RepoOptions { get; set; }
    public IIIFController(IOptions<RepositorySettings> repoOptions)
    {
        RepoOptions = repoOptions.Value;
    }

    private PathRequest GetPathRequest(string path)
    {
        return new PathRequest(RepoOptions.FileSystemRoot, path);
    }

    [HttpGet("{**path}")]
    public IActionResult Get(string path)
    {
        var pathRequest = GetPathRequest(path);

        switch (pathRequest.ResourceType)
        {
            case ResourceType.Manifest:
            case ResourceType.StoredCollection:
                return new PhysicalFileResult(pathRequest.BaseFile.FullName, Constants.PresentationContentType)
                {
                    // see if we get an etag anyway...
                    EntityTag = new Microsoft.Net.Http.Headers.EntityTagHeaderValue(pathRequest.GetETag())
                };
            case ResourceType.StorageCollection:
                var vcb = new StorageCollectionBuilder(pathRequest);
                Response.Headers.ETag = pathRequest.GetETag();
                return new ContentResult
                {
                    ContentType = Constants.PresentationContentType,
                    Content = vcb.Read()
                };
            default:
                return NotFound();
        }
    }


    [HttpPost("{**path}")]
    public IActionResult Post([FromBody] string value, string path)
    {
        // does not require an if-match because always a create.
        // May result in a conflict.
        var pathRequest = GetPathRequest(path);
        if(pathRequest.ResourceType != ResourceType.StorageCollection)
        {
            return BadRequest($"{path} is not a storage collection");
        }
        var body = new IIIFBody(value);
        if(body.IsCollectionWithNoItems)
        {
            // Create a new storage collection within this storage collection, using the supplied id (if present) to name.
            // if id is present must match path and supply a valid, non conflicting file name.

        }
        else if(body.IsCollectionWithItems || body.IsManifest)
        {
            // Add the resource (with extra file extensions)
            // using the naming conventions

        }
        else
        {
            return BadRequest($"Could not parse request body");
        }
    }

    [HttpPut("{**path}")]
    public void Put([FromBody] string value, string path)
    {
        // same ops as POST but direct to target?
        // Path must exist

        // can rename a storage collection this way, too: PUT url has different id from payload, rename (may be conflict)
        // but must be supplying entire JSON except items.
        // PUT a body with items (even the empty array) to a storage collection is an error.
    }

    [HttpPatch("{**path}")]
    public void Patch([FromBody] string value, string path)
    {
        // only for virtual collections (for now)? 
        // no, can also patch `id` of manifests, storedCollections and collections, to rename, but cannot EDIT 
        // manifests and stored collections like this.

        // so for stored collection, can patch:
        // id: rename
        // anything except items

        // manifest: id only.
    }

    [HttpDelete("{**path}")]
    public void Delete(string path)
    {
        // prevent deletion of populated directories?
    }
}
