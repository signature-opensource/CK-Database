using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;

namespace CK.SqlServer
{
    /// <summary>
    /// Captures a select column definition. 
    /// </summary>
    public class SqlExprSelectColumn : SqlExpr
    {
        readonly IAbstractExpr[] _components;
        readonly SqlTokenIdentifier _colName;
        readonly SqlToken _asOrEqual;
        readonly SqlExpr _definition;

        public SqlExprSelectColumn( SqlTokenIdentifier colName, SqlTokenTerminal equalToken, SqlExpr definition )
        {
            if( colName == null ) throw new ArgumentNullException( "colName" );
            if( equalToken == null ) throw new ArgumentNullException( "equalToken" );
            if( equalToken.TokenType != SqlTokenType.Equal ) throw new ArgumentException( "Equal token expected.", "equalToken" );
            if( definition == null ) throw new ArgumentNullException( "definition" );
            _colName = colName;
            _asOrEqual = equalToken;
            _definition = definition;
            _components = BuildComponents();
        }

        public SqlExprSelectColumn( SqlExpr definition, SqlTokenIdentifier asToken, SqlTokenIdentifier colName )
        {
            if( definition == null ) throw new ArgumentNullException( "definition" );
            if( asToken == null ) throw new ArgumentNullException( "asToken" );
            if( !asToken.NameEquals( "as" ) ) throw new ArgumentException( "As token expected.", "equalToken" );
            if( colName == null ) throw new ArgumentNullException( "colName" );
            _colName = colName;
            _asOrEqual = asToken;
            _definition = definition;
            _components = BuildComponents();
        }

        public SqlExprSelectColumn( SqlExpr definition )
        {
            if( definition == null ) throw new ArgumentNullException( "definition" );
            _definition = definition;
            _components = BuildComponents();
        }

        internal SqlExprSelectColumn( IAbstractExpr[] newComponents )
        {
            _components = newComponents;
            if( _components.Length == 1 ) _definition = (SqlExpr)_components[0];
            else
            {
                _asOrEqual = (SqlToken)_components[1];
                if( _asOrEqual is SqlTokenTerminal )
                {
                    _colName = (SqlTokenIdentifier)_components[0];
                    _definition = (SqlExpr)_components[2];
                }
                else
                {
                    _colName = (SqlTokenIdentifier)_components[2];
                    _definition = (SqlExpr)_components[0];
                }
            }
        }

        IAbstractExpr[] BuildComponents()
        {
            if( IsAsSyntax ) return CreateArray( _definition, _asOrEqual, _colName );
            else if( IsEqualSyntax ) return CreateArray( _colName, _asOrEqual, _definition );
            else return CreateArray( _definition );
        }

        public SqlTokenIdentifier ColumnName { get { return _colName; } }

        public bool IsEqualSyntax { get { return _asOrEqual is SqlTokenTerminal; } }

        public bool IsAsSyntax { get { return _asOrEqual is SqlTokenIdentifier; } }

        public SqlToken AsOrEqual { get { return _asOrEqual; } }
        
        public SqlExpr Definition { get { return _definition; } }

        public override IEnumerable<IAbstractExpr> Components
        {
            get { return _components; }
        }

        [DebuggerStepThrough]
        internal protected override T Accept<T>( IExprVisitor<T> visitor )
        {
            return visitor.Visit( this );
        }
    }


}
