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
        public class DefaultAssemblyResolver : CK.CodeGen.IAssemblyResolver
        {
            public string GetAssemblyFilePath( Assembly a ) => new Uri( a.CodeBase ).LocalPath;

            public IEnumerable<AssemblyName> GetReferencedAssemblies( Assembly a ) => a.GetReferencedAssemblies();

            public Assembly LoadByName( AssemblyName n ) => Assembly.Load( n );
        }

        static bool HandleCreateResult( IActivityMonitor monitor, string sourceCode, GenerateResult result )
        {
            using( monitor.OpenInfo( "Code Generation information." ) )
            {
                if( result.LoadFailures.Count > 0 )
                {
                    using( monitor.OpenWarn( $"{result.LoadFailures.Count} assembly load failure(s)." ) )
                        foreach( var e in result.LoadFailures )
                            if( e.SuccessfulWeakFallback != null ) monitor.Warn( $"'{e.Name}' load failed, used '{e.SuccessfulWeakFallback}' instead." );
                            else monitor.Error( $"'{e.Name}' load failed." );
                }
                if( result.Success ) monitor.Trace( "Source code generation and compilation succeeded." );
                else
                {
                    using( monitor.OpenError( "Generation failed." ) )
                    {
                        if( result.EmitError != null )
                        {
                            monitor.Error( result.EmitError );
                        }
                        if( result.EmitResult != null )
                        {
                            if( !result.EmitResult.Success )
                            {
                                using( monitor.OpenError( $"{result.EmitResult.Diagnostics.Count()} Compilation diagnostics & Source code." ) )
                                {
                                    foreach( var diag in result.EmitResult.Diagnostics )
                                    {
                                        monitor.Trace( diag.ToString() );
                                    }
                                }
                                var withNumber = sourceCode
                                                    .Split( new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries )
                                                    .Select( (line,i) => $"{i+1,4} - {line}" )
                                                    .Concatenate(Environment.NewLine);
                                monitor.Trace( withNumber );
                            }
                        }
                    }
                }
                if( result.AssemblyLoadError != null )
                {
                    monitor.Error( "Generated assembly load failed.", result.AssemblyLoadError );
                }
                else if( result.Assembly != null )
                {
                    monitor.Trace( "Generated assembly successfuly loaded." );
                }
            }
            return result.Success;
        }


        public bool GenerateSourceCode( IActivityMonitor monitor, bool saveSource, bool withSrcSuffix )
        {
            try
            {
                // Injects System.Reflection and setup assemblies into the
                // workspace that will be used to generate source code.
                var ws = _tempAssembly.DefaultGenerationNamespace.Workspace;
                ws.EnsureAssemblyReference( typeof( BindingFlags ) );
                ws.EnsureAssemblyReference( _contractResult.Assemblies );

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
                    return false;
                }

                using( monitor.OpenInfo( "Compiling source code." ) )
                {
                    Debug.Assert( _tempAssembly.SaveFilePath.EndsWith( "Src.dll" ) );
                    string fileName = _tempAssembly.SaveFilePath;
                    if( !withSrcSuffix ) fileName = fileName.Substring( 0, fileName.Length - 7 ) + ".dll";

                    var g = new CodeGenerator( CodeWorkspace.Factory );
                    g.Modules.AddRange( _tempAssembly.SourceModules );
                    var result = g.Generate( ws, fileName, new DefaultAssemblyResolver() );
                    if( saveSource && result.Sources != null )
                    {
                        string sourceFile = fileName + ".cs";
                        monitor.Info( $"Saved source file: {sourceFile}" );
                        File.WriteAllText( sourceFile, result.Sources.Select( t => t.ToString() ).Concatenate( Environment.NewLine ) );
                    }
                    result.LogResult( monitor );
                    return result.Success;
                }
            }
            catch( Exception ex )
            {
                monitor.Error( $"While generating final assembly '{_tempAssembly.SaveFileName}' from source code.", ex );
                return false;
            }
        }

        void GenerateContextSource( IActivityMonitor monitor, IDynamicAssembly a)
        {
            var global = a.DefaultGenerationNamespace.Workspace.Global
                          .EnsureUsing( "CK.Core" )
                          .EnsureUsing( "System" )
                          .EnsureUsing( "System.Collections.Generic" )
                          .EnsureUsing( "System.Linq" )
                          .EnsureUsing( "System.Text" )
                          .EnsureUsing( "System.Reflection" );

            var ns = global.FindOrCreateNamespace( "CK.StObj" );

            #region GStObj & GContext
            const string sourceGStObj = @"
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

    public IContextualStObjMap Context { get; internal set; }

    public IStObj Generalization { get; }

    public IStObj Specialization { get; internal set; }

    internal object Instance;
    
    internal GStObj Leaf;

    internal StObjImplementation AsStObjImplementation => new StObjImplementation( this, Instance );
}";

            const string sourceGContext = @"
class GContext : IContextualStObjMap
{
    readonly Dictionary<Type, GStObj> _mappings;

    public GContext( GeneratedRootContext allContexts, Dictionary<Type, GStObj> map, string name)
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

    internal GeneratedRootContext AllContexts { get; } 

    IStObjMap IContextualStObjMap.AllContexts => AllContexts;

    public string Context { get; }

    public int MappedTypeCount => _mappings.Count;

    public IEnumerable<Type> Types => _mappings.Keys;

    IContextualRoot<IContextualTypeMap> IContextualTypeMap.AllContexts => AllContexts;

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

            ns.Append( sourceGStObj ).NewLine();
            ns.Append( sourceGContext ).NewLine();
            var rootCtx = ns.CreateType( "public class GeneratedRootContext : IStObjMap" )
                                .Append( "readonly GContext[] _contexts;" ).NewLine()
                                .Append( "internal readonly GStObj[] _stObjs;" ).NewLine();

            rootCtx.Append( "public GeneratedRootContext(IActivityMonitor monitor, IStObjRuntimeBuilder rb)" ).NewLine()
                   .Append( "{" ).NewLine()
                   .Append( $"_stObjs = new GStObj[{_orderedStObjs.Count}];" ).NewLine();
            int iStObj = 0;
            foreach( var m in _orderedStObjs )
            {
                string generalization = m.Generalization == null ? "null" : $"_stObjs[{m.Generalization.IndexOrdered}]";
                string typeName = m.ObjectType.ToCSharpName();
                string actualTypeName = m.Specialization == null 
                                            ? "typeof("+m.GetFinalTypeCSharpName( monitor, a )+")"
                                            : "null";
                rootCtx.Append( $"_stObjs[{iStObj++}] = new GStObj(" )
                       .Append( $"rb,typeof({typeName}),{generalization},{actualTypeName});" )
                       .NewLine();
            }

            rootCtx.Append( $"_contexts = new GContext[{Contexts.Count}];" ).NewLine();
            int iContext = 0;
            foreach( var ctx in Contexts )
            {
                rootCtx.Append( $"Dictionary<Type,GStObj> map = new Dictionary<Type,GStObj>();" ).NewLine();
                IDictionary all = ctx.InternalMapper.RawMappings;
                // We skip highest implementation Type mappings (ie. AmbientContractInterfaceKey keys) since 
                // there is no ToStObj mapping (to root generalization) on final (runtime) IContextualStObjMap.
                var typeMapping = all.Cast<KeyValuePair<object, MutableItem>>().Where( e => e.Key is Type );
                foreach( var e in typeMapping )
                {
                    rootCtx.Append( $"map.Add( typeof({((Type)e.Key).ToCSharpName()}), _stObjs[{e.Value.IndexOrdered}] );" ).NewLine();
                }
                rootCtx.Append( $"_contexts[{iContext++}] = new GContext( this, map, {ctx.Context.ToSourceString()} );" ).NewLine();
            }
            rootCtx.Append( "Default = _contexts[0];" ).NewLine();

            rootCtx.Append( $"int iStObj = {_orderedStObjs.Count};" ).NewLine();
            rootCtx.Append( "while( --iStObj >= 0 ) {" ).NewLine()
                   .Append( " var o = _stObjs[iStObj];" ).NewLine()
                   .Append( " if( o.Specialization == null ) {" ).NewLine()
                   .Append( "  GStObj g = (GStObj)o.Generalization;" ).NewLine()
                   .Append( "  while( g != null ) {" ).NewLine()
                   .Append( "   g.Specialization = o;" ).NewLine()
                   .Append( "   g.Instance = o.Instance;" ).NewLine()
                   .Append( "   g.Context = o.Context;" ).NewLine()
                   .Append( "   g.Leaf = o.Leaf;" ).NewLine()
                   .Append( "   o = g;" ).NewLine()
                   .Append( "   g = (GStObj)o.Generalization;" ).NewLine()
                   .Append( "  }" ).NewLine()
                   .Append( " }" ).NewLine()
                   .Append( "}" ).NewLine();
            var propertyCache = new Dictionary<ValueTuple<Type, string>, string>();
            foreach( var m in _orderedStObjs )
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
                var mConstruct = m.AmbientTypeInfo.StObjConstruct;
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
            foreach( var m in _orderedStObjs )
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
            foreach( var m in _orderedStObjs )
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

            rootCtx.Append( "public IEnumerable<StObjImplementation> AllStObjs => Contexts.SelectMany( c => c.StObjs );" ).NewLine();
            rootCtx.Append( "public IContextualStObjMap Default { get; }" ).NewLine();
            rootCtx.Append( "public IReadOnlyCollection<IContextualStObjMap> Contexts => _contexts;" ).NewLine();
            rootCtx.Append( "public IContextualStObjMap FindContext( string context ) => Contexts.FirstOrDefault( c => ReferenceEquals( c.Context, context ?? String.Empty ) );" ).NewLine();
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




