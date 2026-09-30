using System;
using System.Threading.Tasks;
using Xunit;

namespace Camunda.Api.Client.Tests
{
    public class JobApiTests
    {
        [Fact]
        public async Task Get_ReturnsJob()
        {
            var handler = new TestHttpMessageHandler("{\"id\":\"j-1\",\"retries\":3,\"priority\":5}");
            var client = handler.CreateClient();

            var job = await client.Jobs["j-1"].Get();

            Assert.Equal(System.Net.Http.HttpMethod.Get, handler.Last.Method);
            Assert.Equal("/job/j-1", handler.Last.PathAndQuery);
            Assert.Equal("j-1", job.Id);
            Assert.Equal(3, job.Retries);
        }

        [Fact]
        public async Task Query_PostsJobQuery()
        {
            var handler = new TestHttpMessageHandler("[]");
            var client = handler.CreateClient();

            var jobs = await client.Jobs.Query().List(0, 5);

            Assert.NotNull(jobs);
            Assert.Equal(System.Net.Http.HttpMethod.Post, handler.Last.Method);
            Assert.Equal("/job?firstResult=0&maxResults=5", handler.Last.PathAndQuery);
        }

        [Fact]
        public async Task QueryCount_UsesCountEndpoint()
        {
            var handler = new TestHttpMessageHandler("{\"count\":4}");
            var client = handler.CreateClient();

            Assert.Equal(4, await client.Jobs.Query().Count());
            Assert.Equal("/job/count", handler.Last.PathAndQuery);
        }

        [Fact]
        public async Task Execute_PostsToExecuteEndpoint()
        {
            var handler = new TestHttpMessageHandler("null");
            var client = handler.CreateClient();

            await client.Jobs["j-1"].Execute();

            Assert.Equal(System.Net.Http.HttpMethod.Post, handler.Last.Method);
            Assert.Equal("/job/j-1/execute", handler.Last.PathAndQuery);
        }

        [Fact]
        public async Task SetDuedate_SendsIso8601Date()
        {
            var handler = new TestHttpMessageHandler("null");
            var client = handler.CreateClient();

            await client.Jobs["j-1"].SetDuedate(new DateTime(2020, 5, 1, 12, 30, 0, DateTimeKind.Utc));

            Assert.Equal(System.Net.Http.HttpMethod.Put, handler.Last.Method);
            Assert.Equal("/job/j-1/duedate", handler.Last.PathAndQuery);
            Assert.Matches(@"^\{""duedate"":""\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}\.\d{3}[+-]\d{4}""\}$", handler.Last.Body);
        }

        [Fact]
        public async Task SetPriority_SendsNumber()
        {
            var handler = new TestHttpMessageHandler("null");
            var client = handler.CreateClient();

            await client.Jobs["j-1"].SetPriority(42);

            Assert.Equal(System.Net.Http.HttpMethod.Put, handler.Last.Method);
            Assert.Equal("/job/j-1/priority", handler.Last.PathAndQuery);
            Assert.Equal("{\"priority\":42}", handler.Last.Body);
        }

        [Fact]
        public async Task SetRetries_SendsNumber()
        {
            var handler = new TestHttpMessageHandler("null");
            var client = handler.CreateClient();

            await client.Jobs["j-1"].SetRetries(3);

            Assert.Equal("/job/j-1/retries", handler.Last.PathAndQuery);
            Assert.Equal("{\"retries\":3}", handler.Last.Body);
        }

        [Fact]
        public async Task UpdateSuspensionState_ForSingleJob()
        {
            var handler = new TestHttpMessageHandler("null");
            var client = handler.CreateClient();

            await client.Jobs["j-1"].UpdateSuspensionState(true);

            Assert.Equal("/job/j-1/suspended", handler.Last.PathAndQuery);
            Assert.Equal("{\"suspended\":true}", handler.Last.Body);
        }

        [Fact]
        public async Task UpdateSuspensionState_ForFilteredJobs()
        {
            var handler = new TestHttpMessageHandler("null");
            var client = handler.CreateClient();

            await client.Jobs.UpdateSuspensionState(new Job.JobSuspensionState
            {
                Suspended = true,
                ProcessDefinitionKey = "pk-1"
            });

            Assert.Equal(System.Net.Http.HttpMethod.Put, handler.Last.Method);
            Assert.Equal("/job/suspended", handler.Last.PathAndQuery);
            Assert.Equal(
                "{\"processDefinitionKey\":\"pk-1\",\"processDefinitionWithoutTenantId\":false,\"suspended\":true}",
                handler.Last.Body);
        }

        [Fact]
        public async Task GetStacktrace_ReturnsRawContent()
        {
            var handler = new TestHttpMessageHandler("java.lang.RuntimeException: boom")
            {
                ResponseMediaType = "text/plain"
            };
            var client = handler.CreateClient();

            var stacktrace = await client.Jobs["j-1"].GetStacktrace();

            Assert.Equal(System.Net.Http.HttpMethod.Get, handler.Last.Method);
            Assert.Equal("/job/j-1/stacktrace", handler.Last.PathAndQuery);
            Assert.Equal("java.lang.RuntimeException: boom", stacktrace);
        }

        [Fact]
        public async Task Delete_UsesDeleteMethod()
        {
            var handler = new TestHttpMessageHandler("null");
            var client = handler.CreateClient();

            await client.Jobs["j-1"].Delete();

            Assert.Equal(System.Net.Http.HttpMethod.Delete, handler.Last.Method);
            Assert.Equal("/job/j-1", handler.Last.PathAndQuery);
        }

        [Fact]
        public async Task SetJobRetries_CreatesBatch()
        {
            var handler = new TestHttpMessageHandler("{\"id\":\"batch-1\"}");
            var client = handler.CreateClient();

            var batch = await client.Jobs.SetJobRetries(new Job.JobRetries
            {
                JobIds = { "j-1", "j-2" },
                Retries = 5
            });

            Assert.Equal(System.Net.Http.HttpMethod.Post, handler.Last.Method);
            Assert.Equal("/job/retries", handler.Last.PathAndQuery);
            Assert.Contains("\"jobIds\":[\"j-1\",\"j-2\"]", handler.Last.Body);
            Assert.Contains("\"retries\":5", handler.Last.Body);
            Assert.Equal("batch-1", batch.Id);
        }

        [Fact]
        public async Task JobResource_ToString_ReturnsId()
        {
            var client = CamundaClient.Create("http://localhost:8080/engine-rest");

            Assert.Equal("j-1", client.Jobs["j-1"].ToString());
        }
    }
}
