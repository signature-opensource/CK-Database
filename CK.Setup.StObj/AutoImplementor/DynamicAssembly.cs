using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection.Emit;
using System.Reflection;
using System.Threading;
using System.IO;

namespace CK.Core
{
    /// <summary>
    /// Manages dynamic assembly creation with one <see cref="ModuleBuilder"/>.
    /// </summary>
    public class DynamicAssembly
    {
        int _typeID;
        ModuleBuilder _moduleBuilder;

        /// <summary>
        /// This is the public key of the generated assembly.
        /// Whenever types created inside the dynamic assembly requires access to internal types of the calling assembly, this key can be used
        /// in the AssemblyInfo:  [assembly: InternalsVisibleTo( "CK.Setup.StObj.TypeImplementor.Assembly, PublicKey=..." )] attribute allows 
        /// the dynamic TypeImplementor assembly to reference and make use of internal types.
        /// </summary>
        static readonly public string DynamicPublicKey = "00240000048000009400000006020000002400005253413100040000010001009fbf2868f04bdf33df4c8c0517bb4c3d743b5b27fcd94009d42d6607446c1887a837e66545221788ecfff8786e85564c839ff56267fe1a3225cd9d8d9caa5aae3ba5d8f67f86ff9dbc5d66f16ba95bacde6d0e02f452fae20022edaea26d31e52870358d0dda69e592ea5cef609a054dac4dbbaa02edc32fb7652df9c0e8e9cd";
        
        /// <summary>
        /// Default assembly name.
        /// </summary>
        static readonly public string DefaultAssemblyName = "CK.Setup.StObj.TypeImplementor.Assembly";

        /// <summary>
        /// Initializes a new <see cref="DynamicAssembly"/> with a name set to <see cref="DefaultAssemblyName"/> and that can only <see cref="AssemblyBuilderAccess.Run"/>.
        /// </summary>
        public DynamicAssembly()
            : this( DefaultAssemblyName, AssemblyBuilderAccess.Run )
        {
        }

        /// <summary>
        /// Initializes a new <see cref="DynamicAssembly"/> with the given name and access.
        /// </summary>
        /// <param name="assemblyName"></param>
        public DynamicAssembly( string assemblyName, AssemblyBuilderAccess access )
        {
            if( String.IsNullOrWhiteSpace( assemblyName ) ) throw new ArgumentException( "Name is invalid.", "assemblyName." );
            AssemblyName aName = new AssemblyName( assemblyName );
            aName.Version = new Version( 1, 0, 0, 0 );
            StrongNameKeyPair kp;
            using( Stream stream = Assembly.GetAssembly( typeof( DynamicAssembly ) ).GetManifestResourceStream( "CK.Setup.StObj.AutoImplementor.DynamicKeyPair.snk" ) )
            {
                byte[] result = new byte[stream.Length];
                stream.Read( result, 0, (int)stream.Length );
                kp = new StrongNameKeyPair( result );
            }
            aName.KeyPair = kp;

            AssemblyBuilder assemblyBuilder = AppDomain.CurrentDomain.DefineDynamicAssembly( aName, access );
            _moduleBuilder = assemblyBuilder.DefineDynamicModule( "TypeImplementorModule" );
        }

        /// <summary>
        /// Gets the <see cref="ModuleBuilder"/> to use for this <see cref="DynamicAssembly"/>.
        /// </summary>
        public ModuleBuilder ModuleBuilder
        {
            get { return _moduleBuilder; }
        }

        /// <summary>
        /// Provides a new unique number that can be used for generating unique namings inside this dynamic assembly.
        /// </summary>
        /// <returns>A unique number.</returns>
        public string NextUniqueNumber()
        {
            return Interlocked.Increment( ref _typeID ).ToString();
        }

        /// <summary>
        /// Implements a concrete (but fake) <see cref="Type"/> in a dynamic assembly and returns it.
        /// </summary>
        /// <param name="logger">Logger to use.</param>
        /// <returns>The newly created type in the dynamic assembly.</returns>
        public Type CreateStubType( IActivityLogger logger, ImplementableTypeInfo t )
        {
            TypeAttributes tA = TypeAttributes.Class | TypeAttributes.Public;
            TypeBuilder b = _moduleBuilder.DefineType( t.AbstractType.Name + NextUniqueNumber(), tA, t.AbstractType );
            foreach( var am in t.MethodsToImplement )
            {
                CK.Reflection.EmitHelper.ImplementEmptyStubMethod( b, am );
            }
            if( t.PropertiesToImplement.Count > 0 )
            {
                logger.Error( "Property auto implementation support is not yet implemented ('{0}').", t.AbstractType.FullName );
                return null;
            }
            try
            {
                return b.CreateType();
            }
            catch( Exception ex )
            {
                logger.Error( ex, "While implementing Stub for Type '{0}'.", t.AbstractType.FullName );
                return null;
            }
        }



    }

}
