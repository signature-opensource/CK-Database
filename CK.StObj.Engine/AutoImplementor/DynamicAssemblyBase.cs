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
using Microsoft.CodeAnalysis;

namespace CK.Core
{
    public abstract class DynamicAssemblyBase : IDynamicAssembly
    {
        int _typeID;
        readonly IDictionary _memory;
        readonly List<Action<IDynamicAssembly>> _postActions;
        readonly string _saveFileName;
        readonly string _saveFilePath;

        /// <summary>
        /// Initializes a new <see cref="DynamicAssembly"/> with the given name and access.
        /// </summary>
        /// <param name="directory">Directory where the assembly must be saved. Must not be null if the assembly must be saved.</param>
        /// <param name="assemblyName">Assembly name to use.</param>
        protected DynamicAssemblyBase(string directory, string assemblyName )
        {
            if (String.IsNullOrWhiteSpace(assemblyName)) throw new ArgumentException("Name is invalid.", nameof(assemblyName));

            AssemblyName = new AssemblyName(assemblyName);
            AssemblyName.Version = new Version(1, 0, 0, 0);
            if (directory != null)
            {
                _saveFileName = AssemblyName.Name + ".dll";
                _saveFilePath = System.IO.Path.Combine(directory, _saveFileName);
            }
            _memory = new Dictionary<object, object>();
            _postActions = new List<Action<IDynamicAssembly>>();
            SourceModules = new List<ICodeGeneratorModule>();
            SourceModules.Add( new ExcludeFromSetupModule() );
            SourceBuilder = new NamespaceBuilder("CK._g");
        }

        class ExcludeFromSetupModule : ICodeGeneratorModule
        {
            public IEnumerable<Assembly> RequiredAssemblies => Enumerable.Empty<Assembly>();

            public void AppendSource( StringBuilder b )
            {
                b.AppendLine( "[assembly:CK.Setup.ExcludeFromSetup()]" );
            }

            public SyntaxTree PostProcess( SyntaxTree t ) => t;
        }

        protected AssemblyName AssemblyName { get; }

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
        public ModuleBuilder ModuleBuilder { get; protected set; }

        /// <summary>
        /// Gets the source builder for this <see cref="IDynamicAssembly"/>.
        /// </summary>
        public NamespaceBuilder SourceBuilder { get; }

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

        /// <summary>
        /// Pushes an action that will be executed before the generation of the final assembly: use this to 
        /// create final type from a <see cref="TypeBuilder"/> or to execute any action that must be done at the end 
        /// of the generation process.
        /// An action can be pushed at any moment and a pushed action can push another action.
        /// </summary>
        /// <param name="postAction">Action to execute.</param>
        public void PushFinalAction(Action<IDynamicAssembly> postAction)
        {
            if (postAction == null) throw new ArgumentNullException("postAction");
            _postActions.Add(postAction);
        }

        /// <summary>
        /// Executes registered post actions and clears them.
        /// </summary>
        protected void ExecutePostActions()
        {
            int i = 0;
            while (i < _postActions.Count)
            {
                var a = _postActions[i];
                _postActions[i++] = null;
                a(this);
            }
            _postActions.Clear();
        }
    }

}
