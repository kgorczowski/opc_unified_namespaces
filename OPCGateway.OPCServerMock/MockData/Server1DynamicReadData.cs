namespace OPCGateway.OPCServerMock.MockData;

public static class Server1DynamicReadData
{
    public static Dictionary<string, DynamicVariableParameters> GetOpcParametersMock()
    {
        return new Dictionary<string, DynamicVariableParameters>()
        {
            {
                "SomeDynamicNodeId",
                new DynamicVariableParameters(100, 300.0f, 1.0f, 0.2f)
            },
            {
                "SecondDynamicNodeId",
                new DynamicVariableParameters(100, 200.0f, 1.0f, 0.2f)
            },
        };
    }
}