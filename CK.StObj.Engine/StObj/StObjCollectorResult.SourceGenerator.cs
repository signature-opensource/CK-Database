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

        #region GStObj
        static readonly string _sourceGStObj = @"
class GStObj : IStObj
{
    public GStObj( IStObjRuntimeBuilder rb, Type t, IStObj g, Type actualType )
    {
        ObjectType = t;
        Generalization = g;
        if( actualType != null ) 
        {
            Instance = rb.CreateInstance( actualType );
            Leaf = this;
        }
    }

    public Type ObjectType { get; }

    public IStObj Generalization { get; }

    public IStObj Specialization { get; internal set; }

    internal object Instance;
    
    internal GStObj Leaf;

    internal StObjImplementation AsStObjImplementation => new StObjImplementation( this, Instance );
}";

        static readonly string _sourceGContext = @"
class GContext : IContextualStObjMap
{
    readonly Dictionary<Type, GStObj> _mappings;

    public GContext( " + StObjContextRoot.RootContextTypeName + @" allContexts, Dictionary<Type, GStObj> map, string name)
    {
        AllContexts = allContexts;
        _mappings = map;
        Context = name;
        var distinct = new HashSet<object>();
        foreach( var gs in map.Values ) 
        { 
            gs.Context = this;
            distinct.Add( gs.Instance ); 
        }
        Implementations = distinct.ToArray();
    }

    public IEnumerable<object> Implementations { get; }

    public IEnumerable<StObjImplementation> StObjs => AllContexts._stObjs.Where( s => s.Context == this ).Select( s => s.AsStObjImplementation );

    public IEnumerable<KeyValuePair<Type, object>> Mappings => _mappings.Select( v => new KeyValuePair<Type, object>( v.Key, v.Value.Instance ) );

    public int MappedTypeCount => _mappings.Count;

    public IEnumerable<Type> Types => _mappings.Keys;

    public bool IsMapped( Type t ) => _mappings.ContainsKey( t );

    public object Obtain( Type t ) => GToLeaf( t )?.Instance;

    public IStObj ToLeaf( Type t ) => GToLeaf( t );

    public Type ToLeafType( Type t ) => GToLeaf( t )?.ObjectType;

    GStObj GToLeaf( Type t )
    {
        GStObj s;
        if( _mappings.TryGetValue( t, out s ) )
        {
            return s.Leaf;
        }
        return null;
    }
}";
        #endregion

        void GenerateContextSource( IActivityMonitor monitor, IDynamicAssembly a )
        {
            var global = a.DefaultGenerationNamespace.Workspace.Global
                          .EnsureUsing( "CK.Core" )
                          .EnsureUsing( "System" )
                          .EnsureUsing( "System.Collections.Generic" )
                          .EnsureUsing( "System.Linq" )
                          .EnsureUsing( "System.Text" )
                          .EnsureUsing( "System.Reflection" );

            var ns = global.FindOrCreateNamespace( "CK.StObj" );

            ns.Append( _sourceGStObj ).NewLine();
            ns.Append( _sourceGContext ).NewLine();
            var rootCtx = ns.CreateType( "public class " + StObjContextRoot.RootContextTypeName + " : IStObjMap, IStObjObjectMap" )
                                .Append( "readonly GStObj[] _stObjs;" ).NewLine()
                                .Append( "readonly GStObj[] _implStObjs;" ).NewLine()
                                .Append( "readonly Dictionary<Type,GStObj> _map;" ).NewLine();

            rootCtx.Append( "public " ).Append( StObjContextRoot.RootContextTypeName ).Append( "(IActivityMonitor monitor, IStObjRuntimeBuilder rb)" ).NewLine()
                   .Append( "{" ).NewLine()
                   .Append( $"_stObjs = new GStObj[{OrderedStObjs.Count}];" ).NewLine()
                   .Append( $"_implStObjs = new GStObj[{AmbientTypeResult.AmbientContracts.EngineMap.AllSpecializations.Count}];" ).NewLine();
            int iStObj = 0;
            int iImplStObj = 0;
            foreach( MutableItem m in OrderedStObjs )
            {
                string generalization = m.Generalization == null ? "null" : $"_stObjs[{m.Generalization.IndexOrdered}]";
                string typeName = m.ObjectType.ToCSharpName();
                string actualTypeName = m.Specialization == null 
                                            ? "typeof("+m.GetFinalTypeCSharpName( monitor, a )+")"
                                            : "null";
                rootCtx.Append( $"_implStObjs[{iImplStObj++}] = " );
                if( m.Specialization == null )
                {
                    rootCtx.Append( $"_stObjs[{iStObj++}] = " );
                }
                rootCtx.Append( "new GStObj(" )
                       .Append( $"rb,typeof({typeName}),{generalization},{actualTypeName});" )
                       .NewLine();
            }

            rootCtx.Append( $"_map = new Dictionary<Type,GStObj>();" ).NewLine();
            var allMappings = AmbientTypeResult.AmbientContracts.EngineMap.RawMappings;
            // We skip highest implementation Type mappings (ie. AmbientContractInterfaceKey keys) since 
            // there is no ToStObj mapping (to root generalization) on final (runtime) IContextualStObjMap.
            foreach( var e in allMappings.Where( e => e.Key is Type ) )
            {
                rootCtx.Append( $"map.Add( typeof({((Type)e.Key).ToCSharpName()}), _stObjs[{e.Value.IndexOrdered}] );" ).NewLine();
            }

            rootCtx.Append( $"int iStObj = {OrderedStObjs.Count};" ).NewLine();
            rootCtx.Append( "while( --iStObj >= 0 ) {" ).NewLine()
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
                            rootCtx
                                .Append( "PropertyInfo " )
                                .Append( varName )
                                .Append( "=" )
                                .Append( "typeof(" )
                                .AppendCSharpName( decl )
                                .Append( ").GetProperty(" )
                                .AppendSourceString( setter.Property.Name )
                                .Append( ",BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic);" )
                                .NewLine();
                            propertyCache.Add( key, varName );

                        }
                        rootCtx.Append(varName)
                               .Append( ".SetValue(_stObjs[" )
                               .Append( m.IndexOrdered).Append( "].Instance," );
                        GenerateValue( rootCtx, setter.Value );
                        rootCtx.Append( ");" ).NewLine();
                    }
                }
                var mConstruct = m.Type.StObjConstruct;
                if( mConstruct != null )
                {
                    rootCtx.Append( $"_stObjs[{m.IndexOrdered}].ObjectType.GetMethod( \"{mConstruct.Name}\", BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic )" )
                           .Append( $".Invoke( _stObjs[{m.IndexOrdered}].Instance, " );
                    if( m.ConstructParameters.Count == 0 ) rootCtx.Append( "Array.Empty<object>()" );
                    else
                    {
                        rootCtx.Append( "new object[] {" );
                        // Missing Value {get;} on IStObjMutableParameter. We cast for the moment.
                        foreach( var p in m.ConstructParameters.Cast<MutableParameter>() )
                        {
                            if( p.BuilderValueIndex < 0 )
                            {
                                rootCtx.Append( $"_stObjs[{-(p.BuilderValueIndex + 1)}].Instance" );
                            }
                            else GenerateValue( rootCtx, p.Value );
                            rootCtx.Append( ',' );
                        }
                        rootCtx.Append( "}" );
                    }
                    rootCtx.Append( ");" ).NewLine();
                }
            }
            foreach( MutableItem m in OrderedStObjs )
            {
                if( m.PostBuildProperties != null )
                {
                    foreach( var p in m.PostBuildProperties )
                    {
                        Type decl = p.Property.DeclaringType;
                        rootCtx.Append( "typeof(" )
                               .AppendCSharpName( decl )
                               .Append( $").GetProperty( \"{p.Property.Name}\", BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic )" )
                               .Append( $".SetValue(_stObjs[{m.IndexOrdered}].Instance, " );
                        GenerateValue( rootCtx, p.Value );
                        rootCtx.Append( ");" ).NewLine();
                    }
                }
            }
            foreach( MutableItem m in OrderedStObjs )
            {
                MethodInfo init = m.ObjectType.GetMethod( StObjContextRoot.InitializeMethodName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic ); 
                if( init != null )
                {
                    rootCtx.Append( $"_stObjs[{m.IndexOrdered}].ObjectType.GetMethod( \"{StObjContextRoot.InitializeMethodName}\", BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic )" )
                           .NewLine();
                    rootCtx.Append( $".Invoke( _stObjs[{m.IndexOrdered}].Instance, new object[]{{ monitor, _stObjs[{m.IndexOrdered}].Context }} );" )
                           .NewLine();
                }
            }
            rootCtx.Append( "} // /ctor" ).NewLine();

            rootCtx.Append( @"
            public IStObjObjectMap StObjs => this;

            int IStObjTypeMap.MappedTypeCount => _map.Count;
            Type IStObjTypeMap.ToLeafType( Type t ) => GToLeaf( t )?.ObjectType;
            bool IStObjTypeMap.IsMapped( Type t ) => _map.ContainsKey( t );
            IEnumerable<Type> IStObjTypeMap.Types => _mappings.Keys;

            IStObj IStObjObjectMap.ToLeaf( Type t ) => GToLeaf( t );
            object IStObjObjectMap.Obtain( Type t ) => _map.TryGetValue( t, out var s ) ? s.Instance : null;
            IEnumerable<object> IStObjObjectMap.Implementations => _implStObjs.Select( s => s.Instance );
            IEnumerable<StObjImplementation> IStObjObjectMap.StObjs => _implStObjs.Select( s => s.AsStObjImplementation );
            IEnumerable<KeyValuePair<Type, object>> IStObjObjectMap.Mappings => _map.Select( v => new KeyValuePair<Type, object>( v.Key, v.Value.Instance ) );

            GStObj GToLeaf( Type t ) => _map.TryGetValue( t, out var s ) ? s.Leaf : null; " ).NewLine();
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




