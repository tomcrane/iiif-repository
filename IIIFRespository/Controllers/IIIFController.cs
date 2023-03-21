using IIIFRepository.Requests;
using IIIFRepository.Responses;
using IIIFRespository.Requests;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Mvc;
using System.Net;
using System.Text.Json;

namespace IIIFRepository.Controllers;


[Route(Constants.IIIFContainer)]
public class IIIFController : ControllerBase
{
    private Storage storage;

    public IIIFController(Storage storage)
    {
        this.storage = storage;
    }

    private ObjectResult SimpleProblem(HttpStatusCode statusCode, string message, string? detail = null)
    {
        return Problem(detail, "IIIF Respository", (int)statusCode, message, null);
    }



    [HttpGet("{**path}")]
    public IActionResult Get(string? path)
    {
        // don't use path, use Request.Path, to see trailing slashes etc
        path = Request.Path.ToString();
        var pathRequest = storage.GetPathRequest(path);
        string eTag = "\"" + pathRequest.GetETag() + "\"";
        switch (pathRequest.ResourceType)
        {
            case ResourceType.Manifest:
            case ResourceType.StoredCollection:
                return new PhysicalFileResult(pathRequest.BaseFile.FullName, Constants.PresentationContentType)
                {
                    // see if we get an etag anyway...
                    EntityTag = new Microsoft.Net.Http.Headers.EntityTagHeaderValue(eTag)
                };
            case ResourceType.StorageCollection:
                if (!path.EndsWith('/'))
                {
                    return Redirect(path + "/");
                }
                var vcb = new StorageCollectionBuilder(pathRequest);
                Response.Headers.ETag = eTag;
                return new ContentResult()
                {
                    ContentType = Constants.PresentationContentType,
                    Content = vcb.Read()
                };
            default:
                return SimpleProblem(HttpStatusCode.NotFound, "Not found");
        }
    }


    [HttpPost("{**path}")]
    public IActionResult Post([FromBody] JsonDocument value, string? path)
    {
        path = Request.Path.ToString();
        // does not require an if-match because always a create.
        // May result in a conflict.
        var pathRequest = storage.GetPathRequest(path);
        if(pathRequest.ResourceType != ResourceType.StorageCollection)
        {
            return SimpleProblem(HttpStatusCode.BadRequest, "Not a storage collection",
                $"{path} is not a Storage Collection. You can only POST to a Storage Collection.");
        }

        var iiifBody = new IIIFBody(value, path);
        if (iiifBody.IdAndPathAreTheSame)
        {
            return SimpleProblem(HttpStatusCode.BadRequest, "Cannot update with POST",
                $"POSTed id is the Storage Collection. You can't update this with a POST.");
        }

        // The body id must either be absent (in which case we'll name this) or it must be a child of the storage container

        var containerUrl = Request.GetEncodedUrl();
        if (!containerUrl.EndsWith('/')) containerUrl += "/";

        string resourcePathName;
        if (iiifBody.HasId)
        {
            if (string.IsNullOrWhiteSpace(iiifBody.ParentId))
            {
                return SimpleProblem(HttpStatusCode.BadRequest, "No parent container",
                    $"No parent container derivable from supplied id: {iiifBody.Id}");
            }
            if (string.IsNullOrWhiteSpace(iiifBody.LastIdElement))
            {
                return SimpleProblem(HttpStatusCode.BadRequest, "Invalid id",
                    $"No resource path element derivable from supplied id: {iiifBody.Id}");
            }
            if (pathRequest.CanonicalStorageCollectionPath != iiifBody.ParentId)
            {
                return SimpleProblem(HttpStatusCode.BadRequest, "Invalid id - not a child path",
                    $"POSTed id {iiifBody.Id} is not a child path of the Storage Collection {path}.");
            }
            resourcePathName = iiifBody.LastIdElement;
        }
        else
        {
            resourcePathName = Guid.NewGuid().ToString();
            iiifBody.SetId(containerUrl + resourcePathName);
        }

        if(storage.Exists(pathRequest, resourcePathName))
        {
            return SimpleProblem(HttpStatusCode.Conflict, "Conflict",
                $"An item named {resourcePathName} already exists in the container {path}");
        }


        if(iiifBody.IsCollectionWithNoItems)
        {
            if (iiifBody.IdEndsWithSlash)
            {
                // Create a new storage collection within this storage collection, using the supplied id (if present) to name.
                // if id is present must match path and supply a valid, non conflicting file name.
                storage.CreateStorageCollection(pathRequest, resourcePathName, iiifBody);
                Response.ContentType = Constants.PresentationContentType;
                return Created(containerUrl + resourcePathName + "/", iiifBody.JsonBody);
            }
            return SimpleProblem(HttpStatusCode.BadRequest, "Id must end with /",
                $"Storage Collection id must end with '/' ({iiifBody.Id}");
        }
        else if(iiifBody.IsCollectionWithItems || iiifBody.IsManifest)
        {
            if (iiifBody.IdEndsWithSlash)
            {
                return SimpleProblem(HttpStatusCode.BadRequest, "Id must NOT end with /",
                    $"Stored resource id must NOT end with '/' ({iiifBody.Id}");
            }
            storage.CreateStoredResource(pathRequest, resourcePathName, iiifBody);
            Response.ContentType = Constants.PresentationContentType;
            return Created(containerUrl + resourcePathName, iiifBody.JsonBody);
        }
        else
        {
            return SimpleProblem(HttpStatusCode.BadRequest, "Bad Request", "Could not parse request body");
        }
    }

    [HttpPut("{**path}")]
    public void Put([FromBody] JsonDocument value, string? path)
    {
        // same ops as POST but direct to target?
        // Path must exist

        // can rename a storage collection this way, too: PUT url has different id from payload, rename (may be conflict)
        // but must be supplying entire JSON except items.
        // PUT a body with items (even the empty array) to a storage collection is an error.
    }

    [HttpPatch("{**path}")]
    public void Patch([FromBody] string value, string? path)
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
