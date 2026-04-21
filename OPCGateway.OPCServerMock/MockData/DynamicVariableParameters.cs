namespace OPCGateway.OPCServerMock.MockData;

public class DynamicVariableParameters(
    int changeIntervalRange,
    float value,
    float maxDelta,
    float valueIncrementRange)
{
    public int ChangeIntervalRange { get; set; } = changeIntervalRange;

    public float Value { get; set; } = value;

    public float MaxDelta { get; set; } = maxDelta;

    public float ValueIncrementRange { get; set; } = valueIncrementRange;
}