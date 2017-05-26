using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CK.Core;
using System.Diagnostics;
using System.Reflection;
using System.Resources;
using System.Collections;
using System.IO;
using CK.CodeGen;

namespace CK.Setup
{
    public partial class StObjCollectorResult
    {
        public class DefaultAssemblyResolver : CK.CodeGen.IAssemblyResolver
        {
            public string GetAssemblyFilePath( Assembly a ) => new Uri( a.CodeBase ).LocalPath;

            public IEnumerable<AssemblyName> GetReferencedAssemblies( Assembly a ) => a.GetReferencedAssemblies();

            public Assembly LoadByName( AssemblyName n ) => Assembly.Load( n );
        }

        private static Func<string, Assembly> GetAssemblyLoader()
        {
            Func<string, Assembly> loader;
#if NET461
            loader = Assembly.LoadFrom;
#else
            loader = System.Runtime.Loader.AssemblyLoadContext.Default.LoadFromAssemblyPath;
#endif
            return loader;
        }

        static bool HandleCreateResult( IActivityMonitor monitor, string sourceCode, GenerateResult result )
        {
            using( monitor.OpenInfo().Send( "Code Generation information." ) )
            {
                if( result.LoadFailures.Count > 0 )
                {
                    using( monitor.OpenWarn().Send( $"{result.LoadFailures.Count} assembly load failure(s)." ) )
                        foreach( var e in result.LoadFailures )
                            if( e.SuccessfulWeakFallback != null ) monitor.Warn().Send( $"'{e.Name}' load failed, used '{e.SuccessfulWeakFallback}' instead." );
                            else monitor.Error().Send( $"'{e.Name}' load failed." );
                }
                if( !result.Success )
                {
                    using( monitor.OpenError().Send( "Generation failed." ) )
                    {
                        if( result.EmitError != null )
                        {
                            monitor.Error().Send( result.EmitError );
                        }
                        if( result.EmitResult != null )
                        {
                            if( !result.EmitResult.Success && !result.EmitResult.Diagnostics.IsEmpty )
                            {
                                using( monitor.OpenError().Send( "Compilation diagnostics." ) )
                                {
                                    foreach( var diag in result.EmitResult.Diagnostics )
                                    {
                                        monitor.Trace().Send( diag.ToString() );
                                    }
                                }
                                monitor.Trace().Send( sourceCode );
                            }
                        }
                    }
                }
                if( result.AssemblyLoadError != null )
                {
                    monitor.Error().Send( result.AssemblyLoadError, "Generated assembly load failed." );
                }
                else if( result.Assembly != null )
                {
                    monitor.Trace().Send( "Generated assembly successfuly loaded." );
                }
            }
            return result.Success;
        }


        public bool GenerateSourceCode( IActivityMonitor monitor, IStObjRuntimeBuilder runtimeBuilder, bool saveSource )
        {
            try
            {
                var b = new StringBuilder();
                GenerateContextSource(b, monitor, _tempAssembly);
                _tempAssembly.SourceBuilder.CreateSource( b );
                var src = b.ToString();
                if( saveSource ) File.WriteAllText( _tempAssembly.SaveFilePath + ".cs", src );
                var g = new CodeGenerator();
                var result = g.Generate( src, _tempAssembly.SaveFilePath, _contractResult.Assemblies.Where( a => !a.IsDynamic ), new DefaultAssemblyResolver(), GetAssemblyLoader() );
                return HandleCreateResult( monitor, src, result );
            }
            catch( Exception ex )
            {
                monitor.Error().Send( ex, $"While generating final assembly '{_tempAssembly.SaveFileName}' from source code." );
                return false;
            }
        }

        void GenerateContextSource(StringBuilder b, IActivityMonitor monitor, IDynamicAssembly a)
        {
            var allTypes = _orderedStObjs
                            .Select( m => m.Specialization == null
                                            ? m.GetFinalTypeFullName( monitor, a )
                                            : m.ObjectType.AssemblyQualifiedName )
                            .ToList();
            b.AppendLine( "using CK.Core; using System; using System.Collections.Generic; using System.Linq; using System.Text;" );
            b.AppendLine( "namespace CK.StObj {" );
            b.AppendLine( "public class GeneratedRootContext : IStObjMap {" );
            b.AppendLine( "readonly GContext[] _contexts;" );
            b.AppendLine( "readonly GStObj[] _stObjs;" );
            b.AppendLine( "readonly GStObjImpl[] _stObjsImpl;" );
            b.AppendLine( "public GeneratedRootContext(IActivityMonitor monitor, IStObjRuntimeBuilder rb) {" );

            b.AppendLine( $"_stObjs = new GStObj[{_orderedStObjs.Count}];" );
            b.AppendLine( $"_stObjsImpl = new GStObjImpl[{TotalSpecializationCount}];" );
            int iStObj = 0;
            int iStObjImpl = 0;
            foreach( var m in _orderedStObjs )
            {
                string generalization = m.Generalization == null ? "null" : $"_stObjs[{m.Generalization.IndexOrdered}]";
                if( m.Specialization == null )
                {
                    string typeName = m.GetFinalTypeFullName( monitor, a );
                    b.AppendLine( $"_stObjs[{iStObj++}] = _stObjsImpl[{iStObjImpl++}] = new GStObjImpl(" );
                    b.AppendLine( $"{typeName.ToSourceString()},{generalization},rb);" );
                }
                else
                {
                    string typeName = m.ObjectType.AssemblyQualifiedName;
                    b.AppendLine( $"_stObjs[{iStObj++}] = new GStObj(" );
                    b.AppendLine( $"{typeName.ToSourceString()},{generalization});" );
                }
            }

            b.AppendLine( $"_contexts = new GContext[{Contexts.Count}];" );
            iStObj = 0;
            foreach( var ctx in Contexts )
            {
                IDictionary all = ctx.InternalMapper.RawMappings;
                // We skip highest implementation Type mappings (ie. AmbientContractInterfaceKey keys) since 
                // there is no ToStObj mapping on final (runtime) IContextualStObjMap.
                var typeMapping = all.Cast<KeyValuePair<object, MutableItem>>().Where( e => e.Key is Type );
                b.AppendLine( "{" );
                b.AppendLine( "var map = new Dictionary<Type,GStObjImpl>();" );
                foreach( var e in typeMapping )
                {
                    string typeName = ((Type)e.Key).AssemblyQualifiedName;
                    b.AppendLine( $"map.Add( Type.GetType({typeName.ToSourceString()}), _stObjsImpl[{e.Value.SpecializationIndexOrdered}] );" );
                }
                b.AppendLine( $"_contexts[{iStObj++}] = new GContext( this, map, {ctx.Context.ToSourceString()} );" );
                b.AppendLine( "}" );
            }
            b.AppendLine( "" );

            b.AppendLine( "}" );
            b.AppendLine( "public IEnumerable<StObjImplementation> AllStObjs => Contexts.SelectMany( c => c.StObjs );" );
            b.AppendLine( "public IContextualStObjMap Default { get; }" );
            b.AppendLine( "public IReadOnlyCollection<IContextualStObjMap> Contexts { get; }" );
            b.AppendLine( "public IContextualStObjMap FindContext( string context ) => Contexts.FirstOrDefault( c => ReferenceEquals( c.Context, context ?? String.Empty ) );" );
            b.AppendLine( "}}" );
        }

    }
}
