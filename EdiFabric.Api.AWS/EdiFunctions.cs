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
    private readonly string _noData = "No data in request body.";

    static EdiFunctions()
    {
        if (string.IsNullOrEmpty(Configuration.ApiKey))
            throw new Exception("No ApiKey configuration.");

        EdiFabricX12.Load(string.IsNullOrWhiteSpace(Configuration.LibraryPath) ? null : Configuration.LibraryPath);
        EdiFabricX12.SetSerial(Configuration.ApiKey);

        var localModels = new LocalModelsService();
        var mapPath = Path.Combine(AppContext.BaseDirectory, "map", "map.json");
        if (File.Exists(mapPath))
            localModels.Load(Configuration.ApiKey, mapPath);
        else
            localModels.LoadOnline(Configuration.ApiKey);
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
            Authorize(req);
            var edi = ReadBody(req, req.GetReadParams().CharSet);
            var result = EdiFabricX12.Parse(edi, ParseMode.Json);

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
            Authorize(req);
            var writeParams = req.GetWriteParams();
            var json = ReadBodyAsString(req);
            var edi = EdiFabricX12.Build(json, writeParams.Postfix);

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
            Authorize(req);
            var edi = ReadEdi(req);
            var result = EdiFabricX12.Parse(edi, ParseMode.JsonValidate, req.GetValidateParams().ToConfig());

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
            Authorize(req);
            var edi = ReadEdi(req);
            var result = EdiFabricX12.Parse(edi, ParseMode.JsonValidateAck, req.GetAckParams().ToConfig());

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

    private static void Authorize(APIGatewayProxyRequest req)
    {
        var apiKey = GetApiKey(req);
        EdiFabricX12.SetSerial(apiKey);
        //  Uncomment and then comment the line above if you wish to use distributed cache for tokens
        //  S3Cache.Set(apiKey);
    }

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

        var text = Encoding.GetEncoding(charSet).GetString(bytes);
        return Encoding.UTF8.GetBytes(text);
    }

    private static string ReadBodyAsString(APIGatewayProxyRequest req)
    {
        return req.IsBase64Encoded
            ? Encoding.UTF8.GetString(Convert.FromBase64String(req.Body))
            : req.Body;
    }

    private static string GetApiKey(APIGatewayProxyRequest req)
    {
        var apiKey = req.GetHeader("Ocp-Apim-Subscription-Key");
        if (!string.IsNullOrEmpty(apiKey))
            return apiKey;

        return Configuration.ApiKey;
    }
}
