using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CK.Core;
using System.Diagnostics;
using System.Reflection;

namespace CK.Setup
{
    /// <summary>
    /// Base class for construct parameters or ambient properties: these references can be resolved
    /// either structurally or dynamically (by <see cref="IStObjValueResolver"/>).
    /// </summary>
    internal abstract class MutableReferenceWithValue : MutableReferenceOptional
    {
        internal MutableReferenceWithValue( MutableItem owner, StObjMutableReferenceKind kind )
            : base( owner, kind )
        {
            Value = Type.Missing;
        }

        public object Value { get; protected set; }

        internal bool HasBeenSet { get { return Value != Type.Missing; } }
    }
}
