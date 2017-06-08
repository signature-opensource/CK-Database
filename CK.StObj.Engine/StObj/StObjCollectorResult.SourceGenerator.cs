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
            using( monitor.OpenInfo().Send( "Code Generation information." ) )
            {
                if( result.LoadFailures.Count > 0 )
                {
                    using( monitor.OpenWarn().Send( $"{result.LoadFailures.Count} assembly load failure(s)." ) )
                        foreach( var e in result.LoadFailures )
                            if( e.SuccessfulWeakFallback != null ) monitor.Warn().Send( $"'{e.Name}' load failed, used '{e.SuccessfulWeakFallback}' instead." );
                            else monitor.Error().Send( $"'{e.Name}' load failed." );
                }
                if( result.Success ) monitor.Trace().Send( "Source code generation and compilation succeeded." );
                else
                {
                    using( monitor.OpenError().Send( "Generation failed." ) )
                    {
                        if( result.EmitError != null )
                        {
                            monitor.Error().Send( result.EmitError );
                        }
                        if( result.EmitResult != null )
                        {
                            if( !result.EmitResult.Success )
                            {
                                using( monitor.OpenError().Send( $"{result.EmitResult.Diagnostics.Count()} Compilation diagnostics & Source code." ) )
                                {
                                    foreach( var diag in result.EmitResult.Diagnostics )
                                    {
                                        monitor.Trace().Send( diag.ToString() );
                                    }
                                }
                                var withNumber = sourceCode
                                                    .Split( new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries )
                                                    .Select( (line,i) => $"{i+1,4} - {line}" )
                                                    .Concatenate(Environment.NewLine);
                                monitor.Trace().Send( withNumber );
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
                GenerateContextSource( b, monitor, _tempAssembly );
                _tempAssembly.SourceBuilder.CreateSource( b );
                var g = new CodeGenerator();
                g.Modules.AddRange( _tempAssembly.SourceModules );
                var assemblies = _contractResult.Assemblies.Where( a => !a.IsDynamic )
                                    .Append( typeof( BindingFlags ).GetTypeInfo().Assembly );
                var result = g.Generate( b.ToString(), _tempAssembly.SaveFilePath, assemblies, new DefaultAssemblyResolver(), null );
                if( saveSource && result.Sources != null )
                {
                    File.WriteAllText( _tempAssembly.SaveFilePath + ".cs", result.Sources.Select( t => t.ToString() ).Concatenate( Environment.NewLine ) );
                }
                result.LogResult( monitor );
                return result.Success;
            }
            catch( Exception ex )
            {
                monitor.Error().Send( ex, $"While generating final assembly '{_tempAssembly.SaveFileName}' from source code." );
                return false;
            }
        }

        void GenerateContextSource(StringBuilder b, IActivityMonitor monitor, IDynamicAssembly a)
        {
            b.AppendLine( "using CK.Core;" );
            b.AppendLine( "using System;" );
            b.AppendLine( "using System.Collections.Generic;" );
            b.AppendLine( "using System.Linq;" );
            b.AppendLine( "using System.Text;" );
            b.AppendLine( "using System.Reflection;" );
            b.AppendLine( "namespace CK.StObj {" );

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
            b.AppendLine( sourceGStObj );

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
            b.AppendLine( sourceGContext );
            #endregion

            b.AppendLine( "public class GeneratedRootContext : IStObjMap {" );
            b.AppendLine( "readonly GContext[] _contexts;" );
            b.AppendLine( "internal readonly GStObj[] _stObjs;" );
            b.AppendLine( "public GeneratedRootContext(IActivityMonitor monitor, IStObjRuntimeBuilder rb) {" );

            b.AppendLine( $"_stObjs = new GStObj[{_orderedStObjs.Count}];" );
            int iStObj = 0;
            foreach( var m in _orderedStObjs )
            {
                string generalization = m.Generalization == null ? "null" : $"_stObjs[{m.Generalization.IndexOrdered}]";
                string typeName = m.ObjectType.ToCSharpName();
                string actualTypeName = m.Specialization == null 
                                            ? "typeof("+m.GetFinalTypeCSharpName( monitor, a )+")"
                                            : "null";
                b.Append( $"_stObjs[{iStObj++}] = new GStObj(" );
                b.AppendLine( $"rb,typeof({typeName}),{generalization},{actualTypeName});" );
            }

            b.AppendLine( $"_contexts = new GContext[{Contexts.Count}];" );
            int iContext = 0;
            foreach( var ctx in Contexts )
            {
                b.AppendLine( $"Dictionary<Type,GStObj> map = new Dictionary<Type,GStObj>();" );
                IDictionary all = ctx.InternalMapper.RawMappings;
                // We skip highest implementation Type mappings (ie. AmbientContractInterfaceKey keys) since 
                // there is no ToStObj mapping (to root generalization) on final (runtime) IContextualStObjMap.
                var typeMapping = all.Cast<KeyValuePair<object, MutableItem>>().Where( e => e.Key is Type );
                foreach( var e in typeMapping )
                {
                    b.AppendLine( $"map.Add( typeof({((Type)e.Key).ToCSharpName()}), _stObjs[{e.Value.IndexOrdered}] );" );
                }
                b.AppendLine( $"_contexts[{iContext++}] = new GContext( this, map, {ctx.Context.ToSourceString()} );" );
            }
            b.AppendLine( "Default = _contexts[0];" );

            b.AppendLine( $"int iStObj = {_orderedStObjs.Count};" );
            b.AppendLine( "while( --iStObj >= 0 ) {" );
            b.AppendLine( " var o = _stObjs[iStObj];" );
            b.AppendLine( " if( o.Specialization == null ) {" );
            b.AppendLine( "  GStObj g = (GStObj)o.Generalization;" );
            b.AppendLine( "  while( g != null ) {" );
            b.AppendLine( "   g.Specialization = o;" );
            b.AppendLine( "   g.Instance = o.Instance;" );
            b.AppendLine( "   g.Context = o.Context;" );
            b.AppendLine( "   g.Leaf = o.Leaf;" );
            b.AppendLine( "   o = g;" );
            b.AppendLine( "   g = (GStObj)o.Generalization;" );
            b.AppendLine( "  }" );
            b.AppendLine( " }" );
            b.AppendLine( "}" );
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
                            b.Append( "PropertyInfo " )
                                .Append( varName )
                                .Append( "=" )
                                .Append( "typeof(" )
                                .AppendCSharpName( decl )
                                .Append( ").GetProperty(" )
                                .AppendSourceString( setter.Property.Name )
                                .AppendLine( ",BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic);" );
                            propertyCache.Add( key, varName );

                        }
                        b.Append(varName).Append( ".SetValue(_stObjs[" ).Append( m.IndexOrdered).Append( "].Instance," );
                        GenerateValue( b, setter.Value );
                        b.AppendLine( ");" );
                    }
                }
                var mConstruct = m.AmbientTypeInfo.StObjConstruct;
                if( mConstruct != null )
                {
                    b.Append( $"_stObjs[{m.IndexOrdered}].ObjectType.GetMethod( \"{mConstruct.Name}\", BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic )" );
                    b.Append( $".Invoke( _stObjs[{m.IndexOrdered}].Instance, " );
                    if( m.ConstructParameters.Count == 0 ) b.Append( "Array.Empty<object>()" );
                    else
                    {
                        b.Append( "new object[] {" );
                        // Missing Value {get;} on IStObjMutableParameter. We cast for the moment.
                        foreach( var p in m.ConstructParameters.Cast<MutableParameter>() )
                        {
                            if( p.BuilderValueIndex < 0 )
                            {
                                b.Append( $"_stObjs[{-(p.BuilderValueIndex + 1)}].Instance" );
                            }
                            else GenerateValue( b, p.Value );
                            b.Append( ',' );
                        }
                        b.Append( "}" );
                    }
                    b.AppendLine( ");" );
                }
            }
            foreach( var m in _orderedStObjs )
            {
                if( m.PostBuildProperties != null )
                {
                    foreach( var p in m.PostBuildProperties )
                    {
                        Type decl = p.Property.DeclaringType;
                        b.Append( "typeof(" )
                         .AppendCSharpName( decl )
                         .Append( $").GetProperty( \"{p.Property.Name}\", BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic )" )
                         .Append( $".SetValue(_stObjs[{m.IndexOrdered}].Instance, " );
                        GenerateValue( b, p.Value );
                        b.AppendLine( ");" );
                    }
                }
            }
            foreach( var m in _orderedStObjs )
            {
                MethodInfo init = m.ObjectType.GetMethod( StObjContextRoot.InitializeMethodName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic ); 
                if( init != null )
                {
                    b.Append( $"_stObjs[{m.IndexOrdered}].ObjectType.GetMethod( \"{StObjContextRoot.InitializeMethodName}\", BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic )" );
                    b.Append( $".Invoke( _stObjs[{m.IndexOrdered}].Instance, new object[]{{ monitor, _stObjs[{m.IndexOrdered}].Context }} );" );
                }
            }
            b.AppendLine( "}" );
            b.AppendLine( "public IEnumerable<StObjImplementation> AllStObjs => Contexts.SelectMany( c => c.StObjs );" );
            b.AppendLine( "public IContextualStObjMap Default { get; }" );
            b.AppendLine( "public IReadOnlyCollection<IContextualStObjMap> Contexts => _contexts;" );
            b.AppendLine( "public IContextualStObjMap FindContext( string context ) => Contexts.FirstOrDefault( c => ReferenceEquals( c.Context, context ?? String.Empty ) );" );
            b.AppendLine( "}}" );
        }

        static void CheckNotNullObject( string t, StringBuilder b )
        {
            b.AppendLine( $"if( {t} == null ) throw new Exception( \"NULL: \" + {t.ToSourceString()} );" );
        }

        static void GenerateValue( StringBuilder b, object o )
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
                b.AppendSourceString( o );
            }
        }

    }
}




