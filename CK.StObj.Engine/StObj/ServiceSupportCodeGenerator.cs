using CK.CodeGen;
using CK.CodeGen.Abstractions;
using CK.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CK.Setup
{
    class ServiceSupportCodeGenerator
    {
        static readonly string _sourceServiceSupport = @"
        public class StObjServiceParameterInfo : IStObjServiceParameterInfo
        {
            public StObjServiceParameterInfo( Type t, int p, string n, IStObjServiceClassFactoryInfo v )
            {
                ParameterType = t;
                Position = p;
                Name = n;
                Value = v;
            }

            public Type ParameterType { get; }

            public int Position { get; }

            public string Name { get; }

            public IStObjServiceClassFactoryInfo Value { get; }
        }

        public class StObjServiceClassFactoryInfo : IStObjServiceClassFactoryInfo
        {
            public StObjServiceClassFactoryInfo( Type t, IReadOnlyList<IStObjServiceParameterInfo> a )
            {
                ClassType = t;
                Assignments = a;
            }

            public Type ClassType { get; }

            public IReadOnlyList<IStObjServiceParameterInfo> Assignments { get; }
        }
";
        readonly ITypeScope _rootType;
        readonly IFunctionScope _rootCtor;
        readonly ITypeScope _infoType;
        readonly Dictionary<IStObjServiceClassFactoryInfo, string> _names;

        public ServiceSupportCodeGenerator( ITypeScope rootType, IFunctionScope rootCtor )
        {
            _rootType = rootType;
            _infoType = rootType.Namespace.CreateType( "public static class SFInfo" );
            _rootCtor = rootCtor;
            _names = new Dictionary<IStObjServiceClassFactoryInfo, string>();
        }

        public void CreateServiceSupportCode( StObjObjectEngineMap liftedMap )
        {
            _infoType.Namespace.Append( _sourceServiceSupport );

            _rootType.Append( @"
Dictionary<Type, Type> _simpleServiceMappings;
Dictionary<Type, IStObjServiceClassFactory> _manualServiceMappings;

public IStObjServiceMap Services => this;
IReadOnlyDictionary<Type, Type> IStObjServiceMap.SimpleMappings => _simpleServiceMappings;
IReadOnlyDictionary<Type, IStObjServiceClassFactory> IStObjServiceMap.ManualMappings => _manualServiceMappings;" )
                     .NewLine();

            // Service mappings (Simple).
            _rootCtor.Append( $"_simpleServiceMappings = new Dictionary<Type, Type>();" ).NewLine();
            foreach( var map in liftedMap.ServiceSimpleMappings )
            {
                _rootCtor.Append( $"_simpleServiceMappings.Add( " )
                       .AppendTypeOf( map.Key )
                       .Append( ", " )
                       .AppendTypeOf( map.Value.FinalType )
                       .Append( " );" )
                       .NewLine();
            }
            // Service mappings (Not so Simple :)).
            _rootCtor.Append( $"_manualServiceMappings = new Dictionary<Type, IStObjServiceClassFactory>();" ).NewLine();
            foreach( var map in liftedMap.ServiceManualMappings )
            {
                _rootCtor.Append( $"_manualServiceMappings.Add( " )
                       .AppendTypeOf( map.Key )
                       .Append( ", " ).Append( GetServiceClassFactoryName( map.Value ) )
                       .Append( " );" ).NewLine();
            }

            foreach( var serviceFactory in liftedMap.ServiceManualList )
            {
                CreateServiceClassFactory( serviceFactory );
            }
        }

        string GetServiceClassFactoryName( IStObjServiceFinalManualMapping f ) => $"SFInfo.S{f.Number}.Default";

        void CreateServiceClassFactory( IStObjServiceFinalManualMapping f )
        {
            var t = _infoType.CreateType( $"public class S{f.Number} : StObjServiceClassFactoryInfo, IStObjServiceClassFactory" );

            t.CreateFunction( ctor =>
            {
                ctor.Append( "public S" ).Append( f.Number ).Append( "()" ).NewLine()
                    .Append( ": base( " ).AppendTypeOf( f.ClassType ).Append( ", " ).NewLine();
                GenerateStObjServiceFactortInfoAssignments( ctor, f.Assignments );
                ctor.Append( ")" );
            } );

            t.CreateFunction( func =>
            {
                func.Append( "public object CreateInstance( IServiceProvider p ) {" );
                var locals = func.CreatePart();
                func.Append( "return " );
                var cache = new Dictionary<IStObjServiceClassFactoryInfo, string>();
                GenerateNew( f, locals, func, cache );
                func.Append( ";" ).NewLine();
            } );
            t.Append( "public static readonly IStObjServiceClassFactory Default = new S" ).Append( f.Number ).Append( "();" ).NewLine();
        }

        void GenerateNew( IStObjServiceClassFactoryInfo c, IFunctionScopePart locals, IFunctionScope func, Dictionary<IStObjServiceClassFactoryInfo, string> cache )
        {
            func.Append( "new " ).AppendCSharpName( c.ClassType ).Append( "(" );
            var ctor = c.GetSingleConstructor();
            var parameters = ctor.GetParameters();
            for( int i = 0; i < parameters.Length; ++i )
            {
                var p = parameters[i];
                if( i > 0 ) func.Append( ", " );
                var mapped = c.Assignments.Where( a => a.Position == p.Position ).FirstOrDefault();
                if( mapped == null )
                {
                    func.Append( "p.GetService( " ).AppendTypeOf( p.ParameterType ).Append( ")" );
                }
                else
                {
                    if( mapped.Value == null )
                    {
                        func.Append( "null" );
                    }
                    else
                    {
                        func.Append( GetLocalName( mapped.Value, locals, cache ) );
                    }
                }
            }
            func.Append( ")" );
        }

        private string GetLocalName( IStObjServiceClassFactoryInfo c, IFunctionScopePart part, Dictionary<IStObjServiceClassFactoryInfo, string> cache )
        {
            if( !cache.TryGetValue( c, out var name ) )
            {
                var locals = part.CreatePart();
                name = $"v{cache.Count}";
                cache.Add( c, name );
                part.Append( "var " ).Append( name ).Append( " = " );
                GenerateNew( c, locals, part, cache );
                part.Append( ";" ).NewLine();
            }
            return name;
        }

        string GetInfoName( IStObjServiceClassFactoryInfo info )
        {
            if( info == null ) return "null";
            if( !_names.TryGetValue( info, out var n ) )
            {
                n = $"I{_names.Count}";
                _names.Add( info, n );
                var part = _infoType.CreatePart( top: true );
                part.Append( "static readonly StObjServiceClassFactoryInfo " ).Append( n )
                         .Append( " = new StObjServiceClassFactoryInfo( " )
                         .AppendTypeOf( info.ClassType ).Append( ", " );
                GenerateStObjServiceFactortInfoAssignments( part, info.Assignments );
                part.Append( ");" ).NewLine();
            }
            return n;
        }

        void GenerateStObjServiceFactortInfoAssignments( ICodeWriter b, IReadOnlyList<IStObjServiceParameterInfo> assignments )
        {
            if( assignments.Count == 0 )
            {
                b.Append( "Array.Empty<StObjServiceParameterInfo>()" );
            }
            else
            {
                b.Append( "new[]{" ).NewLine();
                bool atLeastOne = false;
                foreach( var a in assignments )
                {
                    if( atLeastOne ) b.Append( ", " );
                    atLeastOne = true;
                    b.Append( "new StObjServiceParameterInfo( " )
                     .AppendTypeOf( a.ParameterType ).Append( ", " )
                     .Append( a.Position ).Append( ", " )
                     .AppendSourceString( a.Name ).Append( ", " )
                     .Append( GetInfoName( a.Value ) ).Append( ')' )
                     .NewLine();
                }
                b.Append( '}' );
            }
        }

    }
}
