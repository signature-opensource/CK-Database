using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace CK.Setup.Database.SqlServer
{
    class SqlServerObjectPreParsed : SqlObjectPreParse
    {
        string _type;

        public readonly Match Match;

        public string Type { get { return _type; } }

        public string Schema { get { return Match.Groups[2].Value; } }

        public string Name { get { return Match.Groups[3].Value; } }

        public string FullName { get { return Schema + '.' + Name; } }

        public SqlServerObjectPreParsed( string text, string header, Match mObj )
            : base( text, header )
        {
            Match = mObj;
            switch( char.ToUpperInvariant( Match.Groups[1].Value[0] ) )
            {
                case 'V': _type = "View"; break;
                case 'P': _type = "Procedure"; break;
                default: _type = "Function"; break;
            }
        }

        public override string ToString()
        {
            return Type + ' ' + Schema + '.' + Name;
        }
    }

}
