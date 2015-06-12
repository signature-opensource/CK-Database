using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using CK.Core;
using System.Diagnostics;

namespace CK.SqlServer.Parser.Tests.Parsing
{
    
    class CSharper : SqlItemVisitor
    {
        TextWriter _sw;
        IndentedTextWriter _w;

        public CSharper()
        {
            _sw = new StringWriter();
            _w = new IndentedTextWriter( _sw, "  " );
        }

        public override SqlItem Visit( SqlExprStIf e )
        {
            _w.Write( "if( " );
            VisitItem( e.Condition );
            _w.WriteLine( " )" );
            _w.WriteLine( "{" );
            _w.Indent += 1;
            VisitItem( e.ThenStatement );
            _w.Indent -= 1;
            _w.WriteLine( "}" );
            if( e.HasElse )
            {
                _w.WriteLine( "else" );
                _w.WriteLine( "{" );
                _w.Indent += 1;
                VisitItem( e.ElseStatement );
                _w.Indent -= 1;
                _w.WriteLine( "}" );
            }
            return e;
        }

        public override SqlItem Visit( SqlExprUnaryOperator e )
        {
            _w.Write( e.OperatorT.ToString() );
            VisitItem( e.Expression );
            return e;
        }

        public override SqlItem Visit( SqlExprKoCall e )
        {
            _w.Write( e.FunName );
            _w.Write( "(" );
            int count = 0;
            foreach( var p in e.Parameters )
            {
                _w.Write( count > 0 ? ", " : " " );
                VisitItem( p );
                ++count;
            }
            _w.Write( count > 0 ? " )" : ")" );
            return e;
        }

        public override SqlItem Visit( SqlExprBinaryOperator e )
        {
            for( int i = 0; i < e.Opener.Count; ++i ) _w.Write( "(" );
            VisitItem( e.Left );
            _w.Write( " " );
            string op;
            switch( e.Operator.TokenType )
            {
                case SqlTokenType.Equal: op = "=="; break;
                case SqlTokenType.And: op = "&&"; break;
                case SqlTokenType.Or: op = "||"; break;
                case SqlTokenType.NotEqualTo: op = "!="; break;
                default: op = e.Operator.ToString(); break;
            }
            _w.Write( op );
            _w.Write( " " );
            VisitItem( e.Right );
            if( e.Closer.Count > 0 ) _w.Write( ")" );
            return e;
        }

        public override SqlItem Visit( SqlExprDeclare e )
        {
            Type t = e.Variable.TypeDecl.ActualType.BestNetType();
            if( t != null ) _w.Write( t.Name );
            else
            {
                _w.Write( "/* Unsuported: " );
                _w.Write( e.Variable.ToStringClean() );
                _w.WriteLine( "*/" );
            }
            _w.Write( " " );
            _w.Write( MapVariableName( e.Variable.Identifier.Name ) );
            _w.WriteLine( ";" );
            return e;
        }

        public override SqlItem Visit( SqlExprBetween e )
        {
            _w.Write( "(" );
            VisitItem( e.Left );
            _w.Write( " >= " );
            VisitItem( e.Start );
            _w.Write( " && " );
            VisitItem( e.Left );
            _w.Write( " <= " );
            VisitItem( e.Stop );
            _w.Write( ")" );
            return e;
        }

        public override SqlItem Visit( SqlExprStLabelDef e )
        {
            _w.WriteLine( MapLabelName( e.IdentifierT.Name ) + ':' );
            return e;
        }

        public override SqlItem Visit( SqlExprStGoto e )
        {
            _w.Write( "goto " );
            _w.Write( MapLabelName( e.Target.Name ) );
            _w.WriteLine( ';' );
            return e;
        }

        public override SqlItem Visit( SqlExprStSetVar e )
        {
            _w.Write( MapVariableName( e.Variable.Name ) );
            _w.Write( " = " );
            VisitItem( e.Value );
            _w.WriteLine( ";" );
            return e;
        }

        public override SqlItem Visit( SqlExprIdentifier e )
        {
            if( e.IsVariable ) _w.Write( MapVariableName( e.Name ) );
            return e;
        }

        string MapVariableName( string v )
        {
            Debug.Assert( v[0] == '@' );
            return StdMapName( v.Substring( 1 ) );
        }

        string MapLabelName( string label )
        {
            return StdMapName( label );
        }

        static string StdMapName( string v )
        {
            v = v.Replace( "$", "_" );
            return v;
        }

        public override SqlItem Visit( SqlExprLiteral e )
        {
            _w.Write( e.Token.LiteralValue );
            return e;
        }

        public override SqlItem Visit( SelectSpecification e )
        {
            _w.Indent += 2;
            _w.WriteLine( "/* Select:" );
            _w.WriteLine( e.ToString() );
            _w.Indent -= 2;
            _w.WriteLine( "*/" );
            return e;
        }

        public override string ToString()
        {
            return _sw.ToString();
        }

    }


    [TestFixture]
    public class RewritingTests
    {
        [TestCase( "CLASSEMENT.sql" )]
        [TestCase( "CLASSEMENT_POSTE.sql" )]
        public void parsing_big_sp( string fileName )
        {
            SqlExprStStoredProc sp = SqlAnalyserTest.ReadStatement<SqlExprStStoredProc>( fileName );
            TestHelper.ConsoleMonitor.Trace().Send( sp.ToString() );
            var v = new CSharper();
            v.VisitItem( sp );
            TestHelper.ConsoleMonitor.Trace().Send( v.ToString() );
        }

    }
}
