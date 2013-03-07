using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CK.Core
{
    [AttributeUsage( AttributeTargets.Class, AllowMultiple = true, Inherited = false )]
    public class RemoveContextAttribute : Attribute, IContextDefiner
    {
        public RemoveContextAttribute( string context )
        {
            if( context == null ) throw new ArgumentNullException( "context" );
            Context = context;
        }

        public string Context { get; private set; }
    }
}
