using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using CK.Core;

namespace CK.SqlServer
{
    public class SqlExprParameter : SqlNoExpr
    {
        public SqlExprParameter( SqlExprTypedIdentifier declVar, SqlExprParameterDefaultValue defaultValue = null, SqlTokenIdentifier outputClause = null, SqlTokenIdentifier readonlyClause = null )
            : this( Build( declVar, defaultValue, outputClause, readonlyClause ) )
        {
        }

        static ISqlItem[] Build( SqlExprTypedIdentifier declVar, SqlExprParameterDefaultValue defaultValue, SqlTokenIdentifier outputClause, SqlTokenIdentifier readonlyClause )
        {
            if( declVar == null ) throw new ArgumentNullException( "declVar" );
            if( !declVar.Identifier.IsVariable ) throw new ArgumentException( "Must be a @VariableName", "variable" );
            if( outputClause != null
                && String.Compare( outputClause.Name, "out", StringComparison.OrdinalIgnoreCase ) != 0
                && String.Compare( outputClause.Name, "output", StringComparison.OrdinalIgnoreCase ) != 0 )
            {
                throw new ArgumentException( "Must be out or output.", "outputClause" );
            }
            if( readonlyClause != null
                && String.Compare( outputClause.Name, "readonly", StringComparison.OrdinalIgnoreCase ) != 0 )
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
                        return CreateArray( declVar );
                    }
                    else
                    {
                        return CreateArray( declVar, readonlyClause );
                    }
                }
                else
                {
                    if( readonlyClause == null )
                    {
                        return CreateArray( declVar, outputClause );
                    }
                    else
                    {
                        return CreateArray( declVar, outputClause, readonlyClause );
                    }
                }
            }
            else
            {
                if( outputClause == null )
                {
                    if( readonlyClause == null )
                    {
                        return CreateArray( declVar, defaultValue );
                    }
                    else
                    {
                        return CreateArray( declVar, defaultValue, readonlyClause );
                    }
                }
                else
                {
                    if( readonlyClause == null )
                    {
                        return CreateArray( declVar, defaultValue, outputClause );
                    }
                    else
                    {
                        return CreateArray( declVar, defaultValue, outputClause, readonlyClause );
                    }
                }
            }
        }

        internal SqlExprParameter( ISqlItem[] items )
            : base( items )
        {
        }

        public SqlExprTypedIdentifier Variable { get { return (SqlExprTypedIdentifier)Slots[0]; } }

        public SqlExprParameterDefaultValue DefaultValue { get { return Slots.Length > 1 ? Slots[1] as SqlExprParameterDefaultValue : null; } }

        /// <summary>
        /// Gets whether the parameter is a pure input parameter or an output one with a /*input*/ tag.
        /// </summary>
        public bool IsInput { get { return OutputToken == null || IsInputOutput; } }
        
        public bool IsOutput { get { return OutputToken != null; } }

        public bool IsInputOutput 
        { 
            get 
            {
                if( OutputToken == null ) return false;
                return Tokens.SelectMany( t => t.LeadingTrivia.Concat( t.TrailingTrivia ).Where( trivia => trivia.TokenType != SqlTokenType.None ) ).Any( trivia => trivia.Text.Contains( "input" ) );
            } 
        }
        
        public bool IsReadOnly { get { return ReadOnlyToken != null; } }

        public SqlTokenIdentifier ReadOnlyToken { get { var t = LastTokenClause; return t != null && t.NameEquals( "readonly" ) ? t : null; } }

        public SqlTokenIdentifier OutputToken 
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

        SqlTokenIdentifier LastTokenClause { get { return Slots.Length > 1 ? Slots[Slots.Length - 1] as SqlTokenIdentifier : null; } }

        SqlTokenIdentifier AnteLastTokenClause { get { return Slots.Length > 2 ? Slots[Slots.Length - 2] as SqlTokenIdentifier : null; } }

        public string ToStringClean()
        {
            string s = Variable.ToStringClean();
            if( DefaultValue != null ) s += " " + DefaultValue.Tokens.ToStringWithoutTrivias( " " );
            if( IsOutput )
            {
                if( IsInputOutput ) s += " /*input*/output";
                else s += " output";
            }
            if( IsReadOnly ) s += " readonly";
            return s;
        }

        [DebuggerStepThrough]
        internal protected override T Accept<T>( IExprVisitor<T> visitor )
        {
            return visitor.Visit( this );
        }

    }

}
