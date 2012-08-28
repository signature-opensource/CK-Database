using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CK.Core;

namespace CK.Setup
{
    [AttributeUsage( AttributeTargets.Class, AllowMultiple = false, Inherited = false )]
    public class SetupNameAttribute : Attribute, ISetupNameAttribute
    {
        readonly string _name;

        public SetupNameAttribute( string fullName )
        {
            _name = fullName;
        }

        public string FullName
        {
            get { return _name; }
        }

        static public string GetFullName( IActivityLogger logger, bool warnWhenDefaultToTypeFullName, Type t, string alreadyNamed = null )
        {
            if( logger != null ) throw new ArgumentNullException( "logger" );
            if( t == null ) throw new ArgumentNullException( "t" );
            var all = (ISetupNameAttribute[])t.GetCustomAttributes( typeof( ISetupNameAttribute ), false );
            string name = alreadyNamed;
            foreach( var n in all )
            {
                if( name == null ) name = n.FullName;
                else if( n.FullName != null && String.CompareOrdinal( name, n.FullName ) != 0 ) logger.Error( "FullName '{0}' is already associated to type '{1}'. Extraneous name '{2}' is ignored.", name, t.FullName, n.FullName );
            }
            if( name == null )
            {
                name = t.FullName;
                if( warnWhenDefaultToTypeFullName )
                {
                    logger.Warn( "Type '{0}' has no explicit associated Setup Name. Using the Type's full name.", t.FullName );
                }
            }
            return name;
        }

    }
}
