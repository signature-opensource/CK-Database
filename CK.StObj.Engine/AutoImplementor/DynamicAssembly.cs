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
    public class DynamicAssembly : IDynamicAssembly
    {
        int _typeID;
        ModuleBuilder _moduleBuilder;

        /// <summary>
        /// This is the public key of the generated assembly.
        /// Whenever types created inside the dynamic assembly requires access to internal types of the calling assembly, this key can be used
        /// in the AssemblyInfo.
        /// <code>
        /// [assembly: InternalsVisibleTo( "CK.StObj.AutoAssembly, PublicKey=..." )] 
        /// [assembly: InternalsVisibleTo( "CK.StObj.AutoAssembly.Memory, PublicKey=..." )] 
        /// </code>
        /// These 2 attibutes allows the dynamic assembly to reference and make use of internal types.
        /// </summary>
        /// <remarks>
        /// Its value is: "00240000048000009400000006020000002400005253413100040000010001009fbf2868f04bdf33df4c8c0517bb4c3d743b5b27fcd94009d42d6607446c1887a837e66545221788ecfff8786e85564c839ff56267fe1a3225cd9d8d9caa5aae3ba5d8f67f86ff9dbc5d66f16ba95bacde6d0e02f452fae20022edaea26d31e52870358d0dda69e592ea5cef609a054dac4dbbaa02edc32fb7652df9c0e8e9cd"
        /// </remarks>
        static readonly public string DynamicPublicKey = "00240000048000009400000006020000002400005253413100040000010001009fbf2868f04bdf33df4c8c0517bb4c3d743b5b27fcd94009d42d6607446c1887a837e66545221788ecfff8786e85564c839ff56267fe1a3225cd9d8d9caa5aae3ba5d8f67f86ff9dbc5d66f16ba95bacde6d0e02f452fae20022edaea26d31e52870358d0dda69e592ea5cef609a054dac4dbbaa02edc32fb7652df9c0e8e9cd";
        
        /// <summary>
        /// This is the key used to signe the dynamic assembly.
        /// </summary>
        static readonly public StrongNameKeyPair DynamicKeyPair;
        
        /// <summary>
        /// Default assembly name.
        /// </summary>
        static readonly public string DefaultAssemblyName = "CK.StObj.AutoAssembly";

        static DynamicAssembly()
        {
            using( Stream stream = Assembly.GetAssembly( typeof( DynamicAssembly ) ).GetManifestResourceStream( "CK.Setup.AutoImplementor.DynamicKeyPair.snk" ) )
            {
                byte[] result = new byte[stream.Length];
                stream.Read( result, 0, (int)stream.Length );
                DynamicKeyPair = new StrongNameKeyPair( result );
            }
        }

        /// <summary>
        /// Initializes a new temporary <see cref="DynamicAssembly"/> with a name set to <see cref="DefaultAssemblyName"/>+".Memory" and 
        /// that can only <see cref="AssemblyBuilderAccess.Run"/>.
        /// </summary>
        public DynamicAssembly()
            : this( DefaultAssemblyName+".Memory", AssemblyBuilderAccess.Run )
        {
        }

        /// <summary>
        /// Initializes a new <see cref="DynamicAssembly"/> with the given name and access.
        /// </summary>
        /// <param name="assemblyName">Name to use. Can be <see cref="DefaultAssemblyName"/>.</param>
        /// <param name="access">Typical accesses are Run and RunAndSave (the default).</param>
        public DynamicAssembly( string assemblyName, AssemblyBuilderAccess access = AssemblyBuilderAccess.RunAndSave )
        {
            if( String.IsNullOrWhiteSpace( assemblyName ) ) throw new ArgumentException( "Name is invalid.", "assemblyName." );
            AssemblyName aName = new AssemblyName( assemblyName );
            aName.Version = new Version( 1, 0, 0, 0 );
            aName.KeyPair = DynamicKeyPair;

            AssemblyBuilder assemblyBuilder = AppDomain.CurrentDomain.DefineDynamicAssembly( aName, access );
            _moduleBuilder = assemblyBuilder.DefineDynamicModule( "TypeImplementorModule" );
        }

        /// <summary>
        /// Gets the <see cref="ModuleBuilder"/> for this <see cref="DynamicAssembly"/>.
        /// </summary>
        public ModuleBuilder ModuleBuilder
        {
            get { return _moduleBuilder; }
        }

        /// <summary>
        /// Provides a new unique number that can be used for generating unique names inside this dynamic assembly.
        /// </summary>
        /// <returns>A unique number.</returns>
        public string NextUniqueNumber()
        {
            return Interlocked.Increment( ref _typeID ).ToString();
        }

    }

}
