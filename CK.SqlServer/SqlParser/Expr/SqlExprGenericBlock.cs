using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using CK.Core;

namespace CK.SqlServer
{
    /// <summary>
    /// Generic structure element: it is a list of <see cref="IAbstractExpr"/> (can be <see cref="SqlToken"/> or <see cref="SqlExpr"/>) 
    /// that can be enclosed in parenthesis.
    /// </summary>
    public sealed class SqlExprGenericBlock : SqlExpr, ISqlExprEnclosable
    {
        readonly IAbstractExpr[] _components;

        /// <summary>
        /// Initializes a new block without any opener/closer parenthesis.
        /// </summary>
        /// <param name="componentsOnly">List of <see cref="SqlToken"/> or <see cref="SqlExpr"/> that compose this block.</param>
        public SqlExprGenericBlock( IList<IAbstractExpr> componentsOnly )
        {
            if( componentsOnly == null ) throw new ArgumentNullException( "components" );
            _components = CreateArray( SqlExprMultiToken<SqlTokenOpenPar>.Create(), componentsOnly, 0, componentsOnly.Count, SqlExprMultiToken<SqlTokenClosePar>.Create() );
        }

        /// <summary>
        /// Initializes a new block that is enclosed in a pair of opener/closer parenthesis.
        /// </summary>
        /// <param name="openPar">Opening parenthesis.</param>
        /// <param name="components">
        /// List of <see cref="SqlToken"/> or <see cref="SqlExpr"/> that compose this block. 
        /// This MUST not contain the <see cref="Opener"/> and/or the <see cref="Closer"/>.</param>
        /// <param name="closePar">Closing parentehsis.</param>
        public SqlExprGenericBlock( SqlTokenOpenPar openPar, IList<IAbstractExpr> components, SqlTokenClosePar closePar )
        {
            if( openPar == null ) throw new ArgumentNullException( "openPar" );
            if( components == null ) throw new ArgumentNullException( "components" );
            if( closePar == null ) throw new ArgumentNullException( "closePar" );
            _components = CreateArray( openPar, components, components.Count, closePar );
        }

        internal SqlExprGenericBlock( IAbstractExpr[] newComponents )
        {
            Debug.Assert( newComponents != null );
            _components = newComponents;
        }

        /// <summary>
        /// Gets the opening parenthesis (can be empty).
        /// </summary>
        public SqlExprMultiToken<SqlTokenOpenPar> Opener
        {
            get { return (SqlExprMultiToken<SqlTokenOpenPar>)_components[0]; }
        }

        /// <summary>
        /// Gets the closing parenthesis (can be empty).
        /// </summary>
        public SqlExprMultiToken<SqlTokenClosePar> Closer
        {
            get { return (SqlExprMultiToken<SqlTokenClosePar>)_components[_components.Length-1]; }
        }

        public bool CanEnclose
        {
            get { return true; }
        }

        public ISqlExprEnclosable Enclose( SqlTokenOpenPar openPar, SqlTokenClosePar closePar )
        {
            return new SqlExprGenericBlock( CreateArray( openPar, _components, closePar ) );
        }

        public IEnumerable<IAbstractExpr> ComponentsWithoutParenthesis
        {
            get { return _components.Skip( 1 ).Take( _components.Length - 2 ); }
        }

        public override IEnumerable<IAbstractExpr> Components { get { return _components; } }

        [DebuggerStepThrough]
        internal protected override T Accept<T>( IExprVisitor<T> visitor )
        {
            return visitor.Visit( this );
        }

    }


}
