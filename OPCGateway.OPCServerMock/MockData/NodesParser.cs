namespace OPCGateway.OPCServerMock.MockData;

public static class NodesParser
{
    private static readonly Dictionary<string, DynamicVariableParameters> _dynamicNodeList = [];
    private static readonly Dictionary<string, object> _staticNodeList = [];

    public static Dictionary<string, DynamicVariableParameters> GetCreatedDynamicNodes()
    {
        return _dynamicNodeList;
    }

    public static Dictionary<string, object> GetCreatedStaticNodes()
    {
        return _staticNodeList;
    }

    public static void CreateNodesDictionaries(
        Dictionary<string, DynamicVariableParameters> dynamicTemplateValues,
        Dictionary<string, object> staticTemplateValues)
    {
        foreach (var kvp in dynamicTemplateValues)
        {
            _dynamicNodeList[kvp.Key] = kvp.Value;
        }

        foreach (var kvp in staticTemplateValues)
        {
            _staticNodeList[kvp.Key] = kvp.Value;
        }
    }
}
