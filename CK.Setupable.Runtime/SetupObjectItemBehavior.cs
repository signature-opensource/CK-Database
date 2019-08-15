namespace CK.Setup
{
    /// <summary>
    /// Qualifies the reference to a dynamic item.
    /// </summary>
    public enum SetupObjectItemBehavior
    {
        /// <summary>
        /// Defaults when nothing is specified.
        /// The item is defined: it must not be already defined.
        /// </summary>
        Define,

        /// <summary>
        /// Replaces the item.
        /// The item must be already defined.
        /// </summary>
        Replace,

        /// <summary>
        /// Transforms an already exisiting item.
        /// </summary>
        Transform
    }
}
