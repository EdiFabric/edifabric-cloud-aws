using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;

public class X12Functions
{
    EdiFunctions _ediFunctions = new();

    public async Task<APIGatewayProxyResponse> ReadX12Async(APIGatewayProxyRequest request, ILambdaContext context)
    {
        return await _ediFunctions.Read(request, context.Logger);
    }

    public async Task<APIGatewayProxyResponse> WriteX12Async(APIGatewayProxyRequest request, ILambdaContext context)
    {
        return await _ediFunctions.Write(request, context.Logger);
    }

    public async Task<APIGatewayProxyResponse> ValidateX12Async(APIGatewayProxyRequest request, ILambdaContext context)
    {
        return await _ediFunctions.Validate(request, context.Logger);
    }

    public async Task<APIGatewayProxyResponse> AckX12Async(APIGatewayProxyRequest request, ILambdaContext context)
    {
        return await _ediFunctions.Ack(request, context.Logger);
    }

    public async Task<APIGatewayProxyResponse> ParseX12Async(APIGatewayProxyRequest request, ILambdaContext context)
    {
        return await _ediFunctions.Parse(request, context.Logger);
    }

    public async Task<APIGatewayProxyResponse> BuildX12Async(APIGatewayProxyRequest request, ILambdaContext context)
    {
        return await _ediFunctions.Build(request, context.Logger);
    }
}
