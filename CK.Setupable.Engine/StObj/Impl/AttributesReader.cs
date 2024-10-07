using System;
using System.Linq;
using CK.Core;
using System.Diagnostics;

namespace CK.Setup;

internal class AttributesReader
{
    static internal void CollectItemNames<TAttr>( Type t, Action<string> collector ) where TAttr : BaseItemNamesAttribute
    {
        Debug.Assert( t != null && collector != null );
        foreach( var n in t.GetCustomAttributesData()
                            .Where( d => d.AttributeType is TAttr )
                            .Select( d => (string)d.ConstructorArguments[0].Value ) )
        {
            collector( n );
        }
    }

    static internal SetupAttribute GetSetupAttribute( Type t )
    {
        return (SetupAttribute)t.GetCustomAttributes( typeof( SetupAttribute ), false ).SingleOrDefault();
    }

    static internal string GetFullName( IActivityMonitor monitor, bool warnWhenDefaultToTypeFullName, Type t, string alreadyNamed = null )
    {
        Debug.Assert( monitor != null );
        Debug.Assert( t != null );
        var all = (IAttributeSetupName[])t.GetCustomAttributes( typeof( IAttributeSetupName ), false );
        string name = alreadyNamed;
        foreach( var n in all )
        {
            if( name == null ) name = n.FullName;
            else if( n.FullName != null && String.CompareOrdinal( name, n.FullName ) != 0 )
            {
                monitor.Warn( $"FullName '{name}' is already associated to type '{t.FullName}'. Extraneous name '{n.FullName}' is ignored." );
            }
        }
        if( name == null )
        {
            name = t.FullName;
            if( warnWhenDefaultToTypeFullName )
            {
                monitor.Warn( $"Type '{t.FullName}' has no explicit associated Setup Name. Using the Type's full name." );
            }
        }
        return name;
    }

    static internal string GetVersionsString( Type t )
    {
        var a = (VersionsAttribute)t.GetCustomAttributes( typeof( VersionsAttribute ), false ).SingleOrDefault();
        return a != null ? a.VersionsString : null;
    }


}
