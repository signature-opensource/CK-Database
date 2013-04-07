using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using CK.Core;

namespace CK.SqlServer
{
    public class SqlExprParameter : SqlExpr
    {
        readonly IAbstractExpr[] _components;

        public SqlExprParameter( SqlExprTypedIdentifier declVar, SqlExprParameterDefaultValue defaultValue = null, SqlTokenIdentifier outputClause = null, SqlTokenIdentifier readonlyClause = null )
        {
            if( declVar == null ) throw new ArgumentNullException( "declVar" );
            if( !declVar.Identifier.IsVariable ) throw new ArgumentException( "Must be a @VariableName", "variable" );
            if( outputClause != null
                && (!outputClause.IsUnquotedKeyword
                        || (String.Compare( outputClause.Name, "out", StringComparison.OrdinalIgnoreCase ) != 0
                            && String.Compare( outputClause.Name, "output", StringComparison.OrdinalIgnoreCase ) != 0)) )
            {
                throw new ArgumentException( "Must be out or output.", "outputClause" );
            }
            if( readonlyClause != null
                && (!readonlyClause.IsUnquotedKeyword || String.Compare( outputClause.Name, "readonly", StringComparison.OrdinalIgnoreCase ) != 0 ) )
            {
                throw new ArgumentException( "Must be readonly.", "readonlyClause" );
            }
            //
            if( defaultValue == null )
            {
                if( outputClause == null )
                {
                    if( readonlyClause == null )
                    {
                        _components = CreateArray( declVar );
                    }
                    else
                    {
                        _components = CreateArray( declVar, readonlyClause );
                    }
                }
                else 
                {
                    if( readonlyClause == null )
                    {
                        _components = CreateArray( declVar, outputClause );
                    }
                    else
                    {
                        _components = CreateArray( declVar, outputClause, readonlyClause );
                    }
                }
            }
            else
            {
                if( outputClause == null )
                {
                    if( readonlyClause == null )
                    {
                        _components = CreateArray( declVar, defaultValue );
                    }
                    else
                    {
                        _components = CreateArray( declVar, defaultValue, readonlyClause );
                    }
                }
                else 
                {
                    if( readonlyClause == null )
                    {
                        _components = CreateArray( declVar, defaultValue, outputClause );
                    }
                    else
                    {
                        _components = CreateArray( declVar, defaultValue, outputClause, readonlyClause );
                    }
                }
            }
        }

        internal SqlExprParameter( IAbstractExpr[] newComponents )
        {
            _components = newComponents;
        }

        public SqlExprTypedIdentifier Variable { get { return (SqlExprTypedIdentifier)_components[0]; } }

        public SqlExprParameterDefaultValue DefaultValue { get { return _components.Length > 1 ? _components[1] as SqlExprParameterDefaultValue : null; } }

        public bool IsOutput { get { return OptionClause != null; } }

        public bool IsReadOnly { get { return ReadOnlyClause != null; } }

        public SqlTokenIdentifier ReadOnlyClause { get { var t = LastTokenClause; return t != null && t.NameEquals( "readonly" ) ? t : null; } }

        public SqlTokenIdentifier OptionClause 
        { 
            get 
            {
                var t = LastTokenClause;
                if( t == null ) return null;
                if( !t.NameEquals( "output" ) )
                {
                    t = AnteLastTokenClause;
                    Debug.Assert( t == null || t.NameEquals( "output" ) );
                }
                return t;
            } 
        }

        SqlTokenIdentifier LastTokenClause { get { return _components.Length > 1 ? _components[_components.Length - 1] as SqlTokenIdentifier : null; } }

        SqlTokenIdentifier AnteLastTokenClause { get { return _components.Length > 2 ? _components[_components.Length - 2] as SqlTokenIdentifier : null; } }

        public override sealed IEnumerable<IAbstractExpr> Components
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
