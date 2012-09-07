using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CK.Core;

namespace CK.Setup
{
    public class ScriptSource
    {
        readonly int _index;
        readonly string _name;
        readonly ScriptTypeHandler _handler;

        internal ScriptSource( ScriptTypeHandler h, string name )
        {
            _handler = h;
            _index = h.Sources.Count;
            _name = name;
        }

        internal ScriptTypeHandler Handler
        {
            get { return _handler; }
        }

        public int Index
        {
            get { return _index; }
        }

        public string Name
        {
            get { return _name; }
        }

    }
}
