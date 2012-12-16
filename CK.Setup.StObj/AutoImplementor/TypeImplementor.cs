using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.IO;
using System.Reflection.Emit;
using System.Threading;
using CK.Core;

namespace CK.Setup
{
    public class TypeImplementor
    {
        static int _typeID;
		static ModuleBuilder _moduleBuilder;

        /// <summary>
        /// This is the public key of the generated assembly.
        /// Whenever types created inside the dynamic assembly requires access to internal types of the calling assembly, this key can be used
        /// in the AssemblyInfo:  [assembly: InternalsVisibleTo( "CK.Setup.StObj.TypeImplementor.Assembly, PublicKey=..." )] attribute allows 
        /// the dynamic TypeImplementor assembly to reference and make use of internal types.
        /// </summary>
        static readonly public string DynamicPublicKey = "00240000048000009400000006020000002400005253413100040000010001009fbf2868f04bdf33df4c8c0517bb4c3d743b5b27fcd94009d42d6607446c1887a837e66545221788ecfff8786e85564c839ff56267fe1a3225cd9d8d9caa5aae3ba5d8f67f86ff9dbc5d66f16ba95bacde6d0e02f452fae20022edaea26d31e52870358d0dda69e592ea5cef609a054dac4dbbaa02edc32fb7652df9c0e8e9cd";

        static TypeImplementor()
		{
            AssemblyName assemblyName = new AssemblyName( "CK.Setup.StObj.TypeImplementor.Assembly" );
			assemblyName.Version = new Version( 1, 0, 0, 0 );
            StrongNameKeyPair kp;
            using( Stream stream = Assembly.GetAssembly( typeof( TypeImplementor ) ).GetManifestResourceStream( "CK.Setup.StObj.AutoImplementor.DynamicKeyPair.snk" ) )
            {
                byte[] result = new byte[stream.Length];
                stream.Read( result, 0, (int)stream.Length );
                kp = new StrongNameKeyPair( result );
            }
            assemblyName.KeyPair = kp;
           
			AssemblyBuilder assemblyBuilder = AppDomain.CurrentDomain.DefineDynamicAssembly( assemblyName, AssemblyBuilderAccess.RunAndSave );
            _moduleBuilder = assemblyBuilder.DefineDynamicModule( "TypeImplementorModule" );
		}

        readonly Type _baseType;
        readonly KeyValuePair<MethodInfo,IAutoImplementorMethod>[] _methodImplementors;

        public TypeImplementor( Type baseType, KeyValuePair<MethodInfo,IAutoImplementorMethod>[] methodImplementors )
        {
            if( baseType == null ) throw new ArgumentNullException( "baseType" );
            if( methodImplementors == null ) throw new ArgumentNullException( "methodImplementors" );
            _baseType = baseType;
            _methodImplementors = methodImplementors;
        }

        public virtual Type CreateType( IActivityLogger logger, bool isSealed = false )
        {
            TypeAttributes tA = TypeAttributes.Class | TypeAttributes.Public;
            if( isSealed ) tA |= TypeAttributes.Sealed;
            TypeBuilder b = _moduleBuilder.DefineType( _baseType.Name + Interlocked.Increment( ref _typeID ).ToString(), tA, _baseType );
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
                logger.Error( ex, "While implementing Type '{0}'.", _baseType.FullName );
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
