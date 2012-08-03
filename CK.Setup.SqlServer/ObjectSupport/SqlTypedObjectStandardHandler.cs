//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using CK.Core;

//namespace CK.Setup.SqlServer
//{
//    internal class SqlTypedObjectStandardHandler : TypedObjectHandler
//    {
//        public override bool Register( IActivityLogger logger, IReadOnlyList<Type> pathTypes, ITypedObjectRegisterer registerer )
//        {
//            if( logger == null ) throw new ArgumentNullException( "logger" );
//            if( pathTypes == null ) throw new ArgumentNullException( "pathTypes" );
//            if( registerer == null ) throw new ArgumentNullException( "registerer" );

//            Type baseClass = pathTypes[0];
//            if( typeof( SqlPackageType ).IsAssignableFrom( baseClass ) )
//            {
//                var a = GetRequiredAttribute<SqlPackageAttribute>( logger, baseClass );
//                if( a != null )
//                {
//                    var item = new SqlPackageTypeItem( baseClass, a );
//                    registerer.Register( item );
//                    for( int i = 1; i < pathTypes.Count; ++i )
//                    {
//                        var ao = GetRequiredAttribute<SqlPackageAttribute>( logger, pathTypes[i] );
//                        if( ao != null )
//                        {
//                            item = new SqlPackageTypeItem( pathTypes[i], ao, item );
//                            registerer.Register( item );
//                        }
//                    }
//                }
//                return true;
//            }
//            if( typeof( SqlTableType ).IsAssignableFrom( baseClass ) )
//            {
//                var a = GetRequiredAttribute<SqlTableAttribute>( logger, baseClass );
//                if( a != null )
//                {
//                    var item = new SqlTableTypeItem( baseClass, a );
//                    registerer.Register( item );
//                    for( int i = 1; i < pathTypes.Count; ++i )
//                    {
//                        var ao = GetRequiredAttribute<SqlTableOverrideAttribute>( logger, pathTypes[i] );
//                        if( ao != null )
//                        {
//                            item = new SqlTableTypeItem( pathTypes[i], ao, item );
//                            registerer.Register( item );
//                        }
//                    }
//                }
//                return true;
//            }

//            return false;
//        }

//        private static T GetRequiredAttribute<T>( IActivityLogger logger, Type baseClass ) where T : class
//        {
//            object[] attributes = baseClass.GetCustomAttributes( typeof( T ), false );
//            if( attributes == null || attributes.Length == 0 )
//            {
//                logger.Fatal( "Type {0} must be marked with {1}.", baseClass, typeof( T ).Name );
//                return null;
//            }
//            return (T)attributes[0];
//        }
//    }
//}
