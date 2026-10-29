using System.Net;
using System.Text.Json;
using BlazorClient.Services;
using Xunit;

namespace BlazorClient.Tests
{
    /// <summary>Екран має показувати справжню причину збою читання, а не завжди «перевірте інтернет».</summary>
    public class ApiErrorsTests
    {
        private static HttpRequestException WithStatus(HttpStatusCode status) => new("failed", null, status);

        [Fact]
        public void Unauthorized_MapsToSignInRequired_With401()
        {
            var failure = ApiErrors.Describe(WithStatus(HttpStatusCode.Unauthorized));

            Assert.Equal("ErrorSignInRequired", failure.Key);
            Assert.Equal(401, failure.Status);
        }

        [Fact]
        public void Forbidden_MapsToForbidden_With403()
        {
            var failure = ApiErrors.Describe(WithStatus(HttpStatusCode.Forbidden));

            Assert.Equal("ErrorForbidden", failure.Key);
            Assert.Equal(403, failure.Status);
        }

        [Theory]
        [InlineData(HttpStatusCode.InternalServerError, 500)]
        [InlineData(HttpStatusCode.BadGateway, 502)]
        [InlineData(HttpStatusCode.NotFound, 404)]
        public void OtherStatusCodes_MapToServerFailure_KeepingTheCode(HttpStatusCode status, int expected)
        {
            var failure = ApiErrors.Describe(WithStatus(status));

            Assert.Equal("ErrorServerFailure", failure.Key);
            Assert.Equal(expected, failure.Status);
        }

        [Fact]
        public void NoResponse_MapsToCannotReachApi_WithoutStatus()
        {
            var failure = ApiErrors.Describe(new HttpRequestException("Failed to fetch"));

            Assert.Equal("ErrorCannotReachApi", failure.Key);
            Assert.Null(failure.Status);
        }

        [Theory]
        [InlineData(typeof(JsonException))]
        [InlineData(typeof(NotSupportedException))]
        public void NonJsonAnswer_MapsToUnexpectedResponse(Type exceptionType)
        {
            var exception = (Exception)Activator.CreateInstance(exceptionType, "not json")!;

            Assert.Equal("ErrorUnexpectedResponse", ApiErrors.Describe(exception).Key);
        }

        [Fact]
        public void AnythingElse_MapsToServerFailure()
        {
            Assert.Equal("ErrorServerFailure", ApiErrors.Describe(new InvalidOperationException("boom")).Key);
        }
    }
}
