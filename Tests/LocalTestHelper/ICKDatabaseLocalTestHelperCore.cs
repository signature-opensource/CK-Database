using System;
using System.Collections.Generic;
using CK.Testing;
using CK.Text;

namespace CK.Testing.CKDatabaseLocal
{
    /// <summary>
    /// Helper local to CK-Database solution.
    /// </summary>
    public interface ICKDatabaseLocalTestHelperCore
    {
        /// <summary>
        /// Gets the bin paths in net461 and netstandard2.0 (or netcoreapp2.0) for StObj, Setupable
        /// and SqlServer.Setup.
        /// </summary>
        IEnumerable<NormalizedPath> CKDatabaseComponentsPaths { get; }

        /// <summary>
        /// Gets the bin paths in net461 and netstandard2.0 (or netcoreapp2.0) of SqlActorPackage
        /// and SqlActorPackage.Runtime.
        /// </summary>
        IEnumerable<NormalizedPath> SqlActorPackageComponentsPaths { get; }

        /// <summary>
        /// Gets the bin paths in net461 and netstandard2.0 (or netcoreapp2.0) of SqlActorPackage,
        /// SqlActorPackage.Runtime, SqlZonePackage and SqlZonePackage.Runtime.
        /// </summary>
        IEnumerable<NormalizedPath> SqlZonePackageComponentsPaths { get; }

        /// <summary>
        /// Gets <see cref="CKDatabaseComponentsPaths"/> plus <see cref="SqlZonePackageComponentsPaths"/>
        /// (with the runtimes of SqlActorPackage and SqlZonePackage).
        /// </summary>
        IEnumerable<NormalizedPath> AllLocalComponentsPaths { get; }

        /// <summary>
        /// Deletes <see cref="AllLocalComponentsPaths"/>/publish folders (only the ones in netcoreapp2.0).
        /// </summary>
        void DeleteAllLocalComponentsPublishedFolders();

    }
}
