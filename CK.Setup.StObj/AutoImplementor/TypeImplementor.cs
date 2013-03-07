using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.IO;
using System.Reflection.Emit;
using System.Threading;

namespace CK.Core
{
    public class TypeImplementor
    {

        static int _typeID;
		static ModuleBuilder _moduleBuilder;

        static TypeImplementor()
		{
            AssemblyName assemblyName = new AssemblyName( "TypeImplementorModule" );
			assemblyName.Version = new Version( 1, 0, 0, 0 );
           
			AssemblyBuilder assemblyBuilder = AppDomain.CurrentDomain.DefineDynamicAssembly( assemblyName, AssemblyBuilderAccess.RunAndSave );
            _moduleBuilder = assemblyBuilder.DefineDynamicModule( "TypeImplementorModule" );
		}

        readonly Type _abstractType;
        readonly KeyValuePair<MethodInfo,IAutoImplementorMethod>[] _methodImplementors;

        public TypeImplementor( Type abstractType, KeyValuePair<MethodInfo,IAutoImplementorMethod>[] methodImplementors )
        {
            if( abstractType == null ) throw new ArgumentNullException( "abstractType" );
            if( methodImplementors == null ) throw new ArgumentNullException( "methodImplementors" );
            if( !abstractType.IsAbstract ) throw new ArgumentException( "Type must be abstract.", "abstractType" );
            _abstractType = abstractType;
            _methodImplementors = methodImplementors;
        }

        public virtual Type CreateType( IActivityLogger logger )
        {
            TypeBuilder b = _moduleBuilder.DefineType( _abstractType.Name + Interlocked.Increment( ref _typeID ).ToString(), TypeAttributes.Class | TypeAttributes.Public | TypeAttributes.Sealed, _abstractType );
            foreach( var am in _methodImplementors )
            {
                try
                {
                    if( !am.Value.Implement( logger, am.Key, b ) )
                        return null;
                }
                catch( Exception ex )
                {
                    logger.Error( ex, "While implementing method '{0}.{1}'.", am.Key.DeclaringType.FullName, am.Key.Name );
                    return null;
                }
            }
            try
            {
                return b.CreateType();
            }
            catch( Exception ex )
            {
                logger.Error( ex, "While implementing Type '{0}'.", _abstractType.FullName );
                return null;
            }
        }

        static public KeyValuePair<MethodInfo, IAutoImplementorMethod>[] GetAutoImplementMethodsFromAttributes( IActivityLogger logger, Type abstractType )
        {
            if( logger == null ) throw new ArgumentNullException( "logger" );
            if( abstractType == null ) throw new ArgumentNullException( "abstractType" );
            if( !abstractType.IsAbstract ) throw new ArgumentException( "Type must be abstract.", "abstractType" );

            var candidates = abstractType.GetMethods( BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public ).Where( m => !m.IsSpecialName && m.IsAbstract );
            int nbUncovered = 0;
            List<KeyValuePair<MethodInfo,IAutoImplementorMethod>> implMap = new List<KeyValuePair<MethodInfo, IAutoImplementorMethod>>();
            foreach( var m in candidates )
            {
                ++nbUncovered;
                var c = (IAutoImplementorMethod[])m.GetCustomAttributes( typeof( IAutoImplementorMethod ), false );
                if( c.Length > 0 )
                {
                    --nbUncovered;
                    if( c.Length > 1 )
                    {
                        logger.Error( "Multiple attributes are IAutoImplementorMethod ('{0}'). Only one can be defined.", String.Join( "', '", c.Select( a => a.ToString() ) ) );
                        return null;
                    }
                    else implMap.Add( new KeyValuePair<MethodInfo, IAutoImplementorMethod>( m, c[0] ) );
                }
            }
            if( nbUncovered > 0 ) return null;
            return implMap.ToArray();
        }

    }
}
