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
    /// <summary>
    /// Manages dynamic assembly creation with one <see cref="ModuleBuilder"/>.
    /// Resulting assembly can have a strong name and can be in memory and/or saved to disk.
    /// </summary>
    public class DynamicAssembly : IDynamicAssembly
    {
        int _typeID;
        readonly ModuleBuilder _moduleBuilder;
        readonly AssemblyBuilder _assemblyBuilder;
        readonly IDictionary _memory;
        readonly List<Action<IDynamicAssembly>> _postActions;
        readonly string _saveFileName;
        readonly string _saveFilePath;

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
            using( Stream stream = Assembly.GetAssembly( typeof( DynamicAssembly ) ).GetManifestResourceStream( "CK.Setup.AutoImplementor.DynamicKeyPair.snk" ) )
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
            : this( null, BuilderFinalAssemblyConfiguration.DefaultAssemblyName + ".Memory", null, null, AssemblyBuilderAccess.Run )
        {
        }

        /// <summary>
        /// Initializes a new <see cref="DynamicAssembly"/> with the given name and access.
        /// </summary>
        /// <param name="directory">Directory where the assembly must be saved. Must not be null if the assembly must be saved.</param>
        /// <param name="assemblyName">Name to use. If access has <see cref="AssemblyBuilderAccess.Save"/> bit set, the name of the dll will be with ".dll" suffix.</param>
        /// <param name="externalVersionStamp">Embedded stamp. Used to detect the need to rebuild the assembly.</param>
        /// <param name="signature">Key pair to use to sign the dll.</param>
        /// <param name="access">Typical accesses are Run and RunAndSave (the default).</param>
        public DynamicAssembly( string directory, string assemblyName = BuilderFinalAssemblyConfiguration.DefaultAssemblyName, string externalVersionStamp = null, StrongNameKeyPair signature = null, AssemblyBuilderAccess access = AssemblyBuilderAccess.RunAndSave )
        {
            bool mustSave = (access & AssemblyBuilderAccess.Save) == AssemblyBuilderAccess.Save;

            // Default behavior of .Net DefineDynamicAssembly is to use the current directory (horrible).
            if( mustSave && directory == null ) throw new ArgumentNullException( "directory" );
            if( String.IsNullOrWhiteSpace( assemblyName ) ) throw new ArgumentException( "Name is invalid.", "assemblyName." );

            AssemblyName aName = new AssemblyName( assemblyName );
            aName.Version = new Version( 1, 0, 0, 0 );
            if( signature != null ) aName.KeyPair = signature;
            _assemblyBuilder = AppDomain.CurrentDomain.DefineDynamicAssembly( aName, access, directory );
            if( externalVersionStamp != null )
            {
                var ctor = typeof(AssemblyInformationalVersionAttribute).GetConstructor( new Type[] { typeof( string ) } );
                CustomAttributeBuilder attr = new CustomAttributeBuilder( ctor, new object[] { externalVersionStamp } );
                _assemblyBuilder.SetCustomAttribute( attr );
            }
            if( mustSave )
            {
                _saveFileName = aName.Name + ".dll";
                _moduleBuilder = _assemblyBuilder.DefineDynamicModule( aName.Name, _saveFileName );
                _saveFilePath = Path.Combine( directory, _saveFileName );
            }
            else _moduleBuilder = _assemblyBuilder.DefineDynamicModule( aName.Name );
            _memory = new Hashtable();
            _postActions = new List<Action<IDynamicAssembly>>();
        }

        /// <summary>
        /// Gets the name of the dll (ends with '.dll') if it must be eventually saved, otherwise null.
        /// </summary>
        public string SaveFileName => _saveFileName; 

        /// <summary>
        /// Gets the full path of the dll if it must be eventually saved, otherwise null.
        /// </summary>
        public string SaveFilePath => _saveFilePath; 
            
        /// <summary>
        /// Gets the <see cref="ModuleBuilder"/> for this <see cref="DynamicAssembly"/>.
        /// </summary>
        public ModuleBuilder ModuleBuilder => _moduleBuilder; 

        /// <summary>
        /// Provides a new unique number that can be used for generating unique names inside this dynamic assembly.
        /// </summary>
        /// <returns>A unique number.</returns>
        public string NextUniqueNumber() => Interlocked.Increment( ref _typeID ).ToString();

        /// <summary>
        /// Gets a shared dictionary associated to the dynamic assembly. 
        /// Methods that generate code can rely on this to store shared information as required by their generation process.
        /// </summary>
        public IDictionary Memory => _memory;

        /// <summary>
        /// Pushes an action that will be executed before the generation of the final assembly: use this to 
        /// create final type from a <see cref="TypeBuilder"/> or to execute any action that must be done at the end 
        /// of the generation process.
        /// An action can be pushed at any moment and a pushed action can push another action.
        /// </summary>
        /// <param name="postAction">Action to execute.</param>
        public void PushFinalAction( Action<IDynamicAssembly> postAction )
        {
            if( postAction == null ) throw new ArgumentNullException( "postAction" );
            _postActions.Add( postAction );
        }

        /// <summary>
        /// Saves the dynamic assembly as a ".dll".
        /// This <see cref="DynamicAssembly"/> must have been constructed with an AssemblyBuilderAccess that has <see cref="AssemblyBuilderAccess.Save"/> bit set.
        /// </summary>
        public void Save()
        {
            int i = 0;
            while( i < _postActions.Count )
            {
                var a = _postActions[i];
                _postActions[i++] = null;
                a( this );
            }
            _assemblyBuilder.Save( _assemblyBuilder.GetName().Name + ".dll" );
        }
    }

}
