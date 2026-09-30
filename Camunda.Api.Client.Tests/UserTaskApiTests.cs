using System.Collections.Generic;
using System.Threading.Tasks;
using Camunda.Api.Client.UserTask;
using Xunit;

namespace Camunda.Api.Client.Tests
{
    public class UserTaskApiTests
    {
        [Fact]
        public async Task Get_ReturnsTask()
        {
            var handler = new TestHttpMessageHandler("{\"id\":\"t-1\",\"name\":\"Do it\",\"assignee\":\"kermit\"}");
            var client = handler.CreateClient();

            var task = await client.UserTasks["t-1"].Get();

            Assert.Equal(System.Net.Http.HttpMethod.Get, handler.Last.Method);
            Assert.Equal("/task/t-1", handler.Last.PathAndQuery);
            Assert.Equal("t-1", task.Id);
            Assert.Equal("Do it", task.Name);
            Assert.Equal("kermit", task.Assignee);
        }

        [Fact]
        public async Task Query_PostsTaskQueryWithPaging()
        {
            var handler = new TestHttpMessageHandler("[]");
            var client = handler.CreateClient();

            var query = new TaskQuery { Assignee = "kermit" };
            var result = await client.UserTasks.Query(query).List(1, 10);

            Assert.NotNull(result);
            Assert.Equal(System.Net.Http.HttpMethod.Post, handler.Last.Method);
            Assert.Equal("/task?firstResult=1&maxResults=10", handler.Last.PathAndQuery);
            Assert.Contains("\"assignee\":\"kermit\"", handler.Last.Body);
        }

        [Fact]
        public async Task QueryCount_UsesCountEndpoint()
        {
            var handler = new TestHttpMessageHandler("{\"count\":2}");
            var client = handler.CreateClient();

            var count = await client.UserTasks.Query().Count();

            Assert.Equal(2, count);
            Assert.Equal("/task/count", handler.Last.PathAndQuery);
        }

        [Fact]
        public async Task Claim_SendsUserId()
        {
            var handler = new TestHttpMessageHandler("null");
            var client = handler.CreateClient();

            await client.UserTasks["t-1"].Claim("kermit");

            Assert.Equal(System.Net.Http.HttpMethod.Post, handler.Last.Method);
            Assert.Equal("/task/t-1/claim", handler.Last.PathAndQuery);
            Assert.Equal("{\"userId\":\"kermit\"}", handler.Last.Body);
        }

        [Fact]
        public async Task Unclaim_SendsPostWithoutBody()
        {
            var handler = new TestHttpMessageHandler("null");
            var client = handler.CreateClient();

            await client.UserTasks["t-1"].Unclaim();

            Assert.Equal(System.Net.Http.HttpMethod.Post, handler.Last.Method);
            Assert.Equal("/task/t-1/unclaim", handler.Last.PathAndQuery);
        }

        [Fact]
        public async Task SetAssignee_SendsUserId()
        {
            var handler = new TestHttpMessageHandler("null");
            var client = handler.CreateClient();

            await client.UserTasks["t-1"].SetAssignee("gonzo");

            Assert.Equal("/task/t-1/assignee", handler.Last.PathAndQuery);
            Assert.Equal("{\"userId\":\"gonzo\"}", handler.Last.Body);
        }

        [Fact]
        public async Task Delegate_SendsUserId()
        {
            var handler = new TestHttpMessageHandler("null");
            var client = handler.CreateClient();

            await client.UserTasks["t-1"].Delegate("gonzo");

            Assert.Equal("/task/t-1/delegate", handler.Last.PathAndQuery);
            Assert.Equal("{\"userId\":\"gonzo\"}", handler.Last.Body);
        }

        [Fact]
        public async Task Complete_SendsVariables()
        {
            var handler = new TestHttpMessageHandler("null");
            var client = handler.CreateClient();

            var complete = new CompleteTask().SetVariable("a", "x");
            complete.WithVariablesInReturn = true;

            await client.UserTasks["t-1"].Complete(complete);

            Assert.Equal("/task/t-1/complete", handler.Last.PathAndQuery);
            Assert.Contains("\"variables\":{\"a\":{\"type\":\"String\",\"value\":\"x\"}}", handler.Last.Body);
            Assert.Contains("\"withVariablesInReturn\":true", handler.Last.Body);
        }

        [Fact]
        public async Task CompleteAndFetchVariables_ReturnsVariables()
        {
            var handler = new TestHttpMessageHandler("{\"a\":{\"type\":\"String\",\"value\":\"x\"}}");
            var client = handler.CreateClient();

            var variables = await client.UserTasks["t-1"].CompleteAndFetchVariables(new CompleteTask());

            Assert.Equal("/task/t-1/complete", handler.Last.PathAndQuery);
            Assert.Equal("x", variables["a"].Value);
        }

        [Fact]
        public async Task Resolve_SendsVariables()
        {
            var handler = new TestHttpMessageHandler("null");
            var client = handler.CreateClient();

            await client.UserTasks["t-1"].Resolve(new ResolveTask().SetVariable("a", 1));

            Assert.Equal("/task/t-1/resolve", handler.Last.PathAndQuery);
            Assert.Contains("\"a\":{\"type\":\"Integer\",\"value\":1}", handler.Last.Body);
        }

        [Fact]
        public async Task SubmitForm_SendsCompleteTaskBody()
        {
            var handler = new TestHttpMessageHandler("null");
            var client = handler.CreateClient();

            await client.UserTasks["t-1"].SubmitForm(new CompleteTask());

            Assert.Equal("/task/t-1/submit-form", handler.Last.PathAndQuery);
        }

        [Fact]
        public async Task GetForm_ReturnsFormInfo()
        {
            var handler = new TestHttpMessageHandler("{\"key\":\"form-1\",\"contextPath\":\"/app\"}");
            var client = handler.CreateClient();

            var form = await client.UserTasks["t-1"].GetForm();

            Assert.Equal("/task/t-1/form", handler.Last.PathAndQuery);
            Assert.Equal("form-1", form.Key);
        }

        [Fact]
        public async Task GetRenderedForm_ReturnsHtmlString()
        {
            var handler = new TestHttpMessageHandler("<html>form</html>");
            var client = handler.CreateClient();

            var html = await client.UserTasks["t-1"].GetRenderedForm();

            Assert.Equal("/task/t-1/rendered-form", handler.Last.PathAndQuery);
            Assert.Equal("<html>form</html>", html);
        }

        [Fact]
        public async Task GetFormVariables_JoinsVariableNames()
        {
            var handler = new TestHttpMessageHandler("{}");
            var client = handler.CreateClient();

            await client.UserTasks["t-1"].GetFormVariables("a", "b");

            Assert.Matches(
                @"^/task/t-1/form-variables\?variableNames=a(%2C|,)b&deserializeValues=true$",
                handler.Last.PathAndQuery);
        }

        [Fact]
        public async Task Update_SendsTaskBody()
        {
            var handler = new TestHttpMessageHandler("null");
            var client = handler.CreateClient();

            await client.UserTasks["t-1"].Update(new Camunda.Api.Client.UserTask.UserTask { Assignee = "kermit" });

            Assert.Equal(System.Net.Http.HttpMethod.Put, handler.Last.Method);
            Assert.Equal("/task/t-1", handler.Last.PathAndQuery);
            Assert.Contains("\"assignee\":\"kermit\"", handler.Last.Body);
            Assert.Contains("\"priority\":0", handler.Last.Body);
        }

        [Fact]
        public async Task Delete_UsesDeleteMethod()
        {
            var handler = new TestHttpMessageHandler("null");
            var client = handler.CreateClient();

            await client.UserTasks["t-1"].Delete();

            Assert.Equal(System.Net.Http.HttpMethod.Delete, handler.Last.Method);
            Assert.Equal("/task/t-1", handler.Last.PathAndQuery);
        }

        [Fact]
        public async Task Comments_CanListAndCreate()
        {
            var handler = new TestHttpMessageHandler("[{\"id\":\"c-1\",\"message\":\"hello\"}]");
            var client = handler.CreateClient();

            var comments = await client.UserTasks["t-1"].Comment.GetAll();
            Assert.Equal("/task/t-1/comment", handler.Last.PathAndQuery);
            Assert.Equal("hello", comments[0].Message);

            handler.ResponseJson = "{\"id\":\"c-2\",\"message\":\"created\"}";
            var created = await client.UserTasks["t-1"].Comment.Create("created");

            Assert.Equal(System.Net.Http.HttpMethod.Post, handler.Last.Method);
            Assert.Equal("/task/t-1/comment/create", handler.Last.PathAndQuery);
            Assert.Equal("{\"message\":\"created\"}", handler.Last.Body);
            Assert.Equal("c-2", created.Id);

            handler.ResponseJson = "{\"id\":\"c-1\"}";
            var single = await client.UserTasks["t-1"].Comment.Get("c-1");
            Assert.Equal("/task/t-1/comment/c-1", handler.Last.PathAndQuery);
            Assert.Equal("c-1", single.Id);
        }

        [Fact]
        public async Task IdentityLinks_CanQueryAddAndDelete()
        {
            var handler = new TestHttpMessageHandler("[{\"userId\":\"kermit\",\"type\":\"assignee\"}]");
            var client = handler.CreateClient();

            var links = await client.UserTasks["t-1"].IdentityLink.GetAll();
            Assert.Equal("/task/t-1/identity-links", handler.Last.PathAndQuery);
            Assert.Equal("kermit", links[0].UserId);

            await client.UserTasks["t-1"].IdentityLink.GetAll(IdentityLinkType.Candidate);
            Assert.Equal("/task/t-1/identity-links?type=candidate", handler.Last.PathAndQuery);

            handler.ResponseJson = "null";
            await client.UserTasks["t-1"].IdentityLink.Add(new IdentityLink { GroupId = "mgmt", Type = IdentityLinkType.Candidate });
            Assert.Equal("/task/t-1/identity-links", handler.Last.PathAndQuery);
            Assert.Equal("{\"groupId\":\"mgmt\",\"type\":\"candidate\"}", handler.Last.Body);

            await client.UserTasks["t-1"].IdentityLink.Delete(new IdentityLink { GroupId = "mgmt", Type = IdentityLinkType.Candidate });
            Assert.Equal("/task/t-1/identity-links/delete", handler.Last.PathAndQuery);
        }

        [Fact]
        public async Task Variables_CanGetSetAndDelete()
        {
            var handler = new TestHttpMessageHandler("{\"type\":\"String\",\"value\":\"x\"}");
            var client = handler.CreateClient();

            var variable = await client.UserTasks["t-1"].Variables.Get("a");
            Assert.Equal("/task/t-1/variables/a?deserializeValue=true", handler.Last.PathAndQuery);
            Assert.Equal("x", variable.Value);

            handler.ResponseJson = "null";
            await client.UserTasks["t-1"].Variables.Set("a", VariableValue.FromObject("x"));
            Assert.Equal(System.Net.Http.HttpMethod.Put, handler.Last.Method);
            Assert.Equal("/task/t-1/variables/a", handler.Last.PathAndQuery);

            await client.UserTasks["t-1"].Variables.Delete("a");
            Assert.Equal(System.Net.Http.HttpMethod.Delete, handler.Last.Method);
            Assert.Equal("/task/t-1/variables/a", handler.Last.PathAndQuery);
        }

        [Fact]
        public async Task LocalVariables_UseLocalVariablePath()
        {
            var handler = new TestHttpMessageHandler("{\"type\":\"Integer\",\"value\":1}");
            var client = handler.CreateClient();

            var variable = await client.UserTasks["t-1"].LocalVariables.Get("a");

            Assert.Equal("/task/t-1/localVariables/a?deserializeValue=true", handler.Last.PathAndQuery);
            Assert.Equal(1, System.Convert.ToInt64(variable.Value));

            handler.ResponseJson = "{}";
            await client.UserTasks["t-1"].LocalVariables.GetAll();
            Assert.Equal("/task/t-1/localVariables?deserializeValues=true", handler.Last.PathAndQuery);
        }

        [Fact]
        public async Task GetTaskCountByCandidateGroup_ReturnsReport()
        {
            var handler = new TestHttpMessageHandler("[{\"groupName\":\"mgmt\",\"taskCount\":3},{\"taskCount\":2}]");
            var client = handler.CreateClient();

            var report = await client.UserTasks.GetTaskCountByCandidateGroup();

            Assert.Equal(System.Net.Http.HttpMethod.Get, handler.Last.Method);
            Assert.Equal("/task/report/candidate-group-count", handler.Last.PathAndQuery);
            Assert.Equal(2, report.Count);
            Assert.Equal(3, report[0].TaskCount);
            Assert.Equal("mgmt: 3", report[0].ToString());
            Assert.Equal("2", report[1].ToString());
        }

        [Fact]
        public async Task TaskResource_ToString_ReturnsId()
        {
            var client = CamundaClient.Create("http://localhost:8080/engine-rest");

            Assert.Equal("t-1", client.UserTasks["t-1"].ToString());
            Assert.Equal("t-1", client.UserTasks["t-1"].Comment.ToString());
        }
    }
}

