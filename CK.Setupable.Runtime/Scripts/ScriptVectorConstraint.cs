using System;

namespace CK.Setup;

/// <summary>
/// Defines the constraints that a <see cref="ScriptVector"/> may satisfy.
/// </summary>
[Flags]
public enum ScriptVectortConstraint
{
    /// <summary>
    /// No requirements.
    /// </summary>
    None = 0,

    /// <summary>
    /// The "no version" script must exist ('no version" script is always applied last).
    /// </summary>
    NoVersionIsRequired = 1,

    /// <summary>
    /// A script for the current version is required.
    /// </summary>
    CurrentVersionIsRequired = 2,

    /// <summary>
    /// The migration path must have no holes.
    /// </summary>
    UpgradeVersionPathIsComplete = 4
}
