namespace CK.Setup
{
    /// <summary>
    /// A setup item that is bound to a StObj.
    /// </summary>
    public interface IStObjSetupItem : IMutableSetupItem, ISetupObjectItem
    {
        /// <summary>
        /// Gets the StObj. Null if this item is directly bound to an object.
        /// </summary>
        IStObjResult StObj { get; }

    }
}
