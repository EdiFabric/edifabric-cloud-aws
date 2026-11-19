using System.Net;
using System.Text;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using EdiFabric.Api.AWS;
using EdiFabric.Native.X12;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.Json.JsonSerializer))]

public class EdiFunctions
{
    // The request limits of ediFabric Cloud v3: 5 MB of X12 for parse, 50 MB of JSON for build.
    // API Gateway and Lambda accept smaller payloads (10 MB and 6 MB), so build is limited by them first.
    private const int ParseRequestLimit = 5 * 1024 * 1024;
    private const int BuildRequestLimit = 50 * 1024 * 1024;
    private const string OffsetHeader = "X-EdiFabric-Offset";
    private readonly string _noData = "No data in request body.";

    static EdiFunctions()
    {
        if (string.IsNullOrEmpty(Configuration.ApiKey))
            throw new Exception("No ApiKey configuration.");

        EdiFabricX12.Load(string.IsNullOrWhiteSpace(Configuration.LibraryPath) ? null : Configuration.LibraryPath);

        //  Local models in map/map.json, if there are any, then the license and model map of the configured serial.
        var localModels = new LocalModelsService();
        localModels.Init(Path.Combine(AppContext.BaseDirectory, "map", "map.json"));
        SerialGate.Init(localModels, Configuration.ApiKey);
    }

    public Task<APIGatewayProxyResponse> Read(APIGatewayProxyRequest req, ILambdaLogger logger)
    {
        if (req == null || req.Body == null || req.Body.Length == 0)
        {
            logger.LogError(_noData);
            return Task.FromResult(ErrorHandler.BuildErrorResponse(HttpStatusCode.BadRequest, _noData));
        }

        try
        {
            var edi = ReadBody(req, req.GetReadParams().CharSet);
            var result = SerialGate.Run(GetApiKey(req), () => EdiFabricX12.Parse(edi, ParseMode.Json));

            return Task.FromResult(new APIGatewayProxyResponse
            {
                StatusCode = (int)HttpStatusCode.OK,
                Body = result.Transactions,
                Headers = new Dictionary<string, string> { { "Content-Type", "application/json; charset=utf-8" } }
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex.ToString());
            return Task.FromResult(ErrorHandler.BuildErrorResponse(ex));
        }
    }

    public Task<APIGatewayProxyResponse> Write(APIGatewayProxyRequest req, ILambdaLogger logger)
    {
        if (req == null || req.Body == null || req.Body.Length == 0)
        {
            logger.LogError(_noData);
            return Task.FromResult(ErrorHandler.BuildErrorResponse(HttpStatusCode.BadRequest, _noData));
        }

        try
        {
            var writeParams = req.GetWriteParams();
            var json = ReadBodyAsString(req);
            var edi = SerialGate.Run(GetApiKey(req), () => EdiFabricX12.Build(json, writeParams.Postfix));

            return Task.FromResult(new APIGatewayProxyResponse
            {
                StatusCode = (int)HttpStatusCode.OK,
                Body = edi,
                Headers = new Dictionary<string, string> { { "Content-Type", writeParams.ToContentType() } }
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex.ToString());
            return Task.FromResult(ErrorHandler.BuildErrorResponse(ex));
        }
    }

    public Task<APIGatewayProxyResponse> Validate(APIGatewayProxyRequest req, ILambdaLogger logger)
    {
        if (req == null || req.Body == null || req.Body.Length == 0)
        {
            logger.LogError(_noData);
            return Task.FromResult(ErrorHandler.BuildErrorResponse(HttpStatusCode.BadRequest, _noData));
        }

        try
        {
            var config = req.GetValidateParams().ToConfig();
            var result = SerialGate.Run(GetApiKey(req), () => EdiFabricX12.Parse(ReadEdi(req), ParseMode.JsonValidate, config));

            return Task.FromResult(new APIGatewayProxyResponse
            {
                StatusCode = (int)HttpStatusCode.OK,
                Body = result.Report,
                Headers = new Dictionary<string, string> { { "Content-Type", "application/json; charset=utf-8" } }
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex.ToString());
            return Task.FromResult(ErrorHandler.BuildErrorResponse(ex));
        }
    }

    public Task<APIGatewayProxyResponse> Ack(APIGatewayProxyRequest req, ILambdaLogger logger)
    {
        if (req == null || req.Body == null || req.Body.Length == 0)
        {
            logger.LogError(_noData);
            return Task.FromResult(ErrorHandler.BuildErrorResponse(HttpStatusCode.BadRequest, _noData));
        }

        try
        {
            var config = req.GetAckParams().ToConfig();
            var result = SerialGate.Run(GetApiKey(req), () => EdiFabricX12.Parse(ReadEdi(req), ParseMode.JsonValidateAck, config));

            return Task.FromResult(new APIGatewayProxyResponse
            {
                StatusCode = (int)HttpStatusCode.OK,
                Body = result.Report,
                Headers = new Dictionary<string, string> { { "Content-Type", "application/json; charset=utf-8" } }
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex.ToString());
            return Task.FromResult(ErrorHandler.BuildErrorResponse(ex));
        }
    }

    /// <summary>
    /// ediFabric Cloud v3 Parse X12: a raw X12 file in, its JSON out, with the validation report in mode 2 and the
    /// acknowledgment in mode 3, in one call. In modes 2 and 3 the body is two JSON documents back to back; the report
    /// starts at the byte offset in the X-EdiFabric-Offset header.
    /// </summary>
    public Task<APIGatewayProxyResponse> Parse(APIGatewayProxyRequest req, ILambdaLogger logger)
    {
        try
        {
            if (!HasContentType(req, "application/octet-stream"))
                return Task.FromResult(ErrorHandler.BuildErrorResponse(HttpStatusCode.UnsupportedMediaType, "The Content-Type must be application/octet-stream."));

            if (req.Body == null || req.Body.Length == 0)
            {
                logger.LogError(_noData);
                return Task.FromResult(ErrorHandler.BuildErrorResponse(HttpStatusCode.BadRequest, _noData));
            }

            var parseParams = req.GetParseParams();
            var mode = parseParams.ToMode();
            var edi = ReadBody(req, ParseRequestLimit, parseParams.CharSet);
            var (output, length, offset) = SerialGate.Run(GetApiKey(req), () => EdiFabricX12.ParseUtf8(edi, mode, parseParams.Config));

            return Task.FromResult(new APIGatewayProxyResponse
            {
                StatusCode = (int)HttpStatusCode.OK,
                Body = Encoding.UTF8.GetString(output, 0, length),
                Headers = new Dictionary<string, string>
                {
                    { "Content-Type", mode == ParseMode.Json ? "application/json; charset=utf-8" : "application/octet-stream" },
                    { OffsetHeader, (mode == ParseMode.Json ? 0 : offset).ToString() }
                }
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex.ToString());
            return Task.FromResult(ErrorHandler.BuildErrorResponse(ex));
        }
    }

    /// <summary>
    /// ediFabric Cloud v3 Build X12: an X12Interchange, or an array of them, in the v3 JSON format, out as raw X12.
    /// The segments are written as they are, so the JSON must include SE, GE and IEA with the correct counts.
    /// </summary>
    public Task<APIGatewayProxyResponse> Build(APIGatewayProxyRequest req, ILambdaLogger logger)
    {
        try
        {
            if (!HasContentType(req, "application/json"))
                return Task.FromResult(ErrorHandler.BuildErrorResponse(HttpStatusCode.UnsupportedMediaType, "The Content-Type must be application/json."));

            if (req.Body == null || req.Body.Length == 0)
            {
                logger.LogError(_noData);
                return Task.FromResult(ErrorHandler.BuildErrorResponse(HttpStatusCode.BadRequest, _noData));
            }

            var json = ReadBody(req, BuildRequestLimit);
            var postfix = req.GetBuildParams().Postfix;
            var (output, length) = SerialGate.Run(GetApiKey(req), () => EdiFabricX12.BuildUtf8(WithoutBom(json), postfix));

            return Task.FromResult(new APIGatewayProxyResponse
            {
                StatusCode = (int)HttpStatusCode.OK,
                Body = Encoding.UTF8.GetString(output, 0, length),
                Headers = new Dictionary<string, string> { { "Content-Type", "application/octet-stream; charset=utf-8" } }
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex.ToString());
            return Task.FromResult(ErrorHandler.BuildErrorResponse(ex));
        }
    }

    private static bool HasContentType(APIGatewayProxyRequest req, string mediaType)
    {
        var contentType = req.GetContentType();
        return contentType != null && contentType.Split(';')[0].Trim().Equals(mediaType, StringComparison.OrdinalIgnoreCase);
    }

    private static ReadOnlySpan<byte> WithoutBom(ReadOnlySpan<byte> bytes)
        => bytes.StartsWith(Encoding.UTF8.Preamble) ? bytes[Encoding.UTF8.Preamble.Length..] : bytes;

    // The body arrives as a string, base64 when API Gateway treats the media type as binary. The limit applies to the decoded bytes.
    private static byte[] ReadBody(APIGatewayProxyRequest req, int limit, string? charSet = null)
    {
        var bytes = ReadBody(req, charSet);
        if (bytes.Length > limit)
            throw new RequestTooLargeException(limit);
        return bytes;
    }

    // Validate and ack also take transaction JSON, which is built into X12 first. Native, so it runs inside SerialGate.
    private static byte[] ReadEdi(APIGatewayProxyRequest req)
    {
        var contentType = req.GetContentType();
        if (contentType != null && contentType.StartsWith("application/json", StringComparison.OrdinalIgnoreCase))
        {
            var json = ReadBodyAsString(req);
            return Encoding.UTF8.GetBytes(EdiFabricX12.Build(json));
        }

        return ReadBody(req);
    }

    private static byte[] ReadBody(APIGatewayProxyRequest req, string? charSet = null)
    {
        var bytes = req.IsBase64Encoded
            ? Convert.FromBase64String(req.Body)
            : Encoding.UTF8.GetBytes(req.Body);

        if (string.IsNullOrEmpty(charSet))
            return bytes;

        var text = GetEncoding(charSet).GetString(bytes);
        return Encoding.UTF8.GetBytes(text);
    }

    // An unknown charSet is the caller's mistake (400), as in ediFabric Cloud.
    private static Encoding GetEncoding(string charSet)
    {
        try
        {
            return Encoding.GetEncoding(charSet);
        }
        catch (ArgumentException ex)
        {
            throw new InvalidDataException($"Invalid charSet '{charSet}'.", ex);
        }
    }

    private static string ReadBodyAsString(APIGatewayProxyRequest req)
    {
        return req.IsBase64Encoded
            ? Encoding.UTF8.GetString(Convert.FromBase64String(req.Body))
            : req.Body;
    }

    // The serial of the request, as in ediFabric Cloud, or the configured one. SerialGate runs the native calls under it.
    private static string GetApiKey(APIGatewayProxyRequest req)
    {
        var apiKey = req.GetHeader("Ocp-Apim-Subscription-Key");
        if (!string.IsNullOrWhiteSpace(apiKey))
            return apiKey.Trim();

        return Configuration.ApiKey;
    }
}
