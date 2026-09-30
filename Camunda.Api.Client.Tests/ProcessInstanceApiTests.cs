using System.Collections.Generic;
using System.Threading.Tasks;
using Camunda.Api.Client.ProcessInstance;
using Xunit;

namespace Camunda.Api.Client.Tests
{
    public class ProcessInstanceApiTests
    {
        [Fact]
        public async Task QueryList_PaginatesAndSerializesQuery()
        {
            var handler = new TestHttpMessageHandler("[]");
            var client = handler.CreateClient();

            var query = new ProcessInstanceQuery { BusinessKey = "bk-1" };
            var result = await client.ProcessInstances.Query(query).List(0, 10);

            Assert.NotNull(result);
            Assert.Empty(result);

            Assert.Equal(System.Net.Http.HttpMethod.Post, handler.Last.Method);
            Assert.Equal("/process-instance?firstResult=0&maxResults=10", handler.Last.PathAndQuery);
            Assert.Equal("application/json; charset=utf-8", handler.Last.ContentType);
            Assert.Contains("\"businessKey\":\"bk-1\"", handler.Last.Body);
        }

        [Fact]
        public async Task QueryList_WithoutPaging_OmitsQueryParameters()
        {
            var handler = new TestHttpMessageHandler("[]");
            var client = handler.CreateClient();

            await client.ProcessInstances.Query().List();

            Assert.Equal("/process-instance", handler.Last.PathAndQuery);
            Assert.Contains("\"tenantIdIn\":[]", handler.Last.Body);
        }

        [Fact]
        public async Task QueryList_WithSorting_SerializesSortingArray()
        {
            var handler = new TestHttpMessageHandler("[]");
            var client = handler.CreateClient();

            var query = new ProcessInstanceQuery()
                .Sort(ProcessInstanceSorting.InstanceId, SortOrder.Descending);

            await client.ProcessInstances.Query(query).List();

            Assert.Contains("\"sortBy\":\"instanceId\"", handler.Last.Body);
            Assert.Contains("\"sortOrder\":\"desc\"", handler.Last.Body);
        }

        [Fact]
        public async Task QueryCount_UsesCountEndpoint()
        {
            var handler = new TestHttpMessageHandler("{\"count\":7}");
            var client = handler.CreateClient();

            var count = await client.ProcessInstances.Query(new ProcessInstanceQuery { Suspended = true }).Count();

            Assert.Equal(7, count);
            Assert.Equal(System.Net.Http.HttpMethod.Post, handler.Last.Method);
            Assert.Equal("/process-instance/count", handler.Last.PathAndQuery);
            Assert.Contains("\"suspended\":true", handler.Last.Body);
        }

        [Fact]
        public async Task Get_ReturnsProcessInstance()
        {
            var handler = new TestHttpMessageHandler(
                "{\"id\":\"pi-1\",\"definitionId\":\"pd-1\",\"businessKey\":\"bk-1\",\"suspended\":true}");
            var client = handler.CreateClient();

            var instance = await client.ProcessInstances["pi-1"].Get();

            Assert.Equal(System.Net.Http.HttpMethod.Get, handler.Last.Method);
            Assert.Equal("/process-instance/pi-1", handler.Last.PathAndQuery);
            Assert.Equal("pi-1", instance.Id);
            Assert.Equal("pd-1", instance.DefinitionId);
            Assert.True(instance.Suspended);
        }

        [Fact]
        public async Task Delete_PassesSkipFlagsAsLowercaseQuery()
        {
            var handler = new TestHttpMessageHandler("null");
            var client = handler.CreateClient();

            await client.ProcessInstances["pi-1"].Delete(skipCustomListeners: true, skipIoMappings: true);

            Assert.Equal(System.Net.Http.HttpMethod.Delete, handler.Last.Method);
            Assert.Equal(
                "/process-instance/pi-1?skipCustomListeners=true&skipIoMappings=true&skipSubprocesses=false",
                handler.Last.PathAndQuery);
        }

        [Fact]
        public async Task GetActivityInstance_ReturnsTree()
        {
            var handler = new TestHttpMessageHandler("{\"id\":\"act-1\",\"parentInstanceId\":null,\"activityId\":\"task1\"}");
            var client = handler.CreateClient();

            var tree = await client.ProcessInstances["pi-1"].GetActivityInstance();

            Assert.Equal("/process-instance/pi-1/activity-instances", handler.Last.PathAndQuery);
            Assert.Equal("act-1", tree.Id);
            Assert.Equal("task1", tree.ActivityId);
        }

        [Fact]
        public async Task GetVariable_DeserializesByDefault()
        {
            var handler = new TestHttpMessageHandler("{\"type\":\"String\",\"value\":\"abc\"}");
            var client = handler.CreateClient();

            var variable = await client.ProcessInstances["pi-1"].Variables.Get("foo");

            Assert.Equal("/process-instance/pi-1/variables/foo?deserializeValue=true", handler.Last.PathAndQuery);
            Assert.Equal(VariableType.String, variable.Type);
            Assert.Equal("abc", variable.Value);
        }

        [Fact]
        public async Task GetVariable_CanSkipDeserialization()
        {
            var handler = new TestHttpMessageHandler("{\"type\":\"Object\",\"value\":\"{\\\"a\\\":1}\"}");
            var client = handler.CreateClient();

            await client.ProcessInstances["pi-1"].Variables.Get("foo", deserializeValue: false);

            Assert.Equal("/process-instance/pi-1/variables/foo?deserializeValue=false", handler.Last.PathAndQuery);
        }

        [Fact]
        public async Task GetAllVariables_ReturnsDictionary()
        {
            var handler = new TestHttpMessageHandler(
                "{\"a\":{\"type\":\"String\",\"value\":\"x\"},\"b\":{\"type\":\"Boolean\",\"value\":true}}");
            var client = handler.CreateClient();

            var variables = await client.ProcessInstances["pi-1"].Variables.GetAll();

            Assert.Equal("/process-instance/pi-1/variables?deserializeValues=true", handler.Last.PathAndQuery);
            Assert.Equal(2, variables.Count);
            Assert.Equal("x", variables["a"].Value);
            Assert.Equal(true, variables["b"].Value);
        }

        [Fact]
        public async Task SetVariable_SendsTypedJson()
        {
            var handler = new TestHttpMessageHandler("null");
            var client = handler.CreateClient();

            await client.ProcessInstances["pi-1"].Variables.Set("foo", VariableValue.FromObject("bar"));

            Assert.Equal(System.Net.Http.HttpMethod.Put, handler.Last.Method);
            Assert.Equal("/process-instance/pi-1/variables/foo", handler.Last.PathAndQuery);
            Assert.Equal("{\"type\":\"String\",\"value\":\"bar\"}", handler.Last.Body);
        }

        [Fact]
        public async Task ModifyVariables_SendsModificationsAndDeletions()
        {
            var handler = new TestHttpMessageHandler("null");
            var client = handler.CreateClient();

            await client.ProcessInstances["pi-1"].Variables.Modify(new PatchVariables
            {
                Modifications = new Dictionary<string, VariableValue> { ["a"] = VariableValue.FromObject(1) },
                Deletions = new List<string> { "b" }
            });

            Assert.Equal(System.Net.Http.HttpMethod.Post, handler.Last.Method);
            Assert.Equal("/process-instance/pi-1/variables", handler.Last.PathAndQuery);
            Assert.Equal(
                "{\"modifications\":{\"a\":{\"type\":\"Integer\",\"value\":1}},\"deletions\":[\"b\"]}",
                handler.Last.Body);
        }

        [Fact]
        public async Task SetBinaryVariable_SendsMultipartBody()
        {
            var handler = new TestHttpMessageHandler("null");
            var client = handler.CreateClient();

            var content = new System.IO.MemoryStream(new byte[] { 1, 2, 3 });
            await client.ProcessInstances["pi-1"].Variables.SetBinary(
                "blob",
                new BinaryDataContent(content, "f.bin"),
                BinaryVariableType.Bytes);

            Assert.Equal(System.Net.Http.HttpMethod.Post, handler.Last.Method);
            Assert.Equal("/process-instance/pi-1/variables/blob/data", handler.Last.PathAndQuery);
            Assert.StartsWith("multipart/form-data", handler.Last.ContentType);
            Assert.Contains("name=data", handler.Last.Body);
            Assert.Contains("filename=f.bin", handler.Last.Body);
            Assert.Contains("name=valueType", handler.Last.Body);
            Assert.Contains("Bytes", handler.Last.Body);
        }

        [Fact]
        public async Task GetBinaryVariable_ReturnsRawContent()
        {
            var handler = new TestHttpMessageHandler("{\"type\":\"text/plain\",\"value\":\"AQID\"}");
            var client = handler.CreateClient();

            var content = await client.ProcessInstances["pi-1"].Variables.GetBinary("blob");
            var raw = await content.ReadAsStringAsync();

            Assert.Equal(System.Net.Http.HttpMethod.Get, handler.Last.Method);
            Assert.Equal("/process-instance/pi-1/variables/blob/data", handler.Last.PathAndQuery);
            Assert.Equal("{\"type\":\"text/plain\",\"value\":\"AQID\"}", raw);
        }

        [Fact]
        public async Task UpdateSuspensionState_ForSingleInstance()
        {
            var handler = new TestHttpMessageHandler("null");
            var client = handler.CreateClient();

            await client.ProcessInstances["pi-1"].UpdateSuspensionState(true);

            Assert.Equal(System.Net.Http.HttpMethod.Put, handler.Last.Method);
            Assert.Equal("/process-instance/pi-1/suspended", handler.Last.PathAndQuery);
            Assert.Equal("{\"suspended\":true}", handler.Last.Body);
        }

        [Fact]
        public async Task UpdateSuspensionState_ForAllInstances()
        {
            var handler = new TestHttpMessageHandler("null");
            var client = handler.CreateClient();

            await client.ProcessInstances.UpdateSuspensionState(new ProcessInstanceSuspensionState
            {
                Suspended = true,
                ProcessDefinitionKey = "pk-1"
            });

            Assert.Equal(System.Net.Http.HttpMethod.Put, handler.Last.Method);
            Assert.Equal("/process-instance/suspended", handler.Last.PathAndQuery);
            Assert.Equal(
                "{\"processDefinitionKey\":\"pk-1\",\"processDefinitionWithoutTenantId\":false,\"suspended\":true}",
                handler.Last.Body);
        }
    }
}
