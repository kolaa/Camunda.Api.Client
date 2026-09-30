using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Camunda.Api.Client.Deployment;
using Xunit;

namespace Camunda.Api.Client.Tests
{
    public class DeploymentApiTests
    {
        [Fact]
        public async Task Query_UsesGetWithQueryParameters()
        {
            var handler = new TestHttpMessageHandler("[]");
            var client = handler.CreateClient();

            var query = new DeploymentQuery { Id = "d1", Name = "n" };
            var result = await client.Deployments.Query(query).List(5, 15);

            Assert.NotNull(result);
            Assert.Equal(System.Net.Http.HttpMethod.Get, handler.Last.Method);
            Assert.Null(handler.Last.ContentType);
            Assert.Equal(
                "/deployment?id=d1&name=n&withoutSource=false&withoutTenantId=false" +
                "&includeDeploymentsWithoutTenantId=false&sortBy=id&sortOrder=asc" +
                "&firstResult=5&maxResults=15",
                handler.Last.PathAndQuery);
        }

        [Fact]
        public async Task QueryCount_UsesCountEndpoint()
        {
            var handler = new TestHttpMessageHandler("{\"count\":3}");
            var client = handler.CreateClient();

            var count = await client.Deployments.Query(new DeploymentQuery { Name = "n" }).Count();

            Assert.Equal(3, count);
            Assert.Equal(
                "/deployment/count?name=n&withoutSource=false&withoutTenantId=false" +
                "&includeDeploymentsWithoutTenantId=false&sortBy=id&sortOrder=asc",
                handler.Last.PathAndQuery);
        }

        [Fact]
        public async Task Query_WithDateFilter_FormatsDateWithLocalOffset()
        {
            var handler = new TestHttpMessageHandler("[]");
            var client = handler.CreateClient();

            var date = new DateTime(2020, 5, 1, 12, 30, 0, DateTimeKind.Utc);
            await client.Deployments.Query(new DeploymentQuery { Before = date }).List();

            // dates are converted to local time and url encoded by refit
            var expectedDate = date.ToLocalTime().ToJavaISO8601().Replace(":", "%3A").Replace("+", "%2B");

            Assert.Equal(
                "/deployment?withoutSource=false&before=" + expectedDate +
                "&withoutTenantId=false&includeDeploymentsWithoutTenantId=false&sortBy=id&sortOrder=asc",
                handler.Last.PathAndQuery);
        }

        [Fact]
        public async Task Create_SendsMultipartDeployment()
        {
            var handler = new TestHttpMessageHandler("{\"id\":\"dep-1\",\"name\":\"my-deployment\"}");
            var client = handler.CreateClient();

            var resource = new ResourceDataContent(
                new MemoryStream(Encoding.UTF8.GetBytes("<xml/>")), "proc.bpmn");

            var deployment = await client.Deployments.Create(
                "my-deployment",
                duplicateFiltering: true,
                changedOnly: false,
                deploymentSource: "source",
                tenantId: null,
                resource);

            Assert.Equal("dep-1", deployment.Id);
            Assert.Equal(System.Net.Http.HttpMethod.Post, handler.Last.Method);
            Assert.Equal("/deployment/create", handler.Last.PathAndQuery);
            Assert.StartsWith("multipart/form-data", handler.Last.ContentType);

            Assert.Contains("name=deployment-name", handler.Last.Body);
            Assert.Contains("my-deployment", handler.Last.Body);
            Assert.Contains("name=enable-duplicate-filtering", handler.Last.Body);
            Assert.Contains("true", handler.Last.Body);
            Assert.Contains("name=deploy-changed-only", handler.Last.Body);
            Assert.Contains("name=deployment-source", handler.Last.Body);
            Assert.Contains("source", handler.Last.Body);
            Assert.Contains("filename=proc.bpmn", handler.Last.Body);
            Assert.Contains("<xml/>", handler.Last.Body);
            Assert.Contains("application/octet-stream", handler.Last.Body);
        }

        [Fact]
        public async Task Create_WithShortSignature_UsesDefaults()
        {
            var handler = new TestHttpMessageHandler("{\"id\":\"dep-1\"}");
            var client = handler.CreateClient();

            var resource = new ResourceDataContent(
                new MemoryStream(Encoding.UTF8.GetBytes("<xml/>")), "proc.bpmn");

            await client.Deployments.Create("only-name", resource);

            Assert.Contains("name=enable-duplicate-filtering", handler.Last.Body);
            Assert.Contains("\r\nfalse\r\n", handler.Last.Body);
            Assert.Contains("undefined", handler.Last.Body);
        }

        [Fact]
        public async Task Delete_PassesCascadeFlagsAsLowercaseQuery()
        {
            var handler = new TestHttpMessageHandler("null");
            var client = handler.CreateClient();

            await client.Deployments["dep-1"].Delete(cascade: true, skipCustomListeners: false, skipIoMappings: true);

            Assert.Equal(System.Net.Http.HttpMethod.Delete, handler.Last.Method);
            Assert.Equal(
                "/deployment/dep-1?cascade=true&skipCustomListeners=false&skipIoMappings=true",
                handler.Last.PathAndQuery);
        }

        [Fact]
        public async Task Get_ReturnsDeployment()
        {
            var handler = new TestHttpMessageHandler("{\"id\":\"dep-1\",\"name\":\"n\",\"source\":\"src\"}");
            var client = handler.CreateClient();

            var deployment = await client.Deployments["dep-1"].Get();

            Assert.Equal("/deployment/dep-1", handler.Last.PathAndQuery);
            Assert.Equal("dep-1", deployment.Id);
            Assert.Equal("n", deployment.Name);
            Assert.Equal("src", deployment.Source);
        }
    }
}

