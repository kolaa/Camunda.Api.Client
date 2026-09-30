using System;
using System.Globalization;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Camunda.Api.Client.ProcessInstance;
using Refit;
using RichardSzalay.MockHttp;
using Xunit;

namespace Camunda.Api.Client.Tests
{
    public class DateTimeTests
    {
        [Theory]
        [InlineData("2010-01-01T01:01:01")]
        [InlineData("2010-05-01T01:01:01")]
        public void GetDateTime(string testDateTimeString)
        {
            var dateTime = DateTime.Parse(testDateTimeString, CultureInfo.InvariantCulture);
            var offset = TimeZoneInfo.Local.GetUtcOffset(dateTime);

            var expectedOffset = string.Format(
                CultureInfo.InvariantCulture,
                "{0}{1:00}{2:00}",
                offset < TimeSpan.Zero ? "-" : "+",
                Math.Abs(offset.Hours),
                Math.Abs(offset.Minutes));

            var expected = dateTime.ToString("yyyy-MM-ddTHH':'mm':'ss.fff", CultureInfo.InvariantCulture) + expectedOffset;

            Assert.Equal(expected, dateTime.ToJavaISO8601());
        }
    }
}
