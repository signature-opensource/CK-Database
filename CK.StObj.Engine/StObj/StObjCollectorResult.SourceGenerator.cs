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
using CK.Text;
using CK.CodeGen.Abstractions;

namespace CK.Setup
{
    public partial class StObjCollectorResult
    {
        public struct CodeGenerateResult
        {
            /// <summary>
            /// Gets whether the generation succeeded.
            /// </summary>
            public readonly bool Success;

            /// <summary>
            /// Gets the list of files that have been generated: the assembly itself and
            /// any source code or other files.
            /// </summary>
            public readonly IReadOnlyList<string> GeneratedFileNames;

            internal CodeGenerateResult( bool success, IReadOnlyList<string> fileNames )
            {
                Success = success;
                GeneratedFileNames = fileNames;
            }
        }

        CodeGenerateResult GenerateSourceCode( IActivityMonitor monitor, string finalFilePath, bool saveSource, bool skipCompilation )
        {
            List<string> generatedFileNames = new List<string>();
            try
            {
                // Injects System.Reflection and setup assemblies into the
                // workspace that will be used to generate source code.
                var ws = _tempAssembly.DefaultGenerationNamespace.Workspace;
                ws.EnsureAssemblyReference( typeof( BindingFlags ) );
                ws.EnsureAssemblyReference( AmbientTypeResult.Assemblies );

                IReadOnlyList<ActivityMonitorSimpleCollector.Entry> errorSummary = null;
                using( monitor.OpenInfo( "Generating source code." ) )
                using( monitor.CollectEntries( entries => errorSummary = entries ) )
                {
                   GenerateContextSource( monitor, _tempAssembly );
                }
                if( errorSummary != null )
                {
                    using( monitor.OpenFatal( $"{errorSummary.Count} error(s). Summary:" ) )
                    {
                        foreach( var e in errorSummary )
                        {
                            monitor.Trace( $"{e.MaskedLevel} - {e.Text}" );
                        }
                    }
                    return new CodeGenerateResult( false, generatedFileNames );
                }
                if( skipCompilation )
                {
                    monitor.OpenInfo( "Compilation is skipped." );
                    return new CodeGenerateResult( true, generatedFileNames );
                }
                using( monitor.OpenInfo( "Compiling source code." ) )
                {
                    var g = new CodeGenerator( CodeWorkspace.Factory );
                    g.Modules.AddRange( _tempAssembly.SourceModules );
                    var result = g.Generate( ws, finalFilePath );
                    if( saveSource && result.Sources != null )
                    {
                        for( int i = 0; i < result.Sources.Count; ++i )
                        {
                            string sourceFile = $"{finalFilePath}.{i}.cs";
                            monitor.Info( $"Saved source file: {sourceFile}" );
                            File.WriteAllText( sourceFile, result.Sources[i].ToString() );
                            generatedFileNames.Add( Path.GetFileName( sourceFile ) );
                        }
                    }
                    if( result.Success ) generatedFileNames.Add( Path.GetFileName( finalFilePath ) );
                    result.LogResult( monitor );
                    return new CodeGenerateResult( result.Success, generatedFileNames );
                }
            }
            catch( Exception ex )
            {
                monitor.Error( $"While generating final assembly '{finalFilePath}' from source code.", ex );
                return new CodeGenerateResult( false, generatedFileNames );
            }
        }

        static readonly string _sourceGStObj = @"
class GStObj : IStObj
{
    public GStObj( IStObjRuntimeBuilder rb, Type t, IStObj g, Type actualType, IStObjMap m )
    {
        StObjMap = m;
        ObjectType = t;
        Generalization = g;
        if( actualType != null ) 
        {
            Instance = rb.CreateInstance( actualType );
            Leaf = this;
        }
    }

    public IStObjMap StObjMap { get; }

    public Type ObjectType { get; }

    public IStObj Generalization { get; }

    public IStObj Specialization { get; internal set; }

    internal object Instance;
    
    internal GStObj Leaf;

    internal StObjImplementation AsStObjImplementation => new StObjImplementation( this, Instance );
}";
        void GenerateContextSource( IActivityMonitor monitor, IDynamicAssembly a )
        {
            var global = a.DefaultGenerationNamespace.Workspace.Global
                          .EnsureUsing( "CK.Core" )
                          .EnsureUsing( "System" )
                          .EnsureUsing( "System.Collections.Generic" )
                          .EnsureUsing( "System.Linq" )
                          .EnsureUsing( "System.Text" )
                          .EnsureUsing( "System.Reflection" );

            foreach( var t in AmbientTypeResult.TypesToImplement )
            {
                t.GenerateType( monitor, a );
            }

            var ns = global.FindOrCreateNamespace( "CK.StObj" );

            ns.Append( _sourceGStObj ).NewLine();

            var rootType = ns.CreateType( "public class " + StObjContextRoot.RootContextTypeName + " : IStObjMap, IStObjObjectMap, IStObjServiceMap" )
                                .Append( "readonly GStObj[] _stObjs;" ).NewLine()
                                .Append( "readonly GStObj[] _implStObjs;" ).NewLine()
                                .Append( "readonly Dictionary<Type,GStObj> _map;" ).NewLine();

            var rootCtor = rootType.CreateFunction( $"public {StObjContextRoot.RootContextTypeName}(IActivityMonitor monitor, IStObjRuntimeBuilder rb)" );

            rootCtor.Append( $"_stObjs = new GStObj[{OrderedStObjs.Count}];" ).NewLine()
                    .Append( $"_implStObjs = new GStObj[{AmbientTypeResult.AmbientContracts.EngineMap.AllSpecializations.Count}];" ).NewLine();
            int iStObj = 0;
            int iImplStObj = 0;
            foreach( MutableItem m in OrderedStObjs )
            {
                string generalization = m.Generalization == null ? "null" : $"_stObjs[{m.Generalization.IndexOrdered}]";
                //string typeName = m.ObjectType.ToCSharpName();
                //string actualTypeName = m.Specialization == null 
                //                            ? "typeof("+m.GetFinalTypeCSharpName( monitor, a )+")"
                //                            : "null";
                rootCtor.Append( $"_stObjs[{iStObj++}] = " );
                if( m.Specialization == null )
                {
                    rootCtor.Append( $"_implStObjs[{iImplStObj++}] = " );
                }
                rootCtor.Append( "new GStObj(" )
                       .Append( "rb, " )
                       .AppendTypeOf( m.ObjectType ).Append(", ")
                       .Append( generalization ).Append( ", ")
                       .AppendTypeOf( m.Specialization == null ? m.ImplementableTypeInfo?.StubType ?? m.Type.Type : null )
                       .Append( ", this );" )
                       .NewLine();
            }

            rootCtor.Append( $"_map = new Dictionary<Type,GStObj>();" ).NewLine();
            var allMappings = AmbientTypeResult.AmbientContracts.EngineMap.RawMappings;
            // We skip highest implementation Type mappings (ie. AmbientContractInterfaceKey keys) since 
            // there is no ToStObj mapping (to root generalization) on final (runtime) IStObjMap.
            foreach( var e in allMappings.Where( e => e.Key is Type ) )
            {
                rootCtor.Append( $"_map.Add( " ).AppendTypeOf( (Type)e.Key )
                       .Append( ", _stObjs[").Append( e.Value.IndexOrdered ).Append( "] );" ).NewLine();
            }
            if( OrderedStObjs.Count > 0 )
            {
                rootCtor.Append( $"int iStObj = {OrderedStObjs.Count};" ).NewLine()
                       .Append( "while( --iStObj >= 0 ) {" ).NewLine()
                       .Append( " var o = _stObjs[iStObj];" ).NewLine()
                       .Append( " if( o.Specialization == null ) {" ).NewLine()
                       .Append( "  GStObj g = (GStObj)o.Generalization;" ).NewLine()
                       .Append( "  while( g != null ) {" ).NewLine()
                       .Append( "   g.Specialization = o;" ).NewLine()
                       .Append( "   g.Instance = o.Instance;" ).NewLine()
                       .Append( "   g.Leaf = o.Leaf;" ).NewLine()
                       .Append( "   o = g;" ).NewLine()
                       .Append( "   g = (GStObj)o.Generalization;" ).NewLine()
                       .Append( "  }" ).NewLine()
                       .Append( " }" ).NewLine()
                       .Append( "}" ).NewLine();
            }
            var propertyCache = new Dictionary<ValueTuple<Type, string>, string>();
            foreach( MutableItem m in OrderedStObjs )
            {
                if( m.PreConstructProperties != null )
                {
                    foreach( var setter in m.PreConstructProperties )
                    {
                        Type decl = setter.Property.DeclaringType;
                        string varName;
                        var key = ValueTuple.Create( decl, setter.Property.Name );
                        if(!propertyCache.TryGetValue( key, out varName ))
                        {
                            varName = "pI" + propertyCache.Count.ToString();
                            rootCtor
                                .Append( "PropertyInfo " )
                                .Append( varName )
                                .Append( "=" )
                                .AppendTypeOf( decl )
                                .Append( ".GetProperty(" )
                                .AppendSourceString( setter.Property.Name )
                                .Append( ",BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic);" )
                                .NewLine();
                            propertyCache.Add( key, varName );

                        }
                        rootCtor.Append(varName)
                               .Append( ".SetValue(_stObjs[" )
                               .Append( m.IndexOrdered).Append( "].Instance," );
                        GenerateValue( rootCtor, setter.Value );
                        rootCtor.Append( ");" ).NewLine();
                    }
                }
                var mConstruct = m.Type.StObjConstruct;
                if( mConstruct != null )
                {
                    rootCtor.Append( $"_stObjs[{m.IndexOrdered}].ObjectType.GetMethod( \"{mConstruct.Name}\", BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic )" )
                           .Append( $".Invoke( _stObjs[{m.IndexOrdered}].Instance, " );
                    if( m.ConstructParameters.Count == 0 ) rootCtor.Append( "Array.Empty<object>()" );
                    else
                    {
                        rootCtor.Append( "new object[] {" );
                        // Missing Value {get;} on IStObjMutableParameter. We cast for the moment.
                        foreach( var p in m.ConstructParameters.Cast<MutableParameter>() )
                        {
                            if( p.BuilderValueIndex < 0 )
                            {
                                rootCtor.Append( $"_stObjs[{-(p.BuilderValueIndex + 1)}].Instance" );
                            }
                            else GenerateValue( rootCtor, p.Value );
                            rootCtor.Append( ',' );
                        }
                        rootCtor.Append( "}" );
                    }
                    rootCtor.Append( ");" ).NewLine();
                }
            }
            foreach( MutableItem m in OrderedStObjs )
            {
                if( m.PostBuildProperties != null )
                {
                    foreach( var p in m.PostBuildProperties )
                    {
                        Type decl = p.Property.DeclaringType;
                        rootCtor.Append( "typeof(" )
                               .AppendCSharpName( decl )
                               .Append( $").GetProperty( \"{p.Property.Name}\", BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic )" )
                               .Append( $".SetValue(_stObjs[{m.IndexOrdered}].Instance, " );
                        GenerateValue( rootCtor, p.Value );
                        rootCtor.Append( ");" ).NewLine();
                    }
                }
            }
            foreach( MutableItem m in OrderedStObjs )
            {
                MethodInfo init = m.ObjectType.GetMethod( StObjContextRoot.InitializeMethodName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic ); 
                if( init != null )
                {
                    rootCtor.Append( $"_stObjs[{m.IndexOrdered}].ObjectType.GetMethod( \"{StObjContextRoot.InitializeMethodName}\", BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic )" )
                           .NewLine();
                    rootCtor.Append( $".Invoke( _stObjs[{m.IndexOrdered}].Instance, new object[]{{ monitor, this }} );" )
                           .NewLine();
                }
            }

            rootType.Append( "public string MapName => " ).AppendSourceString( MapName ).Append( ";" ).NewLine()
                    .Append( @"
            public IStObjObjectMap StObjs => this;

            Type IStObjTypeMap.ToLeafType( Type t ) => GToLeaf( t )?.ObjectType;
            bool IStObjTypeMap.IsMapped( Type t ) => _map.ContainsKey( t );
            IEnumerable<Type> IStObjTypeMap.Types => _map.Keys;

            IStObj IStObjObjectMap.ToLeaf( Type t ) => GToLeaf( t );
            object IStObjObjectMap.Obtain( Type t ) => _map.TryGetValue( t, out var s ) ? s.Instance : null;
            IEnumerable<object> IStObjObjectMap.Implementations => _implStObjs.Select( s => s.Instance );
            IEnumerable<StObjImplementation> IStObjObjectMap.StObjs => _implStObjs.Select( s => s.AsStObjImplementation );
            IEnumerable<KeyValuePair<Type, object>> IStObjObjectMap.Mappings => _map.Select( v => new KeyValuePair<Type, object>( v.Key, v.Value.Instance ) );

            GStObj GToLeaf( Type t ) => _map.TryGetValue( t, out var s ) ? s.Leaf : null;
            " );

            var serviceGen = new ServiceSupportCodeGenerator( rootType, rootCtor );
            serviceGen.CreateServiceSupportCode( _liftedMap );
        }

        static void GenerateValue( ICodeWriter b, object o )
        {
            if( o is IActivityMonitor )
            {
                b.Append( "monitor" );
            }
            else if( o is MutableItem )
            {
                b.Append( $"_stObjs[{((MutableItem)o).IndexOrdered}].Instance" );
            }
            else
            {
                b.Append( o );
            }
        }
    }
}




