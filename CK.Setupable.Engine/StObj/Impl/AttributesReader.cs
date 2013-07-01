using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CK.Core;
using System.Diagnostics;

namespace CK.Setup
{
    internal class AttributesReader
    {
        static internal DependentItemGroupList GetGroups( IActivityLogger logger, Type t )
        {
            Debug.Assert( logger != null );
            Debug.Assert( t != null );
            DependentItemGroupList result = new DependentItemGroupList();
            var all = (GroupsAttribute[])t.GetCustomAttributes( typeof( GroupsAttribute ), false );
            foreach( var a in all )
            {
                result.AddCommaSeparatedString( a.Groups );
            }
            return result;
        }

        static internal DependentItemList GetRequirements( IActivityLogger logger, Type t, Type attrType )
        {
            Debug.Assert( logger != null );
            Debug.Assert( t != null );
            Debug.Assert( attrType != null && typeof( RequiresAttribute ).IsAssignableFrom( attrType ) );
            DependentItemList result = new DependentItemList();
            var all = (RequiresAttribute[])t.GetCustomAttributes( attrType, false );
            foreach( var a in all )
            {
                result.AddCommaSeparatedString( a.Requirements );
            }
            return result;
        }

        static internal SetupAttribute GetSetupAttribute( Type t )
        {
            return (SetupAttribute)t.GetCustomAttributes( typeof( SetupAttribute ), false ).SingleOrDefault();
        }

        static internal string GetFullName( IActivityLogger logger, bool warnWhenDefaultToTypeFullName, Type t, string alreadyNamed = null )
        {
            Debug.Assert( logger != null );
            Debug.Assert( t != null );
            var all = (IAttributeSetupName[])t.GetCustomAttributes( typeof( IAttributeSetupName ), false );
            string name = alreadyNamed;
            foreach( var n in all )
            {
                if( name == null ) name = n.FullName;
                else if( n.FullName != null && String.CompareOrdinal( name, n.FullName ) != 0 ) logger.Warn( "FullName '{0}' is already associated to type '{1}'. Extraneous name '{2}' is ignored.", name, t.FullName, n.FullName );
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

        static internal string GetVersionsString( Type t )
        {
            var a = (VersionsAttribute)t.GetCustomAttributes( typeof( VersionsAttribute ), false ).SingleOrDefault();
            return a != null ? a.VersionsString : null;
        }


    }
}
