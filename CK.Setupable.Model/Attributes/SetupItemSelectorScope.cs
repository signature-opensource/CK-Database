

namespace CK.Core
{
    /// <summary>
    /// Defines scope for <see cref="Setup.SetupItemSelectorBaseAttribute"/>.
    /// </summary>
    public enum SetupItemSelectorScope
    {
        /// <summary>
        /// No scope defined.
        /// </summary>
        None,
        /// <summary>
        /// All setup items.
        /// </summary>
        All,
        /// <summary>
        /// Only direct chilren.
        /// </summary>
        DirectChildren,
        /// <summary>
        /// All children.
        /// </summary>
        Children
    }
}
