using CK.Core;
using CK.Text;
using System;

namespace CK.Testing
{
    /// <summary>
    /// Provides basic tests information.
    /// </summary>
    public interface IBasicTestHelper : ITestHelper
    {
        /// <summary>
        /// Gets the build configuration ("Debug" or "Release").
        /// </summary>
        string BuildConfiguration { get;}

        /// <summary>
        /// Gets the path to the root folder: where the .git folder is.
        /// </summary>
        NormalizedPath RepositoryFolder { get; }

        /// <summary>
        /// Gets the solution folder. It is the parent directory of the 'Tests/' folder (that must exist).
        /// </summary>
        NormalizedPath SolutionFolder { get; }

        /// <summary>
        /// Gets the path to the log folder. It is the 'Tests/Logs' folder of the solution. 
        /// </summary>
        NormalizedPath LogFolder { get; }

        /// <summary>
        /// Gets the path to the test project folder.
        /// This is usually where files and folders specific to the test should be (like a
        /// "TestScripts" folder).
        /// </summary>
        NormalizedPath TestProjectFolder { get; }

        /// <summary>
        /// Gets the bin folder where the tests are beeing executed.
        /// This normally is the same as <see cref="AppContext.BaseDirectory"/>.
        /// </summary>
        NormalizedPath BinFolder { get; }

    }
}
