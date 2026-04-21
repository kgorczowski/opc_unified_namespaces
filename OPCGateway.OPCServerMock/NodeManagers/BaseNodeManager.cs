using System.Collections.Concurrent;
using Opc.Ua;
using Opc.Ua.Server;
using OPCGateway.OPCServerMock.MockData;

namespace OPCGateway.OPCServerMock.NodeManagers;

public abstract class BaseNodeManager : CustomNodeManager2
{
    private readonly Random _random;

    private readonly ConcurrentDictionary<int, ConcurrentDictionary<DataItemState, DynamicVariableParameters>>
        _dynamicNodes = new();

    public BaseNodeManager(
        IServerInternal server,
        ApplicationConfiguration configuration)
        : base(
            server,
            configuration,
            "http://yourcompany.com/UA/Data/")
    {
        _random = new Random();

        for (var i = 1; i <= 5; i++)
        {
            _dynamicNodes.TryAdd(i, new ConcurrentDictionary<DataItemState, DynamicVariableParameters>());
        }
    }

    public override NodeId New(ISystemContext context, NodeState node)
    {
        var instance = node as BaseInstanceState;
        if (instance != null && instance.Parent != null)
        {
            var id = instance.Parent.NodeId.Identifier as string;
            if (id != null)
            {
                return new NodeId(id + "_" + instance.SymbolicName, instance.Parent.NodeId.NamespaceIndex);
            }
        }

        return node.NodeId;
    }

    public override void CreateAddressSpace(IDictionary<NodeId, IList<IReference>> externalReferences)
    {
        lock (Lock)
        {
            IList<IReference>? references;
            if (!externalReferences.TryGetValue(ObjectIds.ObjectsFolder, out references))
            {
                externalReferences[ObjectIds.ObjectsFolder] = references = new List<IReference>();
            }

            var root = CreateFolder();
            root.AddReference(ReferenceTypes.Organizes, true, ObjectIds.ObjectsFolder);
            references.Add(new NodeStateReference(ReferenceTypes.Organizes, false, root.NodeId));
            root.EventNotifier = EventNotifiers.SubscribeToEvents;
            AddRootNotifier(root);
            try
            {
                var dynamicValues = ShuffleDictionary(GetMockedDynamicValuesDictionary());
                var nonDynamicValues = GetMockedNonDynamicValuesDictionary();
                if (dynamicValues.Count > 0)
                {
                    foreach (var kvp in dynamicValues)
                    {
                        CreateDynamicVariable(root, kvp.Key, BuiltInType.Float, kvp.Value);
                    }
                }

                if (nonDynamicValues.Count > 0)
                {
                    foreach (var kvp in nonDynamicValues)
                    {
                        var name = kvp.Key;
                        var value = kvp.Value;
                        var dataType = GetType(value);
                        CreateNonDynamicVariable(root, name, dataType, value);
                    }
                }
            }
            catch (Exception e)
            {
                Utils.LogError(e, "Error creating the ReferenceNodeManager address space.");
            }

            AddPredefinedNode(SystemContext, root);
        }
    }

    protected FolderState CreateFolder(NodeState? parent, string path, string name)
    {
        var folder = new FolderState(parent)
        {
            SymbolicName = name, ReferenceTypeId = ReferenceTypes.Organizes,
            TypeDefinitionId = ObjectTypeIds.FolderType, NodeId = new NodeId(path, NamespaceIndex),
            BrowseName = new QualifiedName(path, NamespaceIndex), DisplayName = new LocalizedText("en", name),
            WriteMask = AttributeWriteMask.None, UserWriteMask = AttributeWriteMask.None,
            EventNotifier = EventNotifiers.None,
        };
        if (parent != null)
        {
            parent.AddChild(folder);
        }

        return folder;
    }

    protected abstract FolderState CreateFolder();

    protected abstract Dictionary<string, DynamicVariableParameters> GetMockedDynamicValuesDictionary();

    protected abstract Dictionary<string, object> GetMockedNonDynamicValuesDictionary();

    private DataItemState CreateNonDynamicVariable(
        NodeState parent,
        string name,
        BuiltInType dataType,
        object initialValue)
    {
        var variable = new DataItemState(parent);
        variable.Create(SystemContext, null, variable.BrowseName, null, true);
        variable.NodeId = new NodeId(name, NamespaceIndex);
        variable.BrowseName = new QualifiedName(name, NamespaceIndex);
        variable.DisplayName = new LocalizedText("en", name);
        variable.DataType = (uint)dataType;
        variable.AccessLevel = AccessLevels.CurrentReadOrWrite;
        variable.UserAccessLevel = AccessLevels.CurrentReadOrWrite;
        variable.Value = initialValue;
        variable.StatusCode = StatusCodes.Good;
        variable.Timestamp = DateTime.UtcNow;
        parent.AddChild(variable);
        return variable;
    }

    private DataItemState CreateDynamicVariable(
        NodeState parent,
        string name,
        BuiltInType dataType,
        DynamicVariableParameters variableParameters)
    {
        var variable = new DataItemState(parent);
        variable.Create(SystemContext, null, variable.BrowseName, null, true);
        variable.NodeId = new NodeId(name, NamespaceIndex);
        variable.BrowseName = new QualifiedName(name, NamespaceIndex);
        variable.DisplayName = new LocalizedText("en", name);
        variable.DataType = (uint)dataType;
        variable.AccessLevel = AccessLevels.CurrentReadOrWrite;
        variable.UserAccessLevel = AccessLevels.CurrentReadOrWrite;
        variable.Value = variableParameters.Value;
        variable.StatusCode = StatusCodes.Good;
        variable.Timestamp = DateTime.UtcNow;
        parent.AddChild(
            variable);

        var randomNodeDictionary = _random.Next(1, 6);
        _dynamicNodes[randomNodeDictionary].TryAdd(variable, variableParameters);

        foreach (var dynamicNode in _dynamicNodes)
        {
            if (dynamicNode.Value.Count == 1)
            {
                _ = UpdateAllVariablesAsync(dynamicNode.Value);
            }
        }

        return variable;
    }

    private async Task UpdateAllVariablesAsync(
        ConcurrentDictionary<DataItemState, DynamicVariableParameters> dynamicNode)
    {
        while (true)
        {
            foreach (var (variable, parameters) in dynamicNode)
            {
                var currentValue = (float)variable.Value;
                var minValue = parameters.Value - parameters.MaxDelta;
                var maxValue = parameters.Value + parameters.MaxDelta;
                var delta = (float)(_random.NextDouble() * parameters.ValueIncrementRange * 2) -
                            parameters.ValueIncrementRange;
                currentValue = Math.Clamp(currentValue + delta, minValue, maxValue);
                variable.Value = currentValue;
                variable.Timestamp = DateTime.UtcNow;
                variable.ClearChangeMasks(SystemContext, true);
            }

            await Task.Delay(TimeSpan.FromMilliseconds(_random.Next(50, 100)));
        }
    }

    private BuiltInType GetType(object value)
    {
        return value switch
        {
            bool => BuiltInType.Boolean,
            short => BuiltInType.Int16,
            int => BuiltInType.Int32,
            float => BuiltInType.Float,
            _ => BuiltInType.String,
        };
    }

    private Dictionary<TKey, TValue> ShuffleDictionary<TKey, TValue>(Dictionary<TKey, TValue> dictionary)
        where TKey : notnull
    {
        if (dictionary.Count == 0)
        {
            return dictionary;
        }

        var list = dictionary.ToList();
        var rng = new Random();

        for (var i = list.Count - 1; i > 0; i--)
        {
            var randomIndex = rng.Next(0, i + 1);
            (list[randomIndex], list[i]) = (list[i], list[randomIndex]);
        }

        return list.ToDictionary(x => x.Key, x => x.Value);
    }
}