using IIIF;
using IIIF.Serialisation;
using System.Text;

namespace ProtocolTests
{
    public static class Helpers
    {
        public static HttpContent ToHttpContent(this JsonLdBase iiifResource)
        {
            var content = new StringContent(
                iiifResource.AsJson(),
                Encoding.UTF8,
                "application/json"); // make this the proper one...

            return content;
        }
    }
}
