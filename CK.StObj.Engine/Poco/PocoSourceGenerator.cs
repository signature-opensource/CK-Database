using CK.CodeGen;
using CK.Text;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using Microsoft.CodeAnalysis;

namespace CK.Core
{
    public static class PocoSourceGenerator
    {
        class Module : ICodeGeneratorModule
        {
            readonly IPocoSupportResult _r;

            public Module( IPocoSupportResult r )
            {
                _r = r;
            }

            public IEnumerable<Assembly> RequiredAssemblies => Enumerable.Empty<Assembly>();

            public void AppendSource( StringBuilder b )
            {
                if( _r.AllInterfaces.Count == 0 ) return;
                b.Append( "namespace " ).Append( _r.FinalFactory.Namespace ).AppendLine( "{" );
                b.AppendLine( "using System;" );
                foreach( var root in _r.Roots )
                {
                    b.Append( "class " )
                     .Append( root.PocoClass.Name )
                     .Append( ':' )
                     .AppendStrings( root.Interfaces.Select( i => i.PocoInterface.ToCSharpName() ) )
                     .AppendLine( "{" );
                    foreach( var p in root.PocoClass.GetTypeInfo().GetProperties() )
                    {
                        b.Append("public " ).AppendCSharpName( p.PropertyType ).Append( ' ' ).Append( p.Name ).Append( '{' );
                        b.Append( "get;" );
                        if( p.CanWrite )
                        {
                            b.Append( "set;" );
                        }
                        b.AppendLine( "}" );
                    }
                    b.AppendLine( "}" );
                }
                b.Append( "class " )
                 .Append( _r.FinalFactory.Name )
                 .Append( ':' )
                 .AppendStrings( _r.AllInterfaces.Select( i => i.PocoFactoryInterface.ToCSharpName() ) )
                 .AppendLine( "{" );
                foreach( var i in _r.AllInterfaces )
                {
                    b.AppendCSharpName( i.PocoInterface ).Append( ' ' ).AppendCSharpName( i.PocoFactoryInterface ).Append( ".Create()" )
                        .Append( "=>new " ).AppendCSharpName( i.Root.PocoClass ).AppendLine( "();" );
                    b.Append( "Type " ).AppendCSharpName( i.PocoFactoryInterface ).Append( ".PocoClassType" )
                        .Append( "=>typeof(" ).AppendCSharpName( i.Root.PocoClass ).AppendLine( ");" );
                }
                b.AppendLine( "}" );
                b.AppendLine( "}" );
            }

            public SyntaxTree PostProcess( SyntaxTree t ) => t;
        }

        public static ICodeGeneratorModule CreateModule( IPocoSupportResult r )
        {
            if( r == null ) throw new ArgumentNullException( nameof( r ) );
            return new Module( r );
        }
    }
}
