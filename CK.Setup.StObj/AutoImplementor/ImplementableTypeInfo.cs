using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;

namespace CK.Core
{
    public class ImplementableTypeInfo
    {
        public readonly Type AbstractType;
        public readonly IReadOnlyList<PropertyInfo> PropertiesToImplement;
        public readonly IReadOnlyList<MethodInfo> MethodsToImplement;

        ImplementableTypeInfo( Type t, IReadOnlyList<PropertyInfo> p, IReadOnlyList<MethodInfo> m )
        {
            AbstractType = t;
            PropertiesToImplement = p;
            MethodsToImplement = m;
        }

        static internal ImplementableTypeInfo GetImplementableTypeInfo( IActivityLogger logger, Type abstractType, ICustomAttributeProvider attributeProvider )
        {
            if( logger == null ) throw new ArgumentNullException( "logger" );
            if( abstractType == null ) throw new ArgumentNullException( "abstractType" );
            if( !abstractType.IsAbstract ) throw new ArgumentException( "Type must be abstract.", "abstractType" );

            if( abstractType.IsDefined( typeof( PreventAutoImplementationAttribute ), false ) ) return null;

            var candidates = abstractType.GetMethods( BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public ).Where( m => !m.IsSpecialName && m.IsAbstract );
            int nbUncovered = 0;
            List<MethodInfo> members = new List<MethodInfo>();
            foreach( var m in candidates )
            {
                ++nbUncovered;
                bool isDefined = m.IsDefined( typeof( IAttributeAutoImplemented ), false );
                if( isDefined )
                {
                    --nbUncovered;
                    members.Add( m );
                }
            }
            List<PropertyInfo> pMembers = new List<PropertyInfo>();
            var pCandidates = abstractType.GetProperties( BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public ).Where( p => p.GetGetMethod().IsAbstract || p.GetSetMethod().IsAbstract );
            foreach( var p in pCandidates )
            {
                ++nbUncovered;
                if( !p.GetSetMethod().IsAbstract || !p.GetGetMethod().IsAbstract )
                {
                    logger.Error( "Property {0}.{1} is not a valid abstract property (both getter and setter must be abstract).", p.DeclaringType.FullName, p.Name );
                }
                else
                {
                    bool isDefined = p.IsDefined( typeof( IAttributeAutoImplemented ), false );
                    if( isDefined )
                    {
                        --nbUncovered;
                        pMembers.Add( p );
                    }
                }
            }
            if( nbUncovered > 0 ) return null;
            return new ImplementableTypeInfo( abstractType, pMembers.ToReadOnlyList(), members.ToReadOnlyList() );
        }

    }
}
