using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection.Emit;
using System.Reflection;
using System.Threading;
using System.IO;
using System.Collections;

namespace CK.Core
{
#if NET461
    
    /// <summary>
    /// Manages dynamic assembly creation with one <see cref="ModuleBuilder"/>.
    /// Resulting assembly can have a strong name and can be in memory and/or saved to disk.
    /// </summary>
    public class DynamicAssembly : DynamicAssemblyBase
    {
        readonly AssemblyBuilder _assemblyBuilder;

        /// <summary>
        /// This is the public key of the generated assembly.
        /// Whenever types created inside the dynamic assembly requires access to internal types of the calling assembly, this key can be used
        /// in the AssemblyInfo.
        /// <code>
        /// [assembly: InternalsVisibleTo( "CK.StObj.AutoAssembly, PublicKey=..." )] 
        /// [assembly: InternalsVisibleTo( "CK.StObj.AutoAssembly.Memory, PublicKey=..." )] 
        /// </code>
        /// These 2 attributes allows the dynamic assembly to reference and make use of internal types.
        /// </summary>
        /// <remarks>
        /// Its value is: "00240000048000009400000006020000002400005253413100040000010001009fbf2868f04bdf33df4c8c0517bb4c3d743b5b27fcd94009d42d6607446c1887a837e66545221788ecfff8786e85564c839ff56267fe1a3225cd9d8d9caa5aae3ba5d8f67f86ff9dbc5d66f16ba95bacde6d0e02f452fae20022edaea26d31e52870358d0dda69e592ea5cef609a054dac4dbbaa02edc32fb7652df9c0e8e9cd"
        /// </remarks>
        static readonly public string DynamicPublicKey = "00240000048000009400000006020000002400005253413100040000010001009fbf2868f04bdf33df4c8c0517bb4c3d743b5b27fcd94009d42d6607446c1887a837e66545221788ecfff8786e85564c839ff56267fe1a3225cd9d8d9caa5aae3ba5d8f67f86ff9dbc5d66f16ba95bacde6d0e02f452fae20022edaea26d31e52870358d0dda69e592ea5cef609a054dac4dbbaa02edc32fb7652df9c0e8e9cd";

        /// <summary>
        /// A default key that can be used to sign the dynamic assembly.
        /// </summary>
        static readonly public StrongNameKeyPair DynamicKeyPair;

        static DynamicAssembly()
        {
            using( Stream stream = Assembly.GetAssembly( typeof( DynamicAssembly ) ).GetManifestResourceStream("CK.StObj.Engine.AutoImplementor.DynamicKeyPair.snk") )
            {
                byte[] result = new byte[stream.Length];
                stream.Read( result, 0, (int)stream.Length );
                DynamicKeyPair = new StrongNameKeyPair( result );
            }
        }

        /// <summary>
        /// Initializes a new temporary <see cref="DynamicAssembly"/> with a name set to <see cref="BuilderFinalAssemblyConfiguration.DefaultAssemblyName"/>+".Memory" and 
        /// that can only <see cref="AssemblyBuilderAccess.Run"/>.
        /// </summary>
        public DynamicAssembly()
            : this( null, BuilderFinalAssemblyConfiguration.DefaultAssemblyName + ".Memory", DynamicKeyPair, AssemblyBuilderAccess.Run )
        {
        }

        /// <summary>
        /// Initializes a new <see cref="DynamicAssembly"/> with the given name and access.
        /// </summary>
        /// <param name="directory">Directory where the assembly must be saved. Must not be null if the assembly must be saved.</param>
        /// <param name="assemblyName">Name to use. If access has <see cref="AssemblyBuilderAccess.Save"/> bit set, the name of the dll will be with ".dll" suffix.</param>
        /// <param name="signature">Key pair to use to sign the dll.</param>
        /// <param name="access">Typical accesses are Run and RunAndSave (the default).</param>
        public DynamicAssembly( string directory, string assemblyName = BuilderFinalAssemblyConfiguration.DefaultAssemblyName, StrongNameKeyPair signature = null, AssemblyBuilderAccess access = AssemblyBuilderAccess.RunAndSave )
            : base( directory, assemblyName )
        {
            bool mustSave = (access & AssemblyBuilderAccess.Save) == AssemblyBuilderAccess.Save;

            // Default behavior of .Net DefineDynamicAssembly is to use the current directory (horrible).
            if( mustSave && directory == null ) throw new ArgumentNullException( "directory" );

            if( signature != null ) AssemblyName.KeyPair = signature;
            _assemblyBuilder = AppDomain.CurrentDomain.DefineDynamicAssembly(AssemblyName, access, directory);
            if ( mustSave )
            {
                ModuleBuilder = _assemblyBuilder.DefineDynamicModule(AssemblyName.Name, SaveFileName);
            }
            else ModuleBuilder = _assemblyBuilder.DefineDynamicModule(AssemblyName.Name);
        }

        /// <summary>
        /// Saves the dynamic assembly as a ".dll".
        /// This <see cref="DynamicAssembly"/> must have been constructed with an AssemblyBuilderAccess that has <see cref="AssemblyBuilderAccess.Save"/> bit set.
        /// </summary>
        public override void Save()
        {
            base.Save();
            _assemblyBuilder.Save( _assemblyBuilder.GetName().Name + ".dll" );
        }
    }
#else

    /// <summary>
    /// Manages dynamic assembly creation with one <see cref="ModuleBuilder"/>.
    /// </summary>
    public class DynamicAssembly : DynamicAssemblyBase
    {
        readonly AssemblyBuilder _assemblyBuilder;

        /// <summary>
        /// Initializes a new temporary <see cref="DynamicAssembly"/> with a name set to <see cref="BuilderFinalAssemblyConfiguration.DefaultAssemblyName"/>+".Memory" and 
        /// that can only <see cref="AssemblyBuilderAccess.Run"/>.
        /// </summary>
        public DynamicAssembly()
            : base( null, BuilderFinalAssemblyConfiguration.DefaultAssemblyName + ".Memory" )
        {
            _assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(AssemblyName, AssemblyBuilderAccess.Run);
            ModuleBuilder = _assemblyBuilder.DefineDynamicModule(AssemblyName.Name);
        }
    }

#endif
}
