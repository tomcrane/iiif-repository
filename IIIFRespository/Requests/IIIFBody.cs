namespace IIIFRespository.Requests;

public class IIIFBody
{
    public IIIFBody(string body)
    {

    }

    public bool IsCollectionWithNoItems { get; internal set; }
    public bool IsCollectionWithItems { get; internal set; }
    public bool IsManifest { get; internal set; }
}
