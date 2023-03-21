using System.Net.Http.Headers;
using System.Net.Http;
using System.Text;

namespace ProtocolTests
{
    public static class HttpClientX
    {
        public static async Task<HttpResponseMessage> PutAsyncWithETag(
            this HttpClient client,
            string requestUri,
            HttpContent content,
            string eTag)
        {
            using (var requestMessage = new HttpRequestMessage(HttpMethod.Put, requestUri))
            {
                requestMessage.Content = content;
                requestMessage.Headers.TryAddWithoutValidation("If-Match", eTag);

                return await client.SendAsync(requestMessage);
            }

        }
    }


}
