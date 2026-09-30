using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Camunda.Api.Client.Tests
{
    public class ErrorHandlingTests
    {
        private class StubHandler : HttpMessageHandler
        {
            private readonly HttpStatusCode _status;
            private readonly string _content;
            private readonly string _mediaType;

            public StubHandler(HttpStatusCode status, string content, string mediaType)
            {
                _status = status;
                _content = content;
                _mediaType = mediaType;
            }

            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                var response = new HttpResponseMessage(_status)
                {
                    Content = new StringContent(_content, System.Text.Encoding.UTF8, _mediaType)
                };
                return Task.FromResult(response);
            }
        }

        private static Task<HttpResponseMessage> Send(HttpMessageHandler handler) =>
            new HttpClient(handler).GetAsync("http://localhost:8080/engine-rest/process-instance/pi-1");

        private static ApiException FindApiException(Exception exception)
        {
            while (exception != null)
            {
                if (exception is ApiException apiException)
                    return apiException;
                exception = exception.InnerException;
            }
            return null;
        }

        [Fact]
        public async Task JsonError_WithKnownType_ThrowsTypedExceptionWithProperties()
        {
            var handler = new ErrorMessageHandler(new StubHandler(
                HttpStatusCode.Forbidden,
                "{\"type\":\"AuthorizationException\",\"message\":\"forbidden\"," +
                "\"userId\":\"kermit\",\"permissionName\":\"READ\"," +
                "\"resourceName\":\"processInstance\",\"resourceId\":\"*\"}",
                "application/json"));

            var exception = await Assert.ThrowsAsync<AuthorizationException>(() => Send(handler));

            Assert.Equal("AuthorizationException", exception.ErrorType);
            Assert.Equal("forbidden", exception.Message);
            Assert.Equal("kermit", exception.UserId);
            Assert.Equal("READ", exception.PermissionName);
            Assert.Equal("processInstance", exception.ResourceName);
            Assert.Equal("*", exception.ResourceId);
            Assert.NotNull(exception.Response);
            Assert.Equal(HttpStatusCode.Forbidden, exception.Response.StatusCode);
            Assert.IsType<AuthorizationException>(exception);
            Assert.IsAssignableFrom<ApiException>(exception);
        }

        [Fact]
        public async Task JsonError_WithNestedType_ThrowsDerivedException()
        {
            var handler = new ErrorMessageHandler(new StubHandler(
                HttpStatusCode.BadRequest,
                "{\"type\":\"InvalidRequestException\",\"message\":\"nope\"}",
                "application/json"));

            var exception = await Assert.ThrowsAsync<InvalidRequestException>(() => Send(handler));

            Assert.Equal("InvalidRequestException", exception.ErrorType);
            Assert.Equal("nope", exception.Message);
            Assert.IsAssignableFrom<RestException>(exception);
            Assert.IsAssignableFrom<RuntimeException>(exception);
        }

        [Fact]
        public async Task JsonError_WithUnknownType_FallsBackToBaseException()
        {
            var handler = new ErrorMessageHandler(new StubHandler(
                HttpStatusCode.InternalServerError,
                "{\"type\":\"TotallyUnknownException\",\"message\":\"boom\"}",
                "application/json"));

            var exception = await Assert.ThrowsAsync<ApiException>(() => Send(handler));

            Assert.Equal("TotallyUnknownException", exception.ErrorType);
            Assert.Equal("boom", exception.Message);
            Assert.IsNotType<InvalidRequestException>(exception);
        }

        [Fact]
        public async Task JsonError_WithExtraProperties_FillsExceptionMembers()
        {
            var handler = new ErrorMessageHandler(new StubHandler(
                HttpStatusCode.BadRequest,
                "{\"type\":\"BpmnError\",\"message\":\"business fault\",\"errorCode\":\"ERROR-1\",\"errorMessage\":\"detail\"}",
                "application/json"));

            var exception = await Assert.ThrowsAsync<BpmnError>(() => Send(handler));

            Assert.Equal("ERROR-1", exception.ErrorCode);
            Assert.Equal("detail", exception.ErrorMessage);
        }

        [Fact]
        public async Task ErrorResponseWithoutType_PassesThrough()
        {
            var handler = new ErrorMessageHandler(new StubHandler(
                HttpStatusCode.BadRequest,
                "{\"message\":\"no type here\"}",
                "application/json"));

            var response = await Send(handler);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task NonJsonErrorResponse_PassesThrough()
        {
            var handler = new ErrorMessageHandler(new StubHandler(
                HttpStatusCode.InternalServerError,
                "<html><body>boom</body></html>",
                "text/html"));

            var response = await Send(handler);

            Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        }

        [Fact]
        public async Task MalformedJsonErrorResponse_PassesThrough()
        {
            var handler = new ErrorMessageHandler(new StubHandler(
                HttpStatusCode.BadRequest,
                "this is not json",
                "application/json"));

            var response = await Send(handler);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task SuccessResponse_PassesThrough()
        {
            var handler = new ErrorMessageHandler(new StubHandler(
                HttpStatusCode.OK,
                "{\"id\":\"pi-1\"}",
                "application/json"));

            var response = await Send(handler);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("{\"id\":\"pi-1\"}", await response.Content.ReadAsStringAsync());
        }

        [Fact]
        public async Task Client_SurfacesTypedApiException()
        {
            var handler = new TestHttpMessageHandler(
                "{\"type\":\"AuthorizationException\",\"message\":\"forbidden\",\"userId\":\"kermit\"}")
            {
                Status = HttpStatusCode.Forbidden
            };
            var client = handler.CreateClient();

            var exception = FindApiException(await Assert.ThrowsAnyAsync<Exception>(
                () => client.ProcessInstances["pi-1"].Get()));

            Assert.NotNull(exception);
            Assert.IsType<AuthorizationException>(exception);
            Assert.Equal("kermit", ((AuthorizationException)exception).UserId);
        }

        [Fact]
        public async Task Client_SurfacesUnknownErrorAsBaseApiException()
        {
            var handler = new TestHttpMessageHandler(
                "{\"type\":\"MysteryException\",\"message\":\"huh\"}")
            {
                Status = HttpStatusCode.NotFound
            };
            var client = handler.CreateClient();

            var exception = FindApiException(await Assert.ThrowsAnyAsync<Exception>(
                () => client.ProcessInstances["pi-1"].Get()));

            Assert.NotNull(exception);
            Assert.Equal("MysteryException", exception.ErrorType);
            Assert.Equal("huh", exception.Message);
        }

        [Fact]
        public async Task Client_WithoutErrorMessageHandler_DoesNotTranslateErrors()
        {
            var handler = new TestHttpMessageHandler(
                "{\"type\":\"AuthorizationException\",\"message\":\"forbidden\"}")
            {
                Status = HttpStatusCode.Forbidden
            };
            var client = handler.CreateRawClient();

            var exception = FindApiException(await Assert.ThrowsAnyAsync<Exception>(
                () => client.ProcessInstances["pi-1"].Get()));

            Assert.Null(exception);
        }
    }
}
