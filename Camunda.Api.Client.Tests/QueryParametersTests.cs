using System;
using System.Collections.Generic;
using Camunda.Api.Client.Deployment;
using Camunda.Api.Client.History;
using Xunit;

namespace Camunda.Api.Client.Tests
{
    public class QueryParametersTests
    {
        private class TestQuery : QueryParameters
        {
            public string Name;
            public bool Flag;
            public int Number;
            public List<string> Ids = new List<string>();
            public DateTime When;
            public string Empty;
            public string Missing;
        }

        private static QueryDictionary ToDictionary(TestQuery query) => query;

        [Fact]
        public void ConvertsScalarsToCamelCasedStrings()
        {
            var dictionary = ToDictionary(new TestQuery { Name = "n", Number = 42 });

            Assert.Equal("n", dictionary["name"]);
            Assert.Equal("42", dictionary["number"]);
        }

        [Fact]
        public void ConvertsBooleanToLowercase()
        {
            var dictionary = ToDictionary(new TestQuery { Flag = true });

            Assert.Equal("true", dictionary["flag"]);

            dictionary = ToDictionary(new TestQuery { Flag = false });
            Assert.Equal("false", dictionary["flag"]);
        }

        [Fact]
        public void JoinsArrayItemsWithComma()
        {
            var query = new TestQuery();
            query.Ids.AddRange(new[] { "a", "b", "c" });

            var dictionary = ToDictionary(query);

            Assert.Equal("a,b,c", dictionary["ids"]);
        }

        [Fact]
        public void DropsNullAndEmptyValues()
        {
            var dictionary = ToDictionary(new TestQuery { Name = null, Empty = "", Missing = null });

            Assert.False(dictionary.ContainsKey("name"));
            Assert.False(dictionary.ContainsKey("empty"));
            Assert.False(dictionary.ContainsKey("missing"));
        }

        [Fact]
        public void KeepsDefaultDateTimeValue()
        {
            var dictionary = ToDictionary(new TestQuery());

            Assert.Matches(@"^\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}\.\d{3}[+-]\d{4}$", dictionary["when"]);
        }

        [Fact]
        public void DropsEmptyArrays()
        {
            var dictionary = ToDictionary(new TestQuery());

            Assert.False(dictionary.ContainsKey("ids"));
        }

        [Fact]
        public void ConvertsDateToJavaIso8601()
        {
            var dictionary = ToDictionary(new TestQuery { When = new DateTime(2020, 5, 1, 12, 30, 0, DateTimeKind.Utc) });

            Assert.Matches(@"^\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}\.\d{3}[+-]\d{4}$", dictionary["when"]);
        }

        [Fact]
        public void DeploymentQuery_ConvertsEnumSortingToLowercase()
        {
            QueryDictionary dictionary = new DeploymentQuery { SortBy = DeploymentSorting.Name, SortOrder = SortOrder.Descending };

            Assert.Equal("name", dictionary["sortBy"]);
            Assert.Equal("desc", dictionary["sortOrder"]);
        }

        [Fact]
        public void DeploymentQuery_DropsUnsetNullableDate()
        {
            QueryDictionary dictionary = new DeploymentQuery { Id = "d1", Before = null };

            Assert.Equal("d1", dictionary["id"]);
            Assert.False(dictionary.ContainsKey("before"));
        }

        [Fact]
        public void HistoricQuery_ConvertsDateRange()
        {
            QueryDictionary dictionary = new HistoricActivityStatistics
            {
                StartedAfter = new DateTime(2020, 5, 1, 12, 30, 0, DateTimeKind.Utc)
            };

            Assert.Matches(@"^\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}\.\d{3}[+-]\d{4}$", dictionary["startedAfter"]);
        }
    }
}
