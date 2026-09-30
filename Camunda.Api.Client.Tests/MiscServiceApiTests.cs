using System.Threading.Tasks;
using Xunit;

namespace Camunda.Api.Client.Tests
{
    public class MiscServiceApiTests
    {
        private static string Path(RecordedRequest request) => request.PathAndQuery.Split('?')[0];

        [Fact]
        public async Task GroupResource_CanGetUpdateDeleteAndManageMembers()
        {
            var handler = new TestHttpMessageHandler("{\"id\":\"g-1\",\"name\":\"mgmt\"}");
            var client = handler.CreateClient();

            var group = await client.Group["g-1"].Get();
            Assert.Equal(System.Net.Http.HttpMethod.Get, handler.Last.Method);
            Assert.Equal("/group/g-1", handler.Last.PathAndQuery);
            Assert.Equal("g-1", group.Id);

            handler.ResponseJson = "null";
            await client.Group["g-1"].Update(new Group.GroupInfo { Name = "new" });
            Assert.Equal(System.Net.Http.HttpMethod.Put, handler.Last.Method);
            Assert.Equal("/group/g-1", handler.Last.PathAndQuery);
            Assert.Equal("{\"name\":\"new\"}", handler.Last.Body);

            await client.Group["g-1"].Delete();
            Assert.Equal(System.Net.Http.HttpMethod.Delete, handler.Last.Method);
            Assert.Equal("/group/g-1", handler.Last.PathAndQuery);

            await client.Group["g-1"].AddMember("kermit");
            Assert.Equal("/group/g-1/members/kermit", handler.Last.PathAndQuery);

            await client.Group["g-1"].RemoveMember("kermit");
            Assert.Equal(System.Net.Http.HttpMethod.Delete, handler.Last.Method);
            Assert.Equal("/group/g-1/members/kermit", handler.Last.PathAndQuery);

            await client.Group.Create(new Group.GroupInfo { Id = "g-2", Name = "n", Type = "team" });
            Assert.Equal(System.Net.Http.HttpMethod.Post, handler.Last.Method);
            Assert.Equal("/group/create", handler.Last.PathAndQuery);
            Assert.Equal("{\"id\":\"g-2\",\"name\":\"n\",\"type\":\"team\"}", handler.Last.Body);
        }

        [Fact]
        public async Task GroupQuery_UsesGetEndpoint()
        {
            var handler = new TestHttpMessageHandler("[]");
            var client = handler.CreateClient();

            Assert.NotNull(await client.Group.Query().List());
            Assert.Equal("/group", Path(handler.Last));

            handler.ResponseJson = "{\"count\":1}";
            Assert.Equal(1, await client.Group.Query().Count());
            Assert.Equal("/group/count?sortBy=id&sortOrder=asc", handler.Last.PathAndQuery);
        }

        [Fact]
        public async Task UserResource_CanReadUpdatePasswordAndDelete()
        {
            var handler = new TestHttpMessageHandler("{\"id\":\"u-1\",\"firstName\":\"Kermit\"}");
            var client = handler.CreateClient();

            var profile = await client.Users["u-1"].Get();
            Assert.Equal(System.Net.Http.HttpMethod.Get, handler.Last.Method);
            Assert.Equal("/user/u-1/profile", handler.Last.PathAndQuery);
            Assert.Equal("Kermit", profile.FirstName);

            handler.ResponseJson = "null";
            await client.Users["u-1"].Update(new User.UserProfileInfo { FirstName = "Kermit" });
            Assert.Equal(System.Net.Http.HttpMethod.Put, handler.Last.Method);
            Assert.Equal("/user/u-1/profile", handler.Last.PathAndQuery);

            await client.Users["u-1"].SetPassword("new-pw", "old-pw");
            Assert.Equal("/user/u-1/credentials", handler.Last.PathAndQuery);

            await client.Users["u-1"].Delete();
            Assert.Equal(System.Net.Http.HttpMethod.Delete, handler.Last.Method);
            Assert.Equal("/user/u-1", handler.Last.PathAndQuery);

            await client.Users.Create(new User.UserProfileInfo { Id = "u-2" }, "secret");
            Assert.Equal(System.Net.Http.HttpMethod.Post, handler.Last.Method);
            Assert.Equal("/user/create", handler.Last.PathAndQuery);
            Assert.Contains("\"password\":\"secret\"", handler.Last.Body);
            Assert.Contains("\"id\":\"u-2\"", handler.Last.Body);
        }

        [Fact]
        public async Task UserQuery_UsesGetEndpoint()
        {
            var handler = new TestHttpMessageHandler("[]");
            var client = handler.CreateClient();

            Assert.NotNull(await client.Users.Query().List());
            Assert.Equal("/user", Path(handler.Last));
        }

        [Fact]
        public async Task TenantService_CanCreateAndQuery()
        {
            var handler = new TestHttpMessageHandler("{\"id\":\"t-1\"}");
            var client = handler.CreateClient();

            await client.Tenants.Create(new Tenant.TenantInfo { Id = "t-1", Name = "n" });
            Assert.Equal(System.Net.Http.HttpMethod.Post, handler.Last.Method);
            Assert.Equal("/tenant/create", handler.Last.PathAndQuery);
            Assert.Equal("{\"id\":\"t-1\",\"name\":\"n\"}", handler.Last.Body);

            handler.ResponseJson = "[]";
            Assert.NotNull(await client.Tenants.Query().List());
            Assert.Equal("/tenant", Path(handler.Last));
        }

        [Fact]
        public async Task IncidentQuery_UsesGetEndpoint()
        {
            var handler = new TestHttpMessageHandler("{\"count\":0}");
            var client = handler.CreateClient();

            Assert.Equal(0, await client.Incidents.Query().Count());
            Assert.Equal("/incident/count?sortBy=incidentId&sortOrder=asc", handler.Last.PathAndQuery);

            handler.ResponseJson = "[]";
            Assert.NotNull(await client.Incidents.Query(new Incident.IncidentQuery { IncidentType = "failedJob" }).List());
            Assert.Equal("/incident", Path(handler.Last));
            Assert.Equal("failedJob", System.Net.WebUtility.UrlDecode(
                handler.Last.PathAndQuery.Substring(handler.Last.PathAndQuery.IndexOf("incidentType=") + "incidentType=".Length)
                    .Split('&')[0]));
        }

        [Fact]
        public async Task SignalService_ThrowsSignal()
        {
            var handler = new TestHttpMessageHandler("null");
            var client = handler.CreateClient();

            await client.Signals.ThrowSignal(new Signal.Signal { Name = "sig" });

            Assert.Equal(System.Net.Http.HttpMethod.Post, handler.Last.Method);
            Assert.Equal("/signal", handler.Last.PathAndQuery);
            Assert.Equal("{\"name\":\"sig\",\"variables\":{},\"withoutTenantId\":false}", handler.Last.Body);
        }

        [Fact]
        public async Task ExecutionResource_CanGetTriggerAndReadSubscriptions()
        {
            var handler = new TestHttpMessageHandler("{\"id\":\"e-1\"}");
            var client = handler.CreateClient();

            var execution = await client.Executions["e-1"].Get();
            Assert.Equal(System.Net.Http.HttpMethod.Get, handler.Last.Method);
            Assert.Equal("/execution/e-1", handler.Last.PathAndQuery);
            Assert.Equal("e-1", execution.Id);

            handler.ResponseJson = "null";
            await client.Executions["e-1"].Trigger(new Execution.ExecutionTrigger());
            Assert.Equal(System.Net.Http.HttpMethod.Post, handler.Last.Method);
            Assert.Equal("/execution/e-1/signal", handler.Last.PathAndQuery);

            handler.ResponseJson = "{\"id\":\"sub-1\",\"eventName\":\"msg\"}";
            var subscription = await client.Executions["e-1"].GetMessageEventSubscription("msg").Get();
            Assert.Equal("/execution/e-1/messageSubscriptions/msg", handler.Last.PathAndQuery);
            Assert.Equal("msg", subscription.EventName);

            handler.ResponseJson = "null";
            await client.Executions["e-1"].GetMessageEventSubscription("msg").Trigger(new Execution.ExecutionTrigger());
            Assert.Equal("/execution/e-1/messageSubscriptions/msg/trigger", handler.Last.PathAndQuery);

            handler.ResponseJson = "{\"type\":\"String\",\"value\":\"x\"}";
            await client.Executions["e-1"].LocalVariables.Get("a");
            Assert.Equal("/execution/e-1/localVariables/a?deserializeValue=true", handler.Last.PathAndQuery);
        }

        [Fact]
        public async Task ExecutionQuery_UsesPostEndpoint()
        {
            var handler = new TestHttpMessageHandler("[]");
            var client = handler.CreateClient();

            Assert.NotNull(await client.Executions.Query().List());
            Assert.Equal("/execution", Path(handler.Last));
        }

        [Fact]
        public async Task CaseInstanceResource_CanReadAndChangeState()
        {
            var handler = new TestHttpMessageHandler("{\"id\":\"ci-1\",\"state\":\"ACTIVE\"}");
            var client = handler.CreateClient();

            var caseInstance = await client.CaseInstances["ci-1"].Get();
            Assert.Equal(System.Net.Http.HttpMethod.Get, handler.Last.Method);
            Assert.Equal("/case-instance/ci-1", handler.Last.PathAndQuery);
            Assert.Equal("ci-1", caseInstance.Id);

            handler.ResponseJson = "null";
            await client.CaseInstances["ci-1"].Complete(new CaseInstance.ChangeCaseInstanceState());
            Assert.Equal(System.Net.Http.HttpMethod.Post, handler.Last.Method);
            Assert.Equal("/case-instance/ci-1/complete", handler.Last.PathAndQuery);

            await client.CaseInstances["ci-1"].Close(new CaseInstance.ChangeCaseInstanceState());
            Assert.Equal("/case-instance/ci-1/close", handler.Last.PathAndQuery);

            await client.CaseInstances["ci-1"].Terminate(new CaseInstance.ChangeCaseInstanceState());
            Assert.Equal("/case-instance/ci-1/terminate", handler.Last.PathAndQuery);

            handler.ResponseJson = "{\"type\":\"String\",\"value\":\"x\"}";
            var variable = await client.CaseInstances["ci-1"].Variables.Get("a");
            Assert.Equal("/case-instance/ci-1/variables/a?deserializeValue=true", handler.Last.PathAndQuery);
            Assert.Equal("x", variable.Value);
        }

        [Fact]
        public async Task CaseExecutionResource_CanRunTransitions()
        {
            var handler = new TestHttpMessageHandler("{\"id\":\"ce-1\"}");
            var client = handler.CreateClient();

            var execution = await client.CaseExecutions["ce-1"].Get();
            Assert.Equal("/case-execution/ce-1", handler.Last.PathAndQuery);
            Assert.Equal("ce-1", execution.Id);

            handler.ResponseJson = "null";
            await client.CaseExecutions["ce-1"].Start(new CaseExecution.CaseExecutionStart());
            Assert.Equal(System.Net.Http.HttpMethod.Post, handler.Last.Method);
            Assert.Equal("/case-execution/ce-1/manual-start", handler.Last.PathAndQuery);

            await client.CaseExecutions["ce-1"].Complete(new CaseExecution.CaseExecutionComplete());
            Assert.Equal("/case-execution/ce-1/complete", handler.Last.PathAndQuery);

            await client.CaseExecutions["ce-1"].Disable(new CaseExecution.CaseExecutionDisable());
            Assert.Equal("/case-execution/ce-1/disable", handler.Last.PathAndQuery);

            await client.CaseExecutions["ce-1"].ReEnable(new CaseExecution.CaseExecutionReEnable());
            Assert.Equal("/case-execution/ce-1/reenable", handler.Last.PathAndQuery);

            await client.CaseExecutions["ce-1"].Terminate(new CaseExecution.CaseExecutionTerminate());
            Assert.Equal("/case-execution/ce-1/terminate", handler.Last.PathAndQuery);
        }

        [Fact]
        public async Task CaseAndDecisionDefinitions_CanBeReadByKeyAndById()
        {
            var handler = new TestHttpMessageHandler("{\"id\":\"x-1\",\"key\":\"k\"}");
            var client = handler.CreateClient();

            await client.CaseDefinitions["cd-1"].Get();
            Assert.Equal("/case-definition/cd-1", handler.Last.PathAndQuery);

            await client.CaseDefinitions.ByKey("ck").Get();
            Assert.Equal("/case-definition/key/ck", handler.Last.PathAndQuery);

            await client.CaseDefinitions.ByKey("ck", "t-1").Get();
            Assert.Equal("/case-definition/key/ck/tenant-id/t-1", handler.Last.PathAndQuery);

            handler.ResponseJson = "null";
            await client.CaseDefinitions["cd-1"].GetXml();
            Assert.Equal("/case-definition/cd-1/xml", handler.Last.PathAndQuery);

            await client.DecisionDefinitions["dd-1"].Get();
            Assert.Equal("/decision-definition/dd-1", handler.Last.PathAndQuery);

            await client.DecisionDefinitions.ByKey("dk").Get();
            Assert.Equal("/decision-definition/key/dk", handler.Last.PathAndQuery);
        }

        [Fact]
        public async Task DecisionDefinition_EvaluateDecision()
        {
            var handler = new TestHttpMessageHandler("[{}]");
            var client = handler.CreateClient();

            var result = await client.DecisionDefinitions["dd-1"].Evaluate(
                new DecisionDefinition.EvaluateDecision());

            Assert.Equal(System.Net.Http.HttpMethod.Post, handler.Last.Method);
            Assert.Equal("/decision-definition/dd-1/evaluate", handler.Last.PathAndQuery);
            Assert.NotNull(result);
        }

        [Fact]
        public async Task ExternalTaskResource_CanManageLockAndCompletion()
        {
            var handler = new TestHttpMessageHandler("{\"id\":\"et-1\",\"workerId\":\"w\"}");
            var client = handler.CreateClient();

            var task = await client.ExternalTasks["et-1"].Get();
            Assert.Equal(System.Net.Http.HttpMethod.Get, handler.Last.Method);
            Assert.Equal("/external-task/et-1", handler.Last.PathAndQuery);
            Assert.Equal("et-1", task.Id);

            handler.ResponseJson = "null";
            await client.ExternalTasks["et-1"].SetRetries(3);
            Assert.Equal(System.Net.Http.HttpMethod.Put, handler.Last.Method);
            Assert.Equal("/external-task/et-1/retries", handler.Last.PathAndQuery);
            Assert.Equal("{\"retries\":3}", handler.Last.Body);

            await client.ExternalTasks["et-1"].SetPriority(7);
            Assert.Equal("/external-task/et-1/priority", handler.Last.PathAndQuery);
            Assert.Equal("{\"priority\":7}", handler.Last.Body);

            await client.ExternalTasks["et-1"].Complete(
                new ExternalTask.CompleteExternalTask { WorkerId = "w" });
            Assert.Equal(System.Net.Http.HttpMethod.Post, handler.Last.Method);
            Assert.Equal("/external-task/et-1/complete", handler.Last.PathAndQuery);

            await client.ExternalTasks["et-1"].HandleFailure(
                new ExternalTask.ExternalTaskFailure { WorkerId = "w", ErrorMessage = "boom", Retries = 1 });
            Assert.Equal("/external-task/et-1/failure", handler.Last.PathAndQuery);
            Assert.Contains("\"errorMessage\":\"boom\"", handler.Last.Body);

            await client.ExternalTasks["et-1"].HandleBpmnError(
                new ExternalTask.ExternalTaskBpmnError { WorkerId = "w", ErrorCode = "ERR" });
            Assert.Equal("/external-task/et-1/bpmnError", handler.Last.PathAndQuery);

            await client.ExternalTasks["et-1"].ExtendLock(
                new ExternalTask.ExternalTaskExtendLock { WorkerId = "w", NewDuration = 1000 });
            Assert.Equal("/external-task/et-1/extendLock", handler.Last.PathAndQuery);

            await client.ExternalTasks["et-1"].Unlock();
            Assert.Equal("/external-task/et-1/unlock", handler.Last.PathAndQuery);
        }

        [Fact]
        public async Task ExternalTask_FetchAndLockAndQuery()
        {
            var handler = new TestHttpMessageHandler("[]");
            var client = handler.CreateClient();

            var locked = await client.ExternalTasks.FetchAndLock(new ExternalTask.FetchExternalTasks
            {
                WorkerId = "w",
                MaxTasks = 5
            });

            Assert.NotNull(locked);
            Assert.Equal(System.Net.Http.HttpMethod.Post, handler.Last.Method);
            Assert.Equal("/external-task/fetchAndLock", handler.Last.PathAndQuery);
            Assert.Contains("\"workerId\":\"w\"", handler.Last.Body);
            Assert.Contains("\"maxTasks\":5", handler.Last.Body);

            Assert.NotNull(await client.ExternalTasks.Query().List());
            Assert.Equal("/external-task", Path(handler.Last));

            handler.ResponseJson = "{\"count\":2}";
            Assert.Equal(2, await client.ExternalTasks.Query().Count());
            Assert.Equal("/external-task/count", handler.Last.PathAndQuery);
        }

        [Fact]
        public async Task JobDefinitionResource_CanManageJobs()
        {
            var handler = new TestHttpMessageHandler("{\"id\":\"jd-1\"}");
            var client = handler.CreateClient();

            var definition = await client.JobDefinitions["jd-1"].Get();
            Assert.Equal(System.Net.Http.HttpMethod.Get, handler.Last.Method);
            Assert.Equal("/job-definition/jd-1", handler.Last.PathAndQuery);
            Assert.Equal("jd-1", definition.Id);

            handler.ResponseJson = "null";
            await client.JobDefinitions["jd-1"].UpdateSuspensionState(true);
            Assert.Equal(System.Net.Http.HttpMethod.Put, handler.Last.Method);
            Assert.Equal("/job-definition/jd-1/suspended", handler.Last.PathAndQuery);

            await client.JobDefinitions["jd-1"].SetRetries(2);
            Assert.Equal("/job-definition/jd-1/retries", handler.Last.PathAndQuery);
            Assert.Equal("{\"retries\":2}", handler.Last.Body);

            await client.JobDefinitions["jd-1"].SetPriority(new JobDefinition.JobDefinitionPriority { Priority = 9 });
            Assert.Equal("/job-definition/jd-1/jobPriority", handler.Last.PathAndQuery);
            Assert.Equal("{\"priority\":9,\"includeJobs\":false}", handler.Last.Body);

            await client.JobDefinitions.UpdateSuspensionState(
                new JobDefinition.JobDefinitionSuspensionState { Suspended = true, ProcessDefinitionKey = "pk" });
            Assert.Equal("/job-definition/suspended", handler.Last.PathAndQuery);
        }

        [Fact]
        public async Task VariableInstance_CanBeReadAndQueried()
        {
            var handler = new TestHttpMessageHandler("{\"id\":\"vi-1\",\"name\":\"a\"}");
            var client = handler.CreateClient();

            var variable = await client.VariableInstances["vi-1"].Get();
            Assert.Equal(System.Net.Http.HttpMethod.Get, handler.Last.Method);
            Assert.Equal("/variable-instance/vi-1", handler.Last.PathAndQuery);
            Assert.Equal("vi-1", variable.Id);

            handler.ResponseJson = "[]";
            Assert.NotNull(await client.VariableInstances.Query().List(0, 10));
            Assert.Equal("/variable-instance", Path(handler.Last));
        }

        [Fact]
        public async Task MessageService_DeliversCorrelation()
        {
            var handler = new TestHttpMessageHandler("[]");
            var client = handler.CreateClient();

            var results = await client.Messages.DeliverMessage(
                new Message.CorrelationMessage { MessageName = "msg" });

            Assert.NotNull(results);
            Assert.Equal(System.Net.Http.HttpMethod.Post, handler.Last.Method);
            Assert.Equal("/message", handler.Last.PathAndQuery);
            Assert.Contains("\"messageName\":\"msg\"", handler.Last.Body);
        }

        [Fact]
        public async Task MigrationService_GeneratesValidatesAndExecutes()
        {
            var handler = new TestHttpMessageHandler("{}");
            var client = handler.CreateClient();

            await client.Migrations.Generate(new Migration.MigrationPlanGeneration());
            Assert.Equal(System.Net.Http.HttpMethod.Post, handler.Last.Method);
            Assert.Equal("/migration/generate", handler.Last.PathAndQuery);

            await client.Migrations.Validate(new Migration.MigrationPlan());
            Assert.Equal("/migration/validate", handler.Last.PathAndQuery);

            await client.Migrations.Execute(new Migration.MigrationExecution());
            Assert.Equal("/migration/execute", handler.Last.PathAndQuery);

            await client.Migrations.ExecuteAsync(new Migration.MigrationExecution());
            Assert.Equal("/migration/executeAsync", handler.Last.PathAndQuery);
        }

        [Fact]
        public async Task FilterService_CanCreateAndQuery()
        {
            var handler = new TestHttpMessageHandler("{\"id\":\"f-1\"}");
            var client = handler.CreateClient();

            var filter = await client.Filters.Create(new Filter.FilterInfo.Request
            {
                ResourceType = "task",
                Name = "my filter"
            });

            Assert.Equal(System.Net.Http.HttpMethod.Post, handler.Last.Method);
            Assert.Equal("/filter/create", handler.Last.PathAndQuery);
            Assert.Equal("{\"resourceType\":\"task\",\"name\":\"my filter\"}", handler.Last.Body);
            Assert.Equal("f-1", filter.Id);

            handler.ResponseJson = "{\"id\":\"f-1\"}";
            var single = await client.Filters["f-1"].Get();
            Assert.Equal("/filter/f-1", handler.Last.PathAndQuery);
            Assert.Equal("f-1", single.Id);

            handler.ResponseJson = "[]";
            Assert.NotNull(await client.Filters.Query().List());
            Assert.Equal("/filter", Path(handler.Last));
        }

        [Fact]
        public async Task HistoryServices_CanQueryByRoute()
        {
            var handler = new TestHttpMessageHandler("[]");
            var client = handler.CreateClient();

            Assert.NotNull(await client.History.ProcessInstances.Query().List());
            Assert.Equal("/history/process-instance", Path(handler.Last));

            handler.ResponseJson = "{\"count\":1}";
            Assert.Equal(1, await client.History.ProcessInstances.Query().Count());
            Assert.Equal("/history/process-instance/count", handler.Last.PathAndQuery);

            handler.ResponseJson = "[]";
            Assert.NotNull(await client.History.UserTasks.Query().List());
            Assert.Equal("/history/task", Path(handler.Last));

            Assert.NotNull(await client.History.Incidents.Query().List());
            Assert.Equal("/history/incident", Path(handler.Last));

            Assert.NotNull(await client.History.VariableInstances.Query().List());
            Assert.Equal("/history/variable-instance", Path(handler.Last));

            Assert.NotNull(await client.History.Detail.Query().List());
            Assert.Equal("/history/detail", Path(handler.Last));

            Assert.NotNull(await client.History.DecisionInstances.Query().List());
            Assert.Equal("/history/decision-instance", Path(handler.Last));

            Assert.NotNull(await client.History.CaseInstances.Query().List());
            Assert.Equal("/history/case-instance", Path(handler.Last));

            Assert.NotNull(await client.History.ActivityInstances.Query().List());
            Assert.Equal("/history/activity-instance", Path(handler.Last));

            Assert.NotNull(await client.History.JobLogs.Query().List());
            Assert.Equal("/history/job-log", Path(handler.Last));

            Assert.NotNull(await client.History.ExternalTaskLogs.Query().List());
            Assert.Equal("/history/external-task-log", Path(handler.Last));
        }
    }
}


