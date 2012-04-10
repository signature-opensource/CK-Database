using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using CK.Core;

namespace CK.Setup.Database
{
    public abstract class SqlObjectPreParse
    {
        string _text;
        string _header;

        protected SqlObjectPreParse( string text, string header )
        {
            if( text == null ) throw new ArgumentNullException( "text" );
            _text = text;
            _header = header;
        }

        public string Header { get { return _header; } }

        public string Text { get { return _text; } }
        
    }

}
