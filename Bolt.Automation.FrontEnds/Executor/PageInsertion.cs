namespace Bolt.Automation.FrontEnds.Executor
{
    /// <summary>
    /// A per-test page insertion: places <see cref="Page"/> immediately after <see cref="After"/>
    /// in the cloned flow. The registered flow definition is never modified.
    /// </summary>
    public record PageInsertion(Type Page, Type After);
}
