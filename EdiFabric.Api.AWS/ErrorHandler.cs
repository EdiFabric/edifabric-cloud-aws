using Amazon.Lambda.APIGatewayEvents;
using System.Net;
using System.Text.Json;
using EdiFabric.Native.X12;

static class ErrorHandler
{
    public static APIGatewayProxyResponse BuildErrorResponse(HttpStatusCode statusCode, string message)
    {
        return new APIGatewayProxyResponse
        {
            StatusCode = (int)statusCode,
            Body = JsonSerializer.Serialize(new
            {
                Code = (int)statusCode,
                Details = new List<string> { message }
            }),
            Headers = new Dictionary<string, string> { { "Content-Type", "application/json" } }
        };
    }

    public static APIGatewayProxyResponse BuildErrorResponse(Exception ex)
    {
        return BuildErrorResponse(StatusCodeFor(ex), ex.Message);
    }

    private static HttpStatusCode StatusCodeFor(Exception ex)
    {
        if (ex is InvalidDataException)
            return HttpStatusCode.BadRequest;

        if (ex is EdiFabricException ediEx)
        {
            return ediEx.Code is
                (int)EdiFabricErrorCode.IncorrectInput or
                (int)EdiFabricErrorCode.MapNotSet or
                (int)EdiFabricErrorCode.IncorrectMode or
                (int)EdiFabricErrorCode.ConfigDeserialization or
                (int)EdiFabricErrorCode.SplitSegmentIdMissing
                ? HttpStatusCode.BadRequest
                : HttpStatusCode.InternalServerError;
        }

        return HttpStatusCode.InternalServerError;
    }
}
