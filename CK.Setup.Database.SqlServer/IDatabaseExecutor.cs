using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using CK.Database.SqlServer;

namespace CK.Setup.Database.SqlServer
{
    public interface IDatabaseExecutor
    {
        SqlConnectionProvider Connection { get; }
        bool ExecuteScriptNoLog( string script );
        bool ExecuteScript( string script );
        bool ExecuteScript( params Action<TextWriter>[] writers );
    }

}
