namespace BMDRM.MemberList.State;

public static class NodeStateTypeExtensions
{
    /// <summary>
    /// Returns a string representation of the node state for metrics purposes.
    /// </summary>
    public static string MetricsString(this NodeStateType state)
    {
        return state switch
        {
            NodeStateType.Alive => "alive",
            NodeStateType.Dead => "dead",
            NodeStateType.Suspect => "suspect",
            NodeStateType.Left => "left",
            _ => $"unhandled-value-{(int)state}"
        };
    }
}