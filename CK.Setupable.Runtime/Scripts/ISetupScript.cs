namespace CK.Setup
{
    /// <summary>
    /// A <see cref="ParsedFileName"/> associated to a way to read its content and a script source name.
    /// </summary>
    public interface ISetupScript
    {
        /// <summary>
        /// Gets the name of this script. Never null.
        /// </summary>
        ParsedFileName Name { get; }

        /// <summary>
        /// Gets the script itself. Never null.
        /// </summary>
        /// <returns>The script text.</returns>
        string GetScript();
    }
}
