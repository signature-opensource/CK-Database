using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using CK.Core;

namespace CK.SqlServer.Parser
{
    /// <summary>
    /// Captures a select specification (a <see cref="ISelectSpecification"/> object) followed by optional <see cref="SelectOrderBy"/>, 
    /// <see cref="SelectFor"/> and <see cref="SelectOption"/> clauses.
    /// </summary>
    public class SelectQuery : SqlExpr
    {
        readonly SelectOrderBy _orderBy;
        readonly SelectFor _forPart;
        readonly SelectOption _option;

        public SelectQuery( ISelectSpecification spec, SelectOrderBy orderBy = null, SelectFor forPart = null, SelectOption option = null )
            : this( Build( spec, orderBy, forPart, option ) )
        {
        }

        static ISqlItem[] Build( ISelectSpecification spec, SelectOrderBy orderBy, SelectFor forPart, SelectOption option )
        {
            if( spec == null ) throw new ArgumentNullException( "spec" );
            var c = new List<ISqlItem>();
            c.Add( SqlToken.EmptyOpenPar );
            c.Add( spec );
            if( orderBy != null ) c.Add( orderBy );
            if( forPart != null ) c.Add( forPart );
            if( option != null ) c.Add( option );
            c.Add( SqlToken.EmptyClosePar );
            return c.ToArray();
        }

        internal SelectQuery( ISqlItem[] slots )
            : base( slots )
        {
            _orderBy = Slots.OfType<SelectOrderBy>().FirstOrDefault();
            _forPart = Slots.OfType<SelectFor>().FirstOrDefault();
            _option = Slots.OfType<SelectOption>().FirstOrDefault();
        }

        public ISelectSpecification Specification { get { return (ISelectSpecification)Slots[1]; } }

        public SelectOrderBy Orderby { get { return _orderBy; } }

        [DebuggerStepThrough]
        internal protected override T Accept<T>( ISqlItemVisitor<T> visitor )
        {
            return visitor.Visit( this );
        }

    }
}
