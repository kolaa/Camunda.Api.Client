using System;
using System.Net;
using System.Net.Http;
using Xunit;

namespace Camunda.Api.Client.Tests
{
    public class ApiExceptionTests
    {
        private static readonly HttpResponseMessage Response =
            new HttpResponseMessage(HttpStatusCode.InternalServerError);

        private static ApiException FromType(string type) =>
            ApiException.FromRestError(new RestError { Type = type, Message = "msg" }, Response);

        [Theory]
        [InlineData("AuthorizationException", typeof(AuthorizationException))]
        [InlineData("InvalidRequestException", typeof(InvalidRequestException))]
        [InlineData("BadUserRequestException", typeof(BadUserRequestException))]
        [InlineData("BpmnParseException", typeof(BpmnParseException))]
        [InlineData("OptimisticLockingException", typeof(OptimisticLockingException))]
        [InlineData("TaskAlreadyClaimedException", typeof(TaskAlreadyClaimedException))]
        [InlineData("NullValueException", typeof(NullValueException))]
        [InlineData("CaseException", typeof(CaseException))]
        [InlineData("DecisionException", typeof(DecisionException))]
        [InlineData("DeploymentResourceNotFoundException", typeof(DeploymentResourceNotFoundException))]
        [InlineData("ServerBootstrapException", typeof(ServerBootstrapException))]
        [InlineData("JSONException", typeof(JSONException))]
        [InlineData("UnknownTypeHere", typeof(ApiException))]
        public void FromRestError_MapsTypeToExceptionClass(string type, Type expected)
        {
            var exception = FromType(type);

            Assert.IsType(expected, exception);
            Assert.Equal(type, exception.ErrorType);
            Assert.Equal("msg", exception.Message);
            Assert.Same(Response, exception.Response);
        }

        [Fact]
        public void FromRestError_CachesConstructorPerTypeName()
        {
            var first = FromType("NullValueException");
            var second = FromType("NullValueException");

            Assert.IsType<NullValueException>(first);
            Assert.IsType<NullValueException>(second);
        }

        [Fact]
        public void Hierarchy_MatchesCamundaExceptionTree()
        {
            Assert.IsAssignableFrom<ApiException>(new AuthorizationException(null, null, Response));
            Assert.IsAssignableFrom<ApiException>(new ProcessEngineException(null, null, Response));
            Assert.IsAssignableFrom<RuntimeException>(new RestException(null, null, Response));
            Assert.IsAssignableFrom<ProcessEngineException>(new BadUserRequestException(null, null, Response));
            Assert.IsAssignableFrom<RestException>(new InvalidRequestException(null, null, Response));
            Assert.IsAssignableFrom<ProcessEngineException>(new BpmnError(null, null, Response));
            Assert.IsAssignableFrom<RuntimeException>(new ServerBootstrapException(null, null, Response));
        }

        [Fact]
        public void BpmnError_ExposesErrorCodeAndErrorMessage()
        {
            var error = new BpmnError("BpmnError", "failed", Response);
            Newtonsoft.Json.JsonConvert.PopulateObject(
                "{\"errorCode\":\"ERROR-1\",\"errorMessage\":\"detail\"}", error);

            Assert.Equal("ERROR-1", error.ErrorCode);
            Assert.Equal("detail", error.ErrorMessage);
            Assert.Equal("failed", error.Message);
        }

        [Fact]
        public void ApiException_ExposesErrorTypeAndMessage()
        {
            var exception = new ApiException("CustomType", "something went wrong", Response);

            Assert.Equal("CustomType", exception.ErrorType);
            Assert.Equal("something went wrong", exception.Message);
            Assert.Same(Response, exception.Response);
        }
    }
}
