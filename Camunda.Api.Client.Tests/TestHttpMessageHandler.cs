using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Camunda.Api.Client.Tests
{
    public class RecordedRequest
    {
        public HttpMethod Method;
        public string Url;
        public string PathAndQuery;
        public string Body;
        public string ContentType;
    }

    /// <summary>
    /// Records every request and answers with a configurable canned response.
    /// </summary>
    public class TestHttpMessageHandler : HttpMessageHandler
    {
        private const string Host = "http://localhost:8080/engine-rest";

        public HttpStatusCode Status { get; set; } = HttpStatusCode.OK;
        public string ResponseJson { get; set; } = "null";
        public string ResponseMediaType { get; set; } = "application/json";

        public List<RecordedRequest> Requests { get; } = new List<RecordedRequest>();

        public RecordedRequest Last => Requests[Requests.Count - 1];

        public TestHttpMessageHandler() { }

        public TestHttpMessageHandler(string responseJson)
        {
            ResponseJson = responseJson;
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var url = request.RequestUri.ToString();

            Requests.Add(new RecordedRequest
            {
                Method = request.Method,
                Url = url,
                PathAndQuery = url.StartsWith(Host) ? url.Substring(Host.Length) : url,
                Body = request.Content == null ? null : await request.Content.ReadAsStringAsync(cancellationToken),
                ContentType = request.Content?.Headers?.ContentType?.ToString()
            });

            return new HttpResponseMessage(Status)
            {
                Content = new StringContent(ResponseJson, Encoding.UTF8, ResponseMediaType)
            };
        }

        /// <summary>
        /// Creates a client which translates Camunda error responses into typed <see cref="ApiException"/>s.
        /// </summary>
        public CamundaClient CreateClient() =>
            CamundaClient.Create(Host, new ErrorMessageHandler(this));

        /// <summary>
        /// Creates a client without the <see cref="ErrorMessageHandler"/> (the raw handler is used as-is).
        /// </summary>
        public CamundaClient CreateRawClient() => CamundaClient.Create(Host, this);
    }
}
