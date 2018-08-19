using CK.SqlServer.Parser;
using System;
using System.Reflection;

namespace CK.SqlServer.Setup
{
    /// <summary>
    /// Implementation of <see cref="SqlCallableAttributeBase"/>.
    /// </summary>
    public partial class SqlCallableAttributeImpl : SqlBaseItemMethodAttributeImplBase
    {
        /// <summary>
        /// Initializes a new <see cref="SqlCallableAttributeImpl"/>.
        /// </summary>
        /// <param name="a">The attribute.</param>
        /// <param name="parser">The required parser that will be used.</param>
        public SqlCallableAttributeImpl( SqlCallableAttributeBase a, ISqlServerParser parser )
            : base( a, parser, a.ObjectType )
        {
        }

        /// <summary>
        /// Gets the <see cref="SqlCallableAttributeBase"/> attribute.
        /// </summary>
        protected new SqlCallableAttributeBase Attribute => (SqlCallableAttributeBase)base.Attribute;

        /// <summary>
        /// Tests whether a type has a corresponding <see cref="System.Data.SqlDbType"/>. 
        /// It is all the types that are mapped by <see cref="SqlHelper.FromSqlDbTypeToNetType"/> except <see cref="object"/> 
        /// plus <see cref="char"/> and enum (provided their underlying type is mapped) and 
        /// any <see cref="Nullable{T}"/> where T is mapped.
        /// </summary>
        /// <param name="t">Type to challenge.</param>
        /// <returns>True if this type can be mapped to a basic Sql type.</returns>
        static public bool IsNetTypeMapped( Type t )
        {
            if( t == null ) throw new ArgumentNullException();
            if( t == typeof( object ) ) return false;
            var nT = Nullable.GetUnderlyingType( t );
            if( nT != null ) t = nT;
            if( t == typeof( char ) ) return true;
            if( t.IsEnum ) t = t.GetEnumUnderlyingType();
            return SqlHelper.HasDirectMapping( t );
        }

    }
}
