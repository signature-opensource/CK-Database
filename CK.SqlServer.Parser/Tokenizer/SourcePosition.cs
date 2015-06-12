using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CK.SqlServer.Parser
{
    public struct SourcePosition
    {
        public readonly int Line;
        public readonly int Column;

        public SourcePosition( int line, int column )
        {
            Line = line;
            Column = column;
        }

        public override string ToString()
        {
            return '@' + Line.ToString( CultureInfo.InvariantCulture ) + ',' + Column.ToString( CultureInfo.InvariantCulture );
        }
    }
}
