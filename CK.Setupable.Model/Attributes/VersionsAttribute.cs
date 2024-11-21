using System;

namespace CK.Core;

/// <summary>
/// Defines versions of an object.
/// </summary>
[AttributeUsage( AttributeTargets.Class, AllowMultiple = false, Inherited = false )]
public class VersionsAttribute : Attribute
{
    readonly string _versions;

    /// <summary>
    /// Describes the list of available versions and optional associated previous full names with a string like: "1.2.4, Initial.Name = 1.3.1, A.New.Name=1.4.1, 1.5.0"
    /// The last version must NOT define a previous name since the last version is the current one (an <see cref="ArgumentException"/> will be thrown).
    /// <para>
    /// In the example above, the current version is 1.5.0 (and the object's name is what it is currently), the previous version was 1.4.1 and the object's name was A.NewName.
    /// And the initial name, during the very first versions (from 1.2.4 to 1.3.1 included), was Initial.Name.
    /// </para>
    /// </summary>
    /// <param name="versionsAndPreviousNames">String like "1.2.4, Previous.Name = 1.3.1, A.New.Name=1.4.1, 1.5.0".</param>
    public VersionsAttribute( string versionsAndPreviousNames )
    {
        _versions = versionsAndPreviousNames;
    }

    /// <summary>
    /// Gets a string like "1.2.4, Initial.Name = 1.3.1, A.New.Name=1.4.1, 1.5.0".
    /// </summary>
    public string VersionsString => _versions;

}
