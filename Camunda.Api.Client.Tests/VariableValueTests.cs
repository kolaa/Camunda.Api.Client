using System;
using System.Collections.Generic;
using Iana;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Xunit;

namespace Camunda.Api.Client.Tests
{
    public class VariableValueTests
    {
        private static string Serialize(VariableValue variable) =>
            JsonConvert.SerializeObject(variable, CamundaClient.JsonSerializerSettings);

        private static VariableValue Deserialize(string json) =>
            JsonConvert.DeserializeObject<VariableValue>(json, CamundaClient.JsonSerializerSettings);

        [Fact]
        public void FromObject_String_KeepsStringType()
        {
            var variable = VariableValue.FromObject("abc");

            Assert.Equal(VariableType.String, variable.Type);
            Assert.Equal("abc", variable.Value);
            Assert.Null(variable.ValueInfo);
            Assert.Equal("{\"type\":\"String\",\"value\":\"abc\"}", Serialize(variable));
        }

        [Fact]
        public void FromObject_Integers_MapToMatchingTypes()
        {
            Assert.Equal(VariableType.Integer, VariableValue.FromObject(1).Type);
            Assert.Equal(VariableType.Short, VariableValue.FromObject((short)1).Type);
            Assert.Equal(VariableType.Long, VariableValue.FromObject(1L).Type);
            Assert.Equal(VariableType.Number, VariableValue.FromObject(1m).Type);
        }

        [Fact]
        public void FromObject_Boolean_SerializesAsJsonBoolean()
        {
            var variable = VariableValue.FromObject(true);

            Assert.Equal(VariableType.Boolean, variable.Type);
            Assert.Equal("{\"type\":\"Boolean\",\"value\":true}", Serialize(variable));
        }

        [Fact]
        public void FromObject_Double_SerializesAsJsonNumber()
        {
            var variable = VariableValue.FromObject(1.5);

            Assert.Equal(VariableType.Double, variable.Type);
            Assert.Equal("{\"type\":\"Double\",\"value\":1.5}", Serialize(variable));
        }

        [Fact]
        public void FromObject_UtcDate_UsesJavaIso8601WithZeroOffset()
        {
            var variable = VariableValue.FromObject(new DateTime(2020, 5, 1, 12, 30, 0, DateTimeKind.Utc));

            Assert.Equal(VariableType.Date, variable.Type);
            Assert.Equal("{\"type\":\"Date\",\"value\":\"2020-05-01T12:30:00.000+0000\"}", Serialize(variable));
        }

        [Fact]
        public void FromObject_LocalDate_UsesLocalOffset()
        {
            var dateTime = new DateTime(2020, 5, 1, 12, 30, 0, DateTimeKind.Local);
            var offset = TimeZoneInfo.Local.GetUtcOffset(dateTime);

            var expectedOffset = string.Format(
                "{0}{1:00}{2:00}",
                offset < TimeSpan.Zero ? "-" : "+",
                Math.Abs(offset.Hours),
                Math.Abs(offset.Minutes));

            var variable = VariableValue.FromObject(dateTime);

            Assert.Equal(
                "{\"type\":\"Date\",\"value\":\"2020-05-01T12:30:00.000" + expectedOffset + "\"}",
                Serialize(variable));
        }

        [Fact]
        public void FromObject_Bytes_SerializesAsBase64()
        {
            var variable = VariableValue.FromObject(new byte[] { 1, 2, 3 });

            Assert.Equal(VariableType.Bytes, variable.Type);
            Assert.Equal("{\"type\":\"Bytes\",\"value\":\"AQID\"}", Serialize(variable));
        }

        [Fact]
        public void FromObject_Null_UsesNullTypeWithoutValue()
        {
            var variable = VariableValue.FromObject(null);

            Assert.Equal(VariableType.Null, variable.Type);
            Assert.Null(variable.Value);
            Assert.Equal("{\"type\":\"Null\"}", Serialize(variable));
        }

        [Fact]
        public void FromObject_ComplexObject_SerializesJsonAndValueInfo()
        {
            var variable = VariableValue.FromObject(new { id = 1, name = "x" });

            Assert.Equal(VariableType.Object, variable.Type);
            Assert.Equal(MediaTypes.Application.Json, variable.ValueInfo[VariableValue.ValueInfoSerializationDataFormat]);
            Assert.Equal("java.lang.Object", variable.ValueInfo[VariableValue.ValueInfoObjectTypeName]);

            var json = JObject.Parse(Serialize(variable));
            Assert.Equal("Object", json["type"]);
            Assert.Equal("{\"id\":1,\"name\":\"x\"}", json["value"]);
            Assert.Equal("application/json", json["valueInfo"]["serializationDataFormat"]);
            Assert.Equal("java.lang.Object", json["valueInfo"]["objectTypeName"]);
        }

        [Fact]
        public void FromFile_BuildsFileVariable()
        {
            var variable = VariableValue.FromFile(new byte[] { 1, 2, 3 }, "a.txt", "text/plain", "utf-8");

            Assert.Equal(VariableType.File, variable.Type);
            Assert.Equal("AQID", variable.Value);
            Assert.Equal("a.txt", variable.ValueInfo[VariableValue.ValueInfoFileName]);
            Assert.Equal("text/plain", variable.ValueInfo[VariableValue.ValueInfoFileMimeType]);
            Assert.Equal("utf-8", variable.ValueInfo[VariableValue.ValueInfoFileEncoding]);
        }

        [Fact]
        public void FromBinaryFile_UsesOctetStreamMimeType()
        {
            var variable = VariableValue.FromFile(new byte[] { 9 }, "a.bin", MediaTypes.Application.OctetStream);

            Assert.Equal(VariableType.File, variable.Type);
            Assert.Equal("a.bin", variable.ValueInfo[VariableValue.ValueInfoFileName]);
            Assert.Equal("", variable.ValueInfo[VariableValue.ValueInfoFileEncoding]);
            Assert.Equal(MediaTypes.Application.OctetStream, variable.ValueInfo[VariableValue.ValueInfoFileMimeType]);
        }

        [Fact]
        public void EnableTypedObject_UsesSerializedTypedObjectTypeName()
        {
            var variable = VariableValue.FromObject(new { id = 1 });
            variable.EnableTypedObject();

            Assert.Equal("dto.SerializedTypedObject", variable.ValueInfo[VariableValue.ValueInfoObjectTypeName]);
            Assert.Equal(MediaTypes.Application.Json, variable.ValueInfo[VariableValue.ValueInfoSerializationDataFormat]);
        }

        [Fact]
        public void Deserialize_StringValue()
        {
            var variable = Deserialize("{\"type\":\"String\",\"value\":\"abc\"}");

            Assert.Equal(VariableType.String, variable.Type);
            Assert.Equal("abc", variable.Value);
        }

        [Fact]
        public void Deserialize_IntegerValue_KeepsJsonNumber()
        {
            var variable = Deserialize("{\"type\":\"Integer\",\"value\":42}");

            Assert.Equal(VariableType.Integer, variable.Type);
            Assert.Equal(42, Convert.ToInt64(variable.Value));
        }

        [Fact]
        public void Deserialize_BooleanValue()
        {
            var variable = Deserialize("{\"type\":\"Boolean\",\"value\":false}");

            Assert.Equal(VariableType.Boolean, variable.Type);
            Assert.Equal(false, variable.Value);
        }

        [Fact]
        public void Deserialize_BytesValue_DecodesBase64()
        {
            var variable = Deserialize("{\"type\":\"Bytes\",\"value\":\"AQID\"}");

            Assert.Equal(VariableType.Bytes, variable.Type);
            Assert.Equal(new byte[] { 1, 2, 3 }, (byte[])variable.Value);
        }

        [Fact]
        public void Deserialize_NullValue()
        {
            var variable = Deserialize("{\"type\":\"Null\",\"value\":null}");

            Assert.Equal(VariableType.Null, variable.Type);
            Assert.Null(variable.Value);
        }

        [Fact]
        public void Deserialize_ObjectValue_KeepsJsonToken()
        {
            var variable = Deserialize("{\"type\":\"Object\",\"value\":{\"a\":1}}");

            Assert.Equal(VariableType.Object, variable.Type);
            var token = Assert.IsAssignableFrom<Newtonsoft.Json.Linq.JToken>(variable.Value);
            Assert.Equal(1, token["a"].Value<int>());
        }

        [Fact]
        public void Deserialize_PopulatesValueInfo()
        {
            var variable = Deserialize(
                "{\"type\":\"Object\",\"value\":\"{}\",\"valueInfo\":{" +
                "\"objectTypeName\":\"java.util.HashMap\",\"serializationDataFormat\":\"application/json\"}}");

            Assert.Equal("java.util.HashMap", variable.ValueInfo["objectTypeName"]);
            Assert.Equal("application/json", variable.ValueInfo["serializationDataFormat"]);
        }

        [Fact]
        public void GetValue_ConvertsUnderlyingValue()
        {
            Assert.Equal(42, VariableValue.FromObject("42").GetValue<int>());
            Assert.Equal(7L, VariableValue.FromObject("7").GetValue<long>());
            Assert.Equal("abc", VariableValue.FromObject("abc").GetValue<string>());
        }

        [Fact]
        public void ToString_HandlesNullAndBytes()
        {
            Assert.Equal("null", VariableValue.FromObject(null).ToString());
            Assert.Equal("{byte[3]}", VariableValue.FromObject(new byte[] { 1, 2, 3 }).ToString());
            Assert.Equal("abc", VariableValue.FromObject("abc").ToString());
        }

        [Fact]
        public void DictionarySet_CreatesVariableValueFromRawObject()
        {
            var variables = new Dictionary<string, VariableValue>();

            variables.Set("a", "text");
            variables.Set("b", VariableValue.FromObject(5));

            Assert.Equal(VariableType.String, variables["a"].Type);
            Assert.Equal("text", variables["a"].Value);
            Assert.Equal(VariableType.Integer, variables["b"].Type);
            Assert.Equal(5, variables["b"].Value);
        }
    }
}
