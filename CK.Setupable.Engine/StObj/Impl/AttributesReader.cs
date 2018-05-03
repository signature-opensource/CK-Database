using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CK.Core;
using System.Diagnostics;
using System.Reflection;

namespace CK.Setup
{
    internal class AttributesReader
    {
        static internal DependentItemGroupList GetGroups( IActivityMonitor monitor, Type t )
        {
            Debug.Assert( monitor != null );
            Debug.Assert( t != null );
            DependentItemGroupList result = new DependentItemGroupList();
            var all = (GroupsAttribute[])t.GetTypeInfo().GetCustomAttributes( typeof( GroupsAttribute ), false );
            foreach( var a in all )
            {
                result.AddCommaSeparatedString( a.Groups );
            }
            return result;
        }

        static internal DependentItemList GetRequirements( IActivityMonitor monitor, Type t, Type attrType )
        {
            Debug.Assert( monitor != null );
            Debug.Assert( t != null );
            Debug.Assert( attrType != null && typeof( RequiresAttribute ).IsAssignableFrom( attrType ) );
            DependentItemList result = new DependentItemList();
            var all = (RequiresAttribute[])t.GetTypeInfo().GetCustomAttributes( attrType, false );
            foreach( var a in all )
            {
                result.AddCommaSeparatedString( a.Requirements );
            }
            return result;
        }

        static internal SetupAttribute GetSetupAttribute( Type t )
        {
            return (SetupAttribute)t.GetTypeInfo().GetCustomAttributes( typeof( SetupAttribute ), false ).SingleOrDefault();
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
            var a = (VersionsAttribute)t.GetTypeInfo().GetCustomAttributes( typeof( VersionsAttribute ), false ).SingleOrDefault();
            return a != null ? a.VersionsString : null;
        }


    }
}
