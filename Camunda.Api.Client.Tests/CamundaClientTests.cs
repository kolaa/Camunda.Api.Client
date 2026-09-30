using System.Net.Http;
using System.Threading.Tasks;
using Xunit;

namespace Camunda.Api.Client.Tests
{
    public class CamundaClientTests
    {
        private static void AssertAllServicesAreAvailable(CamundaClient client)
        {
            Assert.NotNull(client.CaseDefinitions);
            Assert.NotNull(client.CaseExecutions);
            Assert.NotNull(client.CaseInstances);
            Assert.NotNull(client.DecisionDefinitions);
            Assert.NotNull(client.Deployments);
            Assert.NotNull(client.Executions);
            Assert.NotNull(client.ExternalTasks);
            Assert.NotNull(client.Group);
            Assert.NotNull(client.History);
            Assert.NotNull(client.Incidents);
            Assert.NotNull(client.JobDefinitions);
            Assert.NotNull(client.Jobs);
            Assert.NotNull(client.Messages);
            Assert.NotNull(client.Migrations);
            Assert.NotNull(client.ProcessDefinitions);
            Assert.NotNull(client.ProcessInstances);
            Assert.NotNull(client.Signals);
            Assert.NotNull(client.Tenants);
            Assert.NotNull(client.Users);
            Assert.NotNull(client.UserTasks);
            Assert.NotNull(client.VariableInstances);
            Assert.NotNull(client.Filters);
        }

        [Fact]
        public void Create_WithHostUrl_ExposesAllServices()
        {
            AssertAllServicesAreAvailable(CamundaClient.Create("http://localhost:8080/engine-rest"));
        }

        [Fact]
        public void Create_WithHostUrlAndHandler_ExposesAllServices()
        {
            var handler = new TestHttpMessageHandler("null");
            AssertAllServicesAreAvailable(handler.CreateClient());
        }

        [Fact]
        public void Create_WithHttpClient_ExposesAllServices()
        {
            var httpClient = new HttpClient(new TestHttpMessageHandler("null"));
            AssertAllServicesAreAvailable(CamundaClient.Create(httpClient));
        }

        [Fact]
        public async Task Create_WithHttpClient_SendsRequestsWithBaseAddress()
        {
            var handler = new TestHttpMessageHandler("{\"id\":\"pi-1\"}");
            var httpClient = new HttpClient(handler)
            {
                BaseAddress = new System.Uri("http://localhost:8080/engine-rest")
            };

            var client = CamundaClient.Create(httpClient);
            var instance = await client.ProcessInstances["pi-1"].Get();

            Assert.Equal("pi-1", instance.Id);
            Assert.Equal("http://localhost:8080/engine-rest/process-instance/pi-1", handler.Last.Url);
        }

        [Fact]
        public void Create_DoesNotShareServiceInstances()
        {
            var client = CamundaClient.Create("http://localhost:8080/engine-rest");

            Assert.NotSame(client.ProcessInstances, client.ProcessInstances);
            Assert.NotSame(client.Deployments, client.Deployments);
        }

        [Fact]
        public void History_ExposesAllHistoricServices()
        {
            var history = CamundaClient.Create("http://localhost:8080/engine-rest").History;

            Assert.NotNull(history.ActivityInstances);
            Assert.NotNull(history.CaseDefinitions);
            Assert.NotNull(history.CaseInstances);
            Assert.NotNull(history.DecisionInstances);
            Assert.NotNull(history.Detail);
            Assert.NotNull(history.Incidents);
            Assert.NotNull(history.JobLogs);
            Assert.NotNull(history.ProcessInstances);
            Assert.NotNull(history.VariableInstances);
            Assert.NotNull(history.UserTasks);
            Assert.NotNull(history.ProcessDefinitions);
            Assert.NotNull(history.ExternalTaskLogs);
        }

        [Fact]
        public void ProcessInstanceResource_ToString_ReturnsId()
        {
            var client = CamundaClient.Create("http://localhost:8080/engine-rest");

            Assert.Equal("pi-1", client.ProcessInstances["pi-1"].ToString());
            Assert.Equal("dep-1", client.Deployments["dep-1"].ToString());
        }

        [Fact]
        public void CountResult_DeserializesCount()
        {
            var count = Newtonsoft.Json.JsonConvert.DeserializeObject<CountResult>(
                "{\"count\":5}", CamundaClient.JsonSerializerSettings);

            Assert.Equal(5, count.Count);
        }

        [Fact]
        public void StringExtensions_JoinsNullAndEmptySequences()
        {
            Assert.Null(StringExtensions.Join(null));
            Assert.Null(new string[0].Join());
            Assert.Equal("a,b", new[] { "a", "b" }.Join());
        }

        [Fact]
        public void SortingInfo_ToString_ContainsSortByAndOrder()
        {
            var sorting = new SortingInfo<ProcessInstance.ProcessInstanceSorting>
            {
                SortBy = ProcessInstance.ProcessInstanceSorting.InstanceId,
                SortOrder = SortOrder.Descending
            };

            Assert.Equal("InstanceId Descending", sorting.ToString());
        }
    }
}
