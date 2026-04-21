namespace OPCGateway.OPCServerMock.MockData;

public static class WriteData
{
    public static Dictionary<string, object> GetOpcParametersMock()
    {
#pragma warning disable SA1509 // Opening braces should not be preceded by blank line
        return new Dictionary<string, object>()
        {
            { "SomeWriteNodeId", 0.2f },

            { "SecondWriteNodeId", 23 },
        };
    }
}