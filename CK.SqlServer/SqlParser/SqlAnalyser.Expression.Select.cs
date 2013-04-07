using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Text;
using CK.SqlServer;

namespace CK.SqlServer
{
        public partial class SqlAnalyser
        {
            bool IsSelect( out SqlExprSelectSpec e )
            {
                e = null;
                return false;
            }


        }

    
}

