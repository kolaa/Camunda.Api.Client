using System.Threading.Tasks;
using Camunda.Api.Client.ProcessDefinition;
using Xunit;

namespace Camunda.Api.Client.Tests
{
    public class ProcessDefinitionApiTests
    {
        [Fact]
        public async Task GetById_ReturnsDefinition()
        {
            var handler = new TestHttpMessageHandler("{\"id\":\"pd-1\",\"key\":\"pk-1\",\"name\":\"My process\"}");
            var client = handler.CreateClient();

            var definition = await client.ProcessDefinitions["pd-1"].Get();

            Assert.Equal(System.Net.Http.HttpMethod.Get, handler.Last.Method);
            Assert.Equal("/process-definition/pd-1", handler.Last.PathAndQuery);
            Assert.Equal("pd-1", definition.Id);
            Assert.Equal("pk-1", definition.Key);
            Assert.Equal("My process", definition.Name);
        }

        [Fact]
        public async Task ByKey_UsesKeyPath()
        {
            var handler = new TestHttpMessageHandler("{\"id\":\"pd-1\",\"key\":\"pk-1\"}");
            var client = handler.CreateClient();

            await client.ProcessDefinitions.ByKey("pk-1").Get();
            Assert.Equal("/process-definition/key/pk-1", handler.Last.PathAndQuery);

            await client.ProcessDefinitions.ByKey("pk-1", "t-1").Get();
            Assert.Equal("/process-definition/key/pk-1/tenant-id/t-1", handler.Last.PathAndQuery);
        }

        [Fact]
        public async Task Query_UsesGetWithQueryParameters()
        {
            var handler = new TestHttpMessageHandler("[]");
            var client = handler.CreateClient();

            var result = await client.ProcessDefinitions.Query(new ProcessDefinitionQuery { Key = "pk-1" }).List(2, 8);

            Assert.NotNull(result);
            Assert.Equal(System.Net.Http.HttpMethod.Get, handler.Last.Method);
            Assert.Matches(@"^/process-definition\?key=pk-1&latestVersion=false&suspended=false", handler.Last.PathAndQuery);
            Assert.EndsWith("&firstResult=2&maxResults=8", handler.Last.PathAndQuery);
        }

        [Fact]
        public async Task QueryCount_UsesCountEndpoint()
        {
            var handler = new TestHttpMessageHandler("{\"count\":1}");
            var client = handler.CreateClient();

            Assert.Equal(1, await client.ProcessDefinitions.Query().Count());
            Assert.StartsWith("/process-definition/count?", handler.Last.PathAndQuery);
        }

        [Fact]
        public async Task GetStatistics_ReturnsStatistics()
        {
            var handler = new TestHttpMessageHandler("[{\"id\":\"pd-1\",\"incidents\":[]}]");
            var client = handler.CreateClient();

            var statistics = await client.ProcessDefinitions.GetStatistics(true);

            Assert.NotNull(statistics);
            Assert.Equal(
                "/process-definition/statistics?failedJobs=true&incidents=false",
                handler.Last.PathAndQuery);
        }

        [Fact]
        public async Task Delete_PassesFlagsAsLowercaseQuery()
        {
            var handler = new TestHttpMessageHandler("null");
            var client = handler.CreateClient();

            await client.ProcessDefinitions["pd-1"].Delete(cascade: true, skipCustomListeners: false, skipIoMappings: true);

            Assert.Equal(System.Net.Http.HttpMethod.Delete, handler.Last.Method);
            Assert.Equal(
                "/process-definition/pd-1?cascade=true&skipCustomListeners=false&skipIoMappings=true",
                handler.Last.PathAndQuery);
        }

        [Fact]
        public async Task GetXml_ReturnsBpmnXml()
        {
            var handler = new TestHttpMessageHandler("{\"id\":\"pd-1\",\"bpmn20Xml\":\"<xml/>\"}");
            var client = handler.CreateClient();

            var diagram = await client.ProcessDefinitions["pd-1"].GetXml();

            Assert.Equal("/process-definition/pd-1/xml", handler.Last.PathAndQuery);
            Assert.Equal("<xml/>", diagram.Bpmn20Xml);
        }

        [Fact]
        public async Task GetDiagram_ReturnsRawContent()
        {
            var handler = new TestHttpMessageHandler("png-bytes");
            var client = handler.CreateClient();

            var content = await client.ProcessDefinitions["pd-1"].GetDiagram();
            var raw = await content.ReadAsStringAsync();

            Assert.Equal("/process-definition/pd-1/diagram", handler.Last.PathAndQuery);
            Assert.Equal("png-bytes", raw);
        }

        [Fact]
        public async Task StartProcessInstance_SendsVariablesAndBusinessKey()
        {
            var handler = new TestHttpMessageHandler("{\"id\":\"pi-1\"}");
            var client = handler.CreateClient();

            var parameters = new StartProcessInstance().SetVariable("a", "x");
            parameters.BusinessKey = "bk-1";

            var instance = await client.ProcessDefinitions["pd-1"].StartProcessInstance(parameters);

            Assert.Equal(System.Net.Http.HttpMethod.Post, handler.Last.Method);
            Assert.Equal("/process-definition/pd-1/start", handler.Last.PathAndQuery);
            Assert.Contains("\"businessKey\":\"bk-1\"", handler.Last.Body);
            Assert.Contains("\"a\":{\"type\":\"String\",\"value\":\"x\"}", handler.Last.Body);
            Assert.Equal("pi-1", instance.Id);
        }

        [Fact]
        public async Task SubmitForm_SendsStartFormBody()
        {
            var handler = new TestHttpMessageHandler("{\"id\":\"pi-2\"}");
            var client = handler.CreateClient();

            var instance = await client.ProcessDefinitions["pd-1"].SubmitForm(
                new SubmitStartForm { BusinessKey = "bk-2" });

            Assert.Equal("/process-definition/pd-1/submit-form", handler.Last.PathAndQuery);
            Assert.Contains("\"businessKey\":\"bk-2\"", handler.Last.Body);
            Assert.Equal("pi-2", instance.Id);
        }

        [Fact]
        public async Task UpdateSuspensionState_SendsState()
        {
            var handler = new TestHttpMessageHandler("null");
            var client = handler.CreateClient();

            await client.ProcessDefinitions["pd-1"].UpdateSuspensionState(
                new ProcessDefinitionSuspensionState { Suspended = true });

            Assert.Equal(System.Net.Http.HttpMethod.Put, handler.Last.Method);
            Assert.Equal("/process-definition/pd-1/suspended", handler.Last.PathAndQuery);
            Assert.Contains("\"includeProcessInstances\":false", handler.Last.Body);
            Assert.Contains("\"suspended\":true", handler.Last.Body);
        }

        [Fact]
        public async Task GetFormVariables_JoinsVariableNames()
        {
            var handler = new TestHttpMessageHandler("{}");
            var client = handler.CreateClient();

            await client.ProcessDefinitions["pd-1"].GetFormVariables("a", "b");

            Assert.Matches(
                @"^/process-definition/pd-1/form-variables\?variableNames=a(%2C|,)b&deserializeValues=true$",
                handler.Last.PathAndQuery);
        }

        [Fact]
        public async Task GetStartForm_ReturnsFormInfo()
        {
            var handler = new TestHttpMessageHandler("{\"key\":\"start-form\",\"contextPath\":\"/app\"}");
            var client = handler.CreateClient();

            var form = await client.ProcessDefinitions["pd-1"].GetStartForm();

            Assert.Equal("/process-definition/pd-1/startForm", handler.Last.PathAndQuery);
            Assert.Equal("start-form", form.Key);
        }

        [Fact]
        public async Task GetRenderedForm_ReturnsHtml()
        {
            var handler = new TestHttpMessageHandler("<html>start</html>");
            var client = handler.CreateClient();

            var html = await client.ProcessDefinitions["pd-1"].GetRenderedForm();

            Assert.Equal("/process-definition/pd-1/rendered-form", handler.Last.PathAndQuery);
            Assert.Equal("<html>start</html>", html);
        }

        [Fact]
        public async Task GetActivityStatistics_ReturnsStatistics()
        {
            var handler = new TestHttpMessageHandler("[{\"activityId\":\"task1\",\"instanceCount\":2}]");
            var client = handler.CreateClient();

            var statistics = await client.ProcessDefinitions["pd-1"].GetActivityStatistics(true, false);

            Assert.NotNull(statistics);
            Assert.Equal(
                "/process-definition/pd-1/statistics?failedJobs=true&incidents=false",
                handler.Last.PathAndQuery);
        }

        [Fact]
        public async Task ProcessDefinitionResource_ToString_ReturnsId()
        {
            var client = CamundaClient.Create("http://localhost:8080/engine-rest");

            Assert.Equal("pd-1", client.ProcessDefinitions["pd-1"].ToString());
            Assert.Equal("pk-1", client.ProcessDefinitions.ByKey("pk-1").ToString());
        }
    }
}

