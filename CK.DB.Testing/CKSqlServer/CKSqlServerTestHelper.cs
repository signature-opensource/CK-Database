using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using CK.Core;
using CK.Text;

namespace CK.Testing
{

    /// <summary>
    /// Provides default implementation of <see cref="ICKSqlServerTestHelperCore"/>.
    /// </summary>
    public class CKSqlServerTestHelper : ICKSqlServerTestHelperCore
    {
        readonly ITestHelperConfiguration _config;
        readonly ISqlServerTestHelperCore _sqlServer;
        readonly IReadOnlyList<string> _usedSchemas;

        public CKSqlServerTestHelper( ITestHelperConfiguration config, ISqlServerTestHelperCore sqlServer )
        {
            _config = config;
            _sqlServer = sqlServer;
            var schemas = config.Get( "SqlServer/UsedSchemas", String.Empty )
                            .Split( new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries )
                            .Select( s => s.Trim() );
            if( schemas.Any() ) schemas = schemas.Append( "CK" ).Append( "CKCore" );
            _usedSchemas = schemas.Distinct().ToArray(); 
        }

        IReadOnlyList<string> ICKSqlServerTestHelperCore.UsedSchemas => _usedSchemas;

        /// <summary>
        /// Gets the <see cref="ICKSqlServerTestHelper"/> default implementation.
        /// </summary>
        public static ICKSqlServerTestHelper TestHelper => TestHelperResolver.Default.Resolve<ICKSqlServerTestHelper>();

    }
}
