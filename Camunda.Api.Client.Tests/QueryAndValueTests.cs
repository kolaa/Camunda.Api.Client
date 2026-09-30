using System;
using System.Globalization;
using System.IO;
using Camunda.Api.Client.UserTask;
using Iana;
using Xunit;

namespace Camunda.Api.Client.Tests
{
    public class QueryAndValueTests
    {
        [Fact]
        public void TaskQuery_SortByPlainField_AddsSortingInfo()
        {
            var query = new TaskQuery();

            var result = query.Sort(TaskSorting.DueDate, SortOrder.Descending);

            Assert.Same(query, result);
            var sorting = Assert.Single(query.Sorting);
            Assert.Equal(TaskSorting.DueDate, sorting.SortBy);
            Assert.Equal(SortOrder.Descending, sorting.SortOrder);
            Assert.Null(sorting.Parameters);
        }

        [Fact]
        public void TaskQuery_SortByVariable_AddsVariableParameters()
        {
            var query = new TaskQuery();

            query.Sort(TaskSorting.ProcessVariable, variable: new VariableOrder("amount", VariableType.Long));

            var sorting = Assert.Single(query.Sorting);
            Assert.Equal(TaskSorting.ProcessVariable, sorting.SortBy);
            Assert.Equal("amount", sorting.Parameters["variable"]);
            Assert.Equal("Long", sorting.Parameters["type"]);
        }

        [Fact]
        public void TaskQuery_SortByVariable_RequiresVariableOrder()
        {
            var query = new TaskQuery();

            var missing = Assert.Throws<ArgumentException>(() => query.Sort(TaskSorting.TaskVariable));
            Assert.Contains("Variable is mandatory", missing.Message);
            Assert.Equal("variable", missing.ParamName);

            var unexpected = Assert.Throws<ArgumentException>(() =>
                query.Sort(TaskSorting.Assignee, variable: new VariableOrder("v", VariableType.String)));
            Assert.Equal("variable", unexpected.ParamName);
        }

        [Fact]
        public void VariableValue_ImplementsIConvertible()
        {
            var numeric = VariableValue.FromObject(42);
            var convertible = (IConvertible)numeric;

            Assert.Equal(TypeCode.Int32, convertible.GetTypeCode());
            Assert.True(convertible.ToBoolean(CultureInfo.InvariantCulture));
            Assert.Equal('*', convertible.ToChar(CultureInfo.InvariantCulture));
            Assert.Equal((sbyte)42, convertible.ToSByte(CultureInfo.InvariantCulture));
            Assert.Equal((byte)42, convertible.ToByte(CultureInfo.InvariantCulture));
            Assert.Equal((short)42, convertible.ToInt16(CultureInfo.InvariantCulture));
            Assert.Equal((ushort)42, convertible.ToUInt16(CultureInfo.InvariantCulture));
            Assert.Equal(42, convertible.ToInt32(CultureInfo.InvariantCulture));
            Assert.Equal(42u, convertible.ToUInt32(CultureInfo.InvariantCulture));
            Assert.Equal(42L, convertible.ToInt64(CultureInfo.InvariantCulture));
            Assert.Equal(42UL, convertible.ToUInt64(CultureInfo.InvariantCulture));
            Assert.Equal(42f, convertible.ToSingle(CultureInfo.InvariantCulture));
            Assert.Equal(42d, convertible.ToDouble(CultureInfo.InvariantCulture));
            Assert.Equal(42m, convertible.ToDecimal(CultureInfo.InvariantCulture));
            Assert.Equal("42", convertible.ToString(CultureInfo.InvariantCulture));
            Assert.Equal(42, convertible.ToType(typeof(int), CultureInfo.InvariantCulture));

            var date = new DateTime(2020, 1, 2, 3, 4, 5, DateTimeKind.Unspecified);
            var dateValue = VariableValue.FromObject(date);
            Assert.Equal(date, ((IConvertible)dateValue).ToDateTime(CultureInfo.InvariantCulture));
        }

        [Fact]
        public void VariableValue_FileFactories_EncodeContent()
        {
            var path = Path.Combine(Path.GetTempPath(), $"camunda-{Guid.NewGuid():N}.txt");
            File.WriteAllText(path, "hello");

            try
            {
                var fromPath = VariableValue.FromFile(path, MediaTypes.Text.Plain);
                Assert.Equal(VariableType.File, fromPath.Type);
                Assert.Equal(Convert.ToBase64String(File.ReadAllBytes(path)), fromPath.GetValue<string>());
                Assert.Equal(Path.GetFileName(path), fromPath.ValueInfo[VariableValue.ValueInfoFileName]);
                Assert.Equal(MediaTypes.Text.Plain, fromPath.ValueInfo[VariableValue.ValueInfoFileMimeType]);

                var textFile = VariableValue.FromTextFile(path);
                Assert.Equal(VariableType.File, textFile.Type);
                Assert.Equal("utf-8", textFile.ValueInfo[VariableValue.ValueInfoFileEncoding]);
                Assert.Equal(MediaTypes.Text.Plain, textFile.ValueInfo[VariableValue.ValueInfoFileMimeType]);

                var binary = VariableValue.FromBinaryFile(path);
                Assert.Equal(VariableType.File, binary.Type);
                Assert.Equal(MediaTypes.Application.OctetStream, binary.ValueInfo[VariableValue.ValueInfoFileMimeType]);
                Assert.Equal("", binary.ValueInfo[VariableValue.ValueInfoFileEncoding]);
            }
            finally
            {
                File.Delete(path);
            }
        }
    }
}
