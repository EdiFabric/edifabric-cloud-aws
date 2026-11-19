using Amazon.Lambda.APIGatewayEvents;
using EdiFabric.Api.AWS.Models;

internal static class Extensions
{
    public static ReadParameters GetReadParams(this APIGatewayProxyRequest req)
    {
        var result = new ReadParameters();
        var query = req.ParseQuery();
        if (query is null)
            return result;

        result.CharSet = query.GetString("charSet");

        return result;
    }

    public static WriteParameters GetWriteParams(this APIGatewayProxyRequest req)
    {
        var result = new WriteParameters();
        var query = req.ParseQuery();
        if (query is null)
            return result;

        result.ContentType = query.GetString("contentType");
        result.CharSet = query.GetString("charSet");
        result.Postfix = query.GetString("postfix");

        return result;
    }

    public static ValidateParameters GetValidateParams(this APIGatewayProxyRequest req)
    {
        var result = new ValidateParameters();
        var query = req.ParseQuery();
        if (query is null)
            return result;

        BindValidate(result, query);
        return result;
    }

    public static AckParameters GetAckParams(this APIGatewayProxyRequest req)
    {
        var result = new AckParameters();
        var query = req.ParseQuery();
        if (query is null)
            return result;

        BindValidate(result, query);

        if (query.TryGetBool("suppressTa1", out var suppressTa1))
            result.SuppressTa1 = suppressTa1;
        if (query.TryGetBool("ak901p", out var ak901p))
            result.Ak901p = ak901p;
        if (query.TryGetBool("genForValid", out var genForValid))
            result.GenForValid = genForValid;
        if (query.TryGetBool("gen997", out var gen997))
            result.Gen997 = gen997;

        return result;
    }

    public static ParseParameters GetParseParams(this APIGatewayProxyRequest req)
    {
        var result = new ParseParameters();
        var query = req.ParseQuery();
        if (query is null)
            return result;

        result.Mode = query.GetString("mode");
        result.CharSet = query.GetString("charSet");
        result.Config = query.GetString("config");

        return result;
    }

    public static BuildParameters GetBuildParams(this APIGatewayProxyRequest req)
    {
        var result = new BuildParameters();
        var query = req.ParseQuery();
        if (query is null)
            return result;

        result.Postfix = query.GetString("postfix");

        return result;
    }

    public static string? GetContentType(this APIGatewayProxyRequest req) => req.GetHeader("Content-Type");

    public static string? GetHeader(this APIGatewayProxyRequest req, string name)
    {
        if (req.Headers == null)
            return null;

        if (req.Headers.TryGetValue(name, out var value) && !string.IsNullOrEmpty(value))
            return value;

        var match = req.Headers.FirstOrDefault(h => string.Equals(h.Key, name, StringComparison.OrdinalIgnoreCase));
        return string.IsNullOrEmpty(match.Value) ? null : match.Value;
    }

    private static void BindValidate(ValidateParameters result, Dictionary<string, string> query)
    {
        result.Regex = query.GetString("regex");
        result.DateFormat = query.GetString("dateFormat");
        result.TimeFormat = query.GetString("timeFormat");
        if (query.TryGetBool("skipSeqCount", out var skipSeqCount))
            result.SkipSeqCount = skipSeqCount;
        if (query.TryGetBool("skipHlSeq", out var skipHlSeq))
            result.SkipHlSeq = skipHlSeq;
        if (query.TryGetInt("snipLevel", out var snipLevel))
            result.SnipLevel = snipLevel;
        if (query.TryGetInt("maxErrors", out var maxErrors))
            result.MaxErrors = maxErrors;
    }

    private static Dictionary<string, string>? ParseQuery(this APIGatewayProxyRequest req)
    {
        if (req.QueryStringParameters == null || req.QueryStringParameters.Count == 0)
            return null;

        return req.QueryStringParameters
            .ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.OrdinalIgnoreCase);
    }

    private static string? GetString(this Dictionary<string, string> query, string key)
    {
        return query.TryGetValue(key, out var value) && !string.IsNullOrEmpty(value) ? value : null;
    }

    private static bool TryGetBool(this Dictionary<string, string> query, string key, out bool value)
    {
        value = default;
        var raw = query.GetString(key);
        return !string.IsNullOrEmpty(raw) && bool.TryParse(raw, out value);
    }

    private static bool TryGetInt(this Dictionary<string, string> query, string key, out int value)
    {
        value = default;
        var raw = query.GetString(key);
        return !string.IsNullOrEmpty(raw) && int.TryParse(raw, out value);
    }
}
