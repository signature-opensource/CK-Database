using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CK.CodeGen;
using CK.CodeGen.Abstractions;
using Microsoft.CodeAnalysis;

namespace CK.Core
{
    public class DynamicAssembly : IDynamicAssembly
    {
        int _typeID;
        readonly IDictionary _memory;

        /// <summary>
        /// Initializes a new <see cref="DynamicAssembly"/>.
        /// </summary>
        public DynamicAssembly()
        {
            var name = Guid.NewGuid().ToString();
            var assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly( new AssemblyName( name ), AssemblyBuilderAccess.Run );
            StubModuleBuilder = assemblyBuilder.DefineDynamicModule( name );

            _memory = new Dictionary<object, object>();

            SourceModules = new List<ICodeGeneratorModule>();
            var ws = CodeWorkspace.Create();
            ws.Global.Append( "[assembly:CK.Setup.ExcludeFromSetup()]" ).NewLine();
            DefaultGenerationNamespace = ws.Global.FindOrCreateNamespace( "CK._g" );
        }

        /// <summary>
        /// Gets the <see cref="StubModuleBuilder"/> for this <see cref="DynamicAssembly"/>.
        /// </summary>
        public ModuleBuilder StubModuleBuilder { get; }

        /// <summary>
        /// Gets the default name space for this <see cref="IDynamicAssembly"/>
        /// into which code should be generated.
        /// Note that nothing prevents the <see cref="ICodeScope.Workspace"/> to be used and other
        /// namespaces to be created.
        /// </summary>
        public INamespaceScope DefaultGenerationNamespace { get; }

        /// <summary>
        /// Gets the source modules for this <see cref="IDynamicAssembly"/>.
        /// </summary>
        public IList<ICodeGeneratorModule> SourceModules { get; }

        /// <summary>
        /// Provides a new unique number that can be used for generating unique names inside this dynamic assembly.
        /// </summary>
        /// <returns>A unique number.</returns>
        public string NextUniqueNumber() => (++_typeID).ToString();

        /// <summary>
        /// Gets a shared dictionary associated to the dynamic assembly. 
        /// Methods that generate code can rely on this to store shared information as required by their generation process.
        /// </summary>
        public IDictionary Memory => _memory;
    } 

}
