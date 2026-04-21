namespace OPCGateway.OPCServerMock.MockData;

public static class Server2DynamicReadData
{
    public static Dictionary<string, DynamicVariableParameters> GetOpcParametersMock()
    {
#pragma warning disable SA1509 // Opening braces should not be preceded by blank line
        return new Dictionary<string, DynamicVariableParameters>()
        {
            { "SomeDataNodeId", new DynamicVariableParameters(2, 18.0f, 0.5f, 0.03f) },
        };
    }
}