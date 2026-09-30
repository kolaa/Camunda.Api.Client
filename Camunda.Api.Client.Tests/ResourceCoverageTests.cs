using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Camunda.Api.Client.Deployment;
using Camunda.Api.Client.ExternalTask;
using Camunda.Api.Client.History;
using Camunda.Api.Client.Identity;
using Camunda.Api.Client.ProcessDefinition;
using Camunda.Api.Client.ProcessInstance;
using Camunda.Api.Client.UserTask;
using Newtonsoft.Json;
using Xunit;

namespace Camunda.Api.Client.Tests
{
    public class ResourceCoverageTests
    {
        private static string Path(RecordedRequest request) => request.PathAndQuery.Split('?')[0];

        [Fact]
        public async Task TaskAttachmentResource_CanListReadDownloadDeleteAndCreate()
        {
            var handler = new TestHttpMessageHandler("[]");
            var client = handler.CreateClient();

            Assert.NotNull(await client.UserTasks["t-1"].Attachment.GetAll());
            Assert.Equal(System.Net.Http.HttpMethod.Get, handler.Last.Method);
            Assert.Equal("/task/t-1/attachment", handler.Last.PathAndQuery);

            handler.ResponseJson = "{\"id\":\"a-1\",\"name\":\"n\",\"taskId\":\"t-1\"}";
            var attachment = await client.UserTasks["t-1"].Attachment.Get("a-1");
            Assert.Equal("/task/t-1/attachment/a-1", handler.Last.PathAndQuery);
            Assert.Equal("a-1", attachment.Id);

            handler.ResponseJson = "file-bytes";
            var data = await client.UserTasks["t-1"].Attachment.GetData("a-1");
            Assert.Equal("/task/t-1/attachment/a-1/data", handler.Last.PathAndQuery);
            Assert.Equal("file-bytes", await data.ReadAsStringAsync());

            handler.ResponseJson = "null";
            await client.UserTasks["t-1"].Attachment.Delete("a-1");
            Assert.Equal(System.Net.Http.HttpMethod.Delete, handler.Last.Method);
            Assert.Equal("/task/t-1/attachment/a-1", handler.Last.PathAndQuery);

            using var stream = new MemoryStream(Encoding.UTF8.GetBytes("content-bytes"));
            handler.ResponseJson = "{\"id\":\"a-2\",\"name\":\"report.pdf\"}";
            var created = await client.UserTasks["t-1"].Attachment.Create(
                "report.pdf", "desc", "application/pdf", "http://x/y", new AttachmentContent(stream));

            Assert.Equal(System.Net.Http.HttpMethod.Post, handler.Last.Method);
            Assert.Equal("/task/t-1/attachment/create", handler.Last.PathAndQuery);
            Assert.StartsWith("multipart/form-data", handler.Last.ContentType);
            Assert.Contains("name=attachment-name", handler.Last.Body);
            Assert.Contains("name=attachment-description", handler.Last.Body);
            Assert.Contains("name=attachment-type", handler.Last.Body);
            Assert.Contains("name=url", handler.Last.Body);
            Assert.Contains("name=content", handler.Last.Body);
            Assert.Contains("Content-Transfer-Encoding: binary", handler.Last.Body);
            Assert.Equal("a-2", created.Id);
        }

        [Fact]
        public async Task DeploymentResource_CanReadDeleteRedeployAndReadResources()
        {
            var handler = new TestHttpMessageHandler("{\"id\":\"d-1\",\"name\":\"app\"}");
            var client = handler.CreateClient();

            var deployment = await client.Deployments["d-1"].Get();
            Assert.Equal(System.Net.Http.HttpMethod.Get, handler.Last.Method);
            Assert.Equal("/deployment/d-1", Path(handler.Last));
            Assert.Equal("d-1", deployment.Id);

            handler.ResponseJson = "{\"id\":\"d-2\"}";
            var redeployed = await client.Deployments["d-1"].Redeploy(new RedeploymentInfo());
            Assert.Equal(System.Net.Http.HttpMethod.Post, handler.Last.Method);
            Assert.Equal("/deployment/d-1/redeploy", handler.Last.PathAndQuery);
            Assert.Equal("d-2", redeployed.Id);

            handler.ResponseJson = "null";
            await client.Deployments["d-1"].Delete();
            Assert.Equal(System.Net.Http.HttpMethod.Delete, handler.Last.Method);
            Assert.Equal("/deployment/d-1", Path(handler.Last));

            handler.ResponseJson = "[{\"id\":\"r-1\",\"name\":\"process.bpmn\",\"deploymentId\":\"d-1\"}]";
            var resources = await client.Deployments["d-1"].Resources.GetAll();
            Assert.Equal("/deployment/d-1/resources", handler.Last.PathAndQuery);
            Assert.Single(resources);

            handler.ResponseJson = "{\"id\":\"r-1\",\"name\":\"process.bpmn\",\"deploymentId\":\"d-1\"}";
            var resource = await client.Deployments["d-1"].Resources.Get("r-1");
            Assert.Equal("/deployment/d-1/resources/r-1", handler.Last.PathAndQuery);
            Assert.Equal("process.bpmn", resource.Name);

            handler.ResponseJson = "bpmn-xml";
            var content = await client.Deployments["d-1"].Resources.GetData("r-1");
            Assert.Equal("/deployment/d-1/resources/r-1/data", handler.Last.PathAndQuery);
            Assert.Equal("bpmn-xml", await content.ReadAsStringAsync());
        }

        [Fact]
        public async Task ProcessDefinitionByKey_CanReadStartFormsAndDelete()
        {
            var handler = new TestHttpMessageHandler("{\"id\":\"pd-1\",\"key\":\"pk\"}");
            var client = handler.CreateClient();

            var definition = await client.ProcessDefinitions.ByKey("pk").Get();
            Assert.Equal(System.Net.Http.HttpMethod.Get, handler.Last.Method);
            Assert.Equal("/process-definition/key/pk", handler.Last.PathAndQuery);
            Assert.Equal("pk", definition.Key);

            handler.ResponseJson = "{}";
            await client.ProcessDefinitions.ByKey("pk").GetXml();
            Assert.Equal("/process-definition/key/pk/xml", handler.Last.PathAndQuery);

            handler.ResponseJson = "png-bytes";
            var diagram = await client.ProcessDefinitions.ByKey("pk").GetDiagram();
            Assert.Equal("/process-definition/key/pk/diagram", handler.Last.PathAndQuery);
            Assert.Equal("png-bytes", await diagram.ReadAsStringAsync());

            handler.ResponseJson = "{\"id\":\"pi-1\"}";
            var instance = await client.ProcessDefinitions.ByKey("pk").StartProcessInstance(
                new StartProcessInstance { BusinessKey = "bk" });
            Assert.Equal(System.Net.Http.HttpMethod.Post, handler.Last.Method);
            Assert.Equal("/process-definition/key/pk/start", handler.Last.PathAndQuery);
            Assert.Contains("\"businessKey\":\"bk\"", handler.Last.Body);

            handler.ResponseJson = "{\"id\":\"pi-2\"}";
            await client.ProcessDefinitions.ByKey("pk").SubmitForm(new SubmitStartForm());
            Assert.Equal("/process-definition/key/pk/submit-form", handler.Last.PathAndQuery);

            handler.ResponseJson = "{\"key\":\"startForm\"}";
            var startForm = await client.ProcessDefinitions.ByKey("pk").GetStartForm();
            Assert.Equal("/process-definition/key/pk/startForm", handler.Last.PathAndQuery);
            Assert.Equal("startForm", startForm.Key);

            handler.ResponseJson = "<form>html</form>";
            var rendered = await client.ProcessDefinitions.ByKey("pk").GetRenderedForm();
            Assert.Equal("/process-definition/key/pk/rendered-form", handler.Last.PathAndQuery);
            Assert.Equal("<form>html</form>", rendered);

            handler.ResponseJson = "{\"a\":{\"type\":\"String\",\"value\":\"x\"}}";
            var formVariables = await client.ProcessDefinitions.ByKey("pk").GetFormVariables("a");
            Assert.Equal("/process-definition/key/pk/form-variables", Path(handler.Last));
            Assert.Equal("x", formVariables["a"].GetValue<string>());

            handler.ResponseJson = "{\"a\":{\"type\":\"String\",\"value\":\"y\"}}";
            await client.ProcessDefinitions.ByKey("pk").GetFormVariables(new[] { "a" }, false);
            Assert.Contains("deserializeValues=false", handler.Last.PathAndQuery);

            handler.ResponseJson = "null";
            await client.ProcessDefinitions.ByKey("pk").UpdateSuspensionState(
                new ProcessDefinitionSuspensionState { Suspended = true });
            Assert.Equal(System.Net.Http.HttpMethod.Put, handler.Last.Method);
            Assert.Equal("/process-definition/key/pk/suspended", handler.Last.PathAndQuery);

            await client.ProcessDefinitions.ByKey("pk").Delete(true, false, true);
            Assert.Equal(System.Net.Http.HttpMethod.Delete, handler.Last.Method);
            Assert.Equal("/process-definition/key/pk/delete", Path(handler.Last));
        }

        [Fact]
        public async Task ExternalTaskTopics_AreSerializedInFetchAndLock()
        {
            var handler = new TestHttpMessageHandler("[]");
            var client = handler.CreateClient();

            var topic = new FetchExternalTaskTopic("invoice", 10000)
            {
                BusinessKey = "bk",
                DeserializeValues = true,
                LocalVariables = true,
                ProcessDefinitionId = "pd-1",
                ProcessDefinitionKey = "pk",
                ProcessDefinitionKeys = new List<string> { "k1", "k2" },
                TenantIds = new List<string> { "t-1" },
                Variables = new List<string> { "v1" },
                WithoutTenantId = true
            };
            topic.ProcessDefinitionIds.Add("pd-2");
            topic.ProcessVariables["amount"] = 12;

            var locked = await client.ExternalTasks.FetchAndLock(new ExternalTask.FetchExternalTasks
            {
                WorkerId = "w-1",
                MaxTasks = 1,
                Topics = new List<FetchExternalTaskTopic> { topic }
            });

            Assert.NotNull(locked);
            Assert.Equal("/external-task/fetchAndLock", handler.Last.PathAndQuery);
            var body = handler.Last.Body;
            Assert.Contains("\"topicName\":\"invoice\"", body);
            Assert.Contains("\"lockDuration\":10000", body);
            Assert.Contains("\"businessKey\":\"bk\"", body);
            Assert.Contains("\"deserializeValues\":true", body);
            Assert.Contains("\"localVariables\":true", body);
            Assert.Contains("\"processDefinitionId\":\"pd-1\"", body);
            Assert.Contains("\"processDefinitionIdIn\":[\"pd-2\"]", body);
            Assert.Contains("\"processDefinitionKey\":\"pk\"", body);
            Assert.Contains("\"processDefinitionKeyIn\":[\"k1\",\"k2\"]", body);
            Assert.Contains("\"tenantIdIn\":[\"t-1\"]", body);
            Assert.Contains("\"variables\":[\"v1\"]", body);
            Assert.Contains("\"withoutTenantId\":true", body);
        }

        [Fact]
        public async Task ProcessInstance_ModificationSerializesTriggerVariableValues()
        {
            var handler = new TestHttpMessageHandler("null");
            var client = handler.CreateClient();

            var modification = new ProcessInstanceModification();
            modification.Instructions.Add(new ProcessInstanceModificationInstruction
            {
                Type = InstructionType.StartBeforeActivity,
                ActivityId = "Activity_1"
            });
            modification.Instructions[0].Variables["localFlag"] =
                TriggerVariableValue.FromObject(true, true);
            modification.Instructions[0].Variables["amount"] =
                TriggerVariableValue.FromObject(42, false);

            await client.ProcessInstances["pi-1"].ModifyProcessInstance(modification);

            Assert.Equal(System.Net.Http.HttpMethod.Post, handler.Last.Method);
            Assert.Equal("/process-instance/pi-1/modification", handler.Last.PathAndQuery);
            var body = handler.Last.Body;
            Assert.Contains("\"type\":\"startBeforeActivity\"", body);
            Assert.Contains("\"activityId\":\"Activity_1\"", body);
            Assert.Contains("\"localFlag\":{\"local\":true,\"type\":\"Boolean\",\"value\":true}", body);
            Assert.Contains("\"amount\":{\"local\":false,\"type\":\"Integer\",\"value\":42}", body);
        }

        [Fact]
        public async Task TaskLocalVariableResource_CoversFullLifecycle()
        {
            var handler = new TestHttpMessageHandler("{}");
            var client = handler.CreateClient();

            Assert.NotNull(await client.UserTasks["t-1"].LocalVariables.GetAll());
            Assert.Equal("/task/t-1/localVariables", Path(handler.Last));

            handler.ResponseJson = "{\"type\":\"String\",\"value\":\"x\"}";
            var value = await client.UserTasks["t-1"].LocalVariables.Get("a", false);
            Assert.Equal("/task/t-1/localVariables/a", Path(handler.Last));
            Assert.Contains("deserializeValue=false", handler.Last.PathAndQuery);
            Assert.Equal("x", value.GetValue<string>());

            handler.ResponseJson = "bytes";
            var binary = await client.UserTasks["t-1"].LocalVariables.GetBinary("a");
            Assert.Equal("/task/t-1/localVariables/a/data", handler.Last.PathAndQuery);
            Assert.Equal("bytes", await binary.ReadAsStringAsync());

            handler.ResponseJson = "null";
            await client.UserTasks["t-1"].LocalVariables.Set("a",
                VariableValue.FromObject("next"));
            Assert.Equal(System.Net.Http.HttpMethod.Put, handler.Last.Method);
            Assert.Equal("/task/t-1/localVariables/a", handler.Last.PathAndQuery);

            using var stream = new MemoryStream(Encoding.UTF8.GetBytes("bin"));
            await client.UserTasks["t-1"].LocalVariables.SetBinary(
                "a", new BinaryDataContent(stream, "file.bin"), BinaryVariableType.File);
            Assert.Equal(System.Net.Http.HttpMethod.Post, handler.Last.Method);
            Assert.Equal("/task/t-1/localVariables/a/data", handler.Last.PathAndQuery);
            Assert.StartsWith("multipart/form-data", handler.Last.ContentType);
            Assert.Contains("filename=file.bin", handler.Last.Body);

            await client.UserTasks["t-1"].LocalVariables.Delete("a");
            Assert.Equal(System.Net.Http.HttpMethod.Delete, handler.Last.Method);
            Assert.Equal("/task/t-1/localVariables/a", handler.Last.PathAndQuery);

            await client.UserTasks["t-1"].LocalVariables.Modify(new PatchVariables
            {
                Modifications = new Dictionary<string, VariableValue>
                {
                    ["b"] = VariableValue.FromObject(1)
                },
                Deletions = new List<string> { "c" }
            });
            Assert.Equal(System.Net.Http.HttpMethod.Post, handler.Last.Method);
            Assert.Equal("/task/t-1/localVariables", handler.Last.PathAndQuery);
            Assert.Contains("\"b\":", handler.Last.Body);
            Assert.Contains("\"c\"", handler.Last.Body);
        }

        [Fact]
        public async Task HistoryResources_CanBeReadById()
        {
            var handler = new TestHttpMessageHandler("{}");
            var client = handler.CreateClient();

            await client.History.ActivityInstances["h-1"].Get();
            Assert.Equal("/history/activity-instance/h-1", handler.Last.PathAndQuery);

            await client.History.CaseInstances["h-1"].Get();
            Assert.Equal("/history/case-instance/h-1", handler.Last.PathAndQuery);

            await client.History.DecisionInstances["h-1"].Get();
            Assert.Equal("/history/decision-instance/h-1", handler.Last.PathAndQuery);

            await client.History.JobLogs["h-1"].Get();
            Assert.Equal("/history/job-log/h-1", handler.Last.PathAndQuery);

            handler.ResponseJson = "stacktrace-text";
            var stacktrace = await client.History.JobLogs["h-1"].GetStacktrace();
            Assert.Equal("/history/job-log/h-1/stacktrace", handler.Last.PathAndQuery);
            Assert.Equal("stacktrace-text", stacktrace);

            handler.ResponseJson = "{}";
            await client.History.ExternalTaskLogs["h-1"].Get();
            Assert.Equal("/history/external-task-log/h-1", handler.Last.PathAndQuery);

            handler.ResponseJson = "error text";
            var errorDetails = await client.History.ExternalTaskLogs["h-1"].GetErrorDetails();
            Assert.Equal("/history/external-task-log/h-1/error-details", handler.Last.PathAndQuery);
            Assert.Equal("error text", errorDetails);

            handler.ResponseJson = "{}";
            await client.History.Detail["h-1"].Get();
            Assert.Equal("/history/detail/h-1", handler.Last.PathAndQuery);

            handler.ResponseJson = "detail-bytes";
            var detailData = await client.History.Detail["h-1"].GetData();
            Assert.Equal("/history/detail/h-1/data", handler.Last.PathAndQuery);
            Assert.Equal("detail-bytes", await detailData.ReadAsStringAsync());

            handler.ResponseJson = "{\"id\":\"hv-1\"}";
            var historicVariable = await client.History.VariableInstances["hv-1"].Get();
            Assert.Equal("/history/variable-instance/hv-1", Path(handler.Last));
            Assert.Contains("deserializeValue=true", handler.Last.PathAndQuery);
            Assert.Equal("hv-1", historicVariable.Id);

            handler.ResponseJson = "var-bytes";
            var historicBinary = await client.History.VariableInstances["hv-1"].GetBinary();
            Assert.Equal("/history/variable-instance/hv-1/data", handler.Last.PathAndQuery);
            Assert.Equal("var-bytes", await historicBinary.ReadAsStringAsync());

            handler.ResponseJson = "{\"id\":\"hpi-1\"}";
            var historicInstance = await client.History.ProcessInstances["hpi-1"].Get();
            Assert.Equal("/history/process-instance/hpi-1", handler.Last.PathAndQuery);
            Assert.Equal("hpi-1", historicInstance.Id);

            handler.ResponseJson = "null";
            await client.History.ProcessInstances["hpi-1"].Delete();
            Assert.Equal(System.Net.Http.HttpMethod.Delete, handler.Last.Method);
            Assert.Equal("/history/process-instance/hpi-1", handler.Last.PathAndQuery);
        }

        [Fact]
        public async Task HistoryCaseActivityInstances_CanBeQueriedAndRead()
        {
            var handler = new TestHttpMessageHandler("[]");
            var client = handler.CreateClient();

            Assert.NotNull(await client.History.CaseActivityInstances.Query(new HistoricCaseActivityInstanceQuery()).List(0, 10));
            Assert.Equal("/history/case-activity-instance", Path(handler.Last));

            handler.ResponseJson = "{\"id\":\"cai-1\"}";
            var resource = await client.History.CaseActivityInstances["cai-1"].Get();
            Assert.Equal(System.Net.Http.HttpMethod.Get, handler.Last.Method);
            Assert.Equal("/history/case-activity-instance/cai-1", handler.Last.PathAndQuery);
            Assert.Equal("cai-1", resource.Id);
        }

        [Fact]
        public async Task HistoryCaseAndProcessDefinitionReports_CanBeRead()
        {
            var handler = new TestHttpMessageHandler("[]");
            var client = handler.CreateClient();

            Assert.NotNull(await client.History.CaseDefinitions["cd-1"].GetActivityStatistics());
            Assert.Equal("/history/case-definition/cd-1/statistics", handler.Last.PathAndQuery);

            Assert.NotNull(await client.History.ProcessDefinitions.GetHistoricActivityStatistics(
                "pd-1", new HistoricActivityStatistics()));
            Assert.Equal("/history/process-definition/pd-1/statistics", Path(handler.Last));

            Assert.NotNull(await client.History.ProcessDefinitions.GetCleanableProcessInstanceReport(
                new CleanableProcessInstanceReport()));
            Assert.Equal("/history/process-definition/cleanable-process-instance-report", Path(handler.Last));

            handler.ResponseJson = "{\"count\":4}";
            Assert.Equal(4, await client.History.ProcessDefinitions.GetCleanableProcessInstanceReportCount(
                new CleanableProcessInstanceReportCount()));
            Assert.Equal("/history/process-definition/cleanable-process-instance-report/count", Path(handler.Last));
        }

        [Fact]
        public async Task HistoryProcessInstanceReportsAndBatchDelete_CanBeRequested()
        {
            var handler = new TestHttpMessageHandler("[]");
            var client = handler.CreateClient();

            Assert.NotNull(await client.History.ProcessInstances.GetDurationReport(
                new HistoricProcessInstanceReport()));
            Assert.Equal("/history/process-instance/report", Path(handler.Last));

            handler.ResponseJson = "{\"id\":\"b-1\"}";
            var batch = await client.History.ProcessInstances.Delete(new DeleteHistoricProcessInstances
            {
                HistoricProcessInstanceIds = new List<string> { "hpi-1" },
                DeleteReason = "cleanup"
            });
            Assert.Equal(System.Net.Http.HttpMethod.Post, handler.Last.Method);
            Assert.Equal("/history/process-instance/delete", handler.Last.PathAndQuery);
            Assert.Contains("hpi-1", handler.Last.Body);
            Assert.Equal("b-1", batch.Id);
        }

        [Fact]
        public async Task IdentityGroupMembership_DeserializesLists()
        {
            const string json = @"{
                ""groups"": [{""id"":""g-1"",""name"":""developers""}],
                ""groupUsers"": [{""id"":""u-1"",""firstName"":""Kermit"",""lastName"":""Frog"",""displayName"":""Kermit Frog""}]
            }";

            var membership = JsonConvert.DeserializeObject<IdentityGroupMembership>(json);

            Assert.Single(membership.Groups);
            Assert.Equal("g-1", membership.Groups[0].Id);
            Assert.Equal("developers", membership.Groups[0].Name);
            Assert.Single(membership.GroupUsers);
            Assert.Equal("Kermit Frog", membership.GroupUsers[0].DisplayName);
            Assert.Equal("g-1", membership.Groups[0].ToString());
            Assert.Equal("u-1", membership.GroupUsers[0].ToString());
        }

        [Fact]
        public void VariableValue_TypeConverterConvertsBothWays()
        {
            var converter = System.ComponentModel.TypeDescriptor.GetConverter(typeof(VariableValue));

            Assert.True(converter.CanConvertTo(typeof(int)));
            Assert.True(converter.CanConvertFrom(typeof(string)));

            var value = VariableValue.FromObject(42);
            Assert.Equal(42, converter.ConvertTo(value, typeof(int)));
            Assert.Equal("42", converter.ConvertTo(value, typeof(string)));

            var roundTrip = (VariableValue)converter.ConvertFrom("text");
            Assert.Equal("text", roundTrip.GetValue<string>());
        }

        [Fact]
        public void NamedVariableValue_AndVariableOrder_RoundTrip()
        {
            var named = NamedVariableValue.FromObject("amount", 10);
            Assert.Equal("amount", named.Name);
            Assert.Equal(10, named.GetValue<int>());
            Assert.Equal("amount = 10", named.ToString());

            var order = new VariableOrder("amount", VariableType.Long);
            Assert.Equal("amount", order.VariableName);
            Assert.Equal(VariableType.Long, order.Type);
        }

    }
}





