using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CK.Core;

namespace CK.SqlServer
{
    public struct SourceLocation
    {
        public const string NoSource = "(no source)";

        public static readonly SourceLocation Empty = new SourceLocation() { SourceName = NoSource };

        public string SourceName;
        public int Index;

        public override int GetHashCode()
        {
            return Util.Hash.Combine( Util.Hash.StartValue, SourceName, Index ).GetHashCode();
        }

        public override bool Equals( object obj )
        {
            if( obj is SourceLocation )
            {
                SourceLocation other = (SourceLocation)obj;
                return Index == other.Index && SourceName == other.SourceName;
            }
            return false;
        }

        public override string ToString()
        {
            return String.Format( "{0} ({2})", SourceName, Index );
        }
    }
}
