using CK.CodeGen;
using CK.Text;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using Microsoft.CodeAnalysis;
using CK.CodeGen.Abstractions;

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

            public IReadOnlyList<SyntaxTree> Rewrite( IReadOnlyList<SyntaxTree> trees ) => trees;

            public void Inject( ICodeWorkspace code )
            {
                if( _r.AllInterfaces.Count == 0 ) return;
                var b = code.Global
                                .FindOrCreateNamespace( _r.FinalFactory.Namespace )
                                .EnsureUsing( "System" );
                foreach( var root in _r.Roots )
                {
                    var tB = b.CreateType( t => t.Append( "class " )
                                                 .Append( root.PocoClass.Name )
                                                 .Append( " : " )
                                                 .Append( root.Interfaces.Select( i => i.PocoInterface.ToCSharpName() ) ) );
                    foreach( var p in root.PocoClass.GetTypeInfo().GetProperties() )
                    {
                        tB.Append("public " ).AppendCSharpName( p.PropertyType ).Space().Append( p.Name ).Append( "{" );
                        b.Append( "get;" );
                        if( p.CanWrite ) b.Append( "set;" );
                        b.Append( "}" ).NewLine();
                    }
                }
                var fB = b.CreateType( t => t.Append( "class " )
                                             .Append( _r.FinalFactory.Name )
                                             .Append( " : " )
                                             .Append( _r.AllInterfaces.Select( i => i.PocoFactoryInterface.ToCSharpName() ) ) );
                foreach( var i in _r.AllInterfaces )
                {
                    fB.AppendCSharpName( i.PocoInterface )
                      .Space()
                      .AppendCSharpName( i.PocoFactoryInterface )
                      .Append( ".Create() => new " ).AppendCSharpName( i.Root.PocoClass ).Append( "();" )
                      .NewLine();
                   fB.Append( "Type " )
                     .AppendCSharpName( i.PocoFactoryInterface )
                     .Append( ".PocoClassType => typeof(" ).AppendCSharpName( i.Root.PocoClass ).Append( ");" )
                     .NewLine();
                }
            }
        }

        public static ICodeGeneratorModule CreateModule( IPocoSupportResult r )
        {
            if( r == null ) throw new ArgumentNullException( nameof( r ) );
            return new Module( r );
        }
    }
}
