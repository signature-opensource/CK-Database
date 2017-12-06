using System;
using System.Collections;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Reflection;
using System.Reflection.Emit;
using System.Linq;
using CK.Core;
using CK.Setup;
using CK.SqlServer.Parser;
using CK.Reflection;
using CK.CodeGen;

namespace CK.SqlServer.Setup
{

    public partial class SqlCallableAttributeImpl : SqlBaseItemMethodAttributeImplBase
    {
        public SqlCallableAttributeImpl( SqlCallableAttributeBase a, ISqlServerParser parser )
            : base( a, parser, a.ObjectType )
        {
        }

        protected new SqlCallableAttributeBase Attribute => (SqlCallableAttributeBase)base.Attribute;

        /// <summary>
        /// Tests whether a type has a corresponding <see cref="SqlDbType"/>. 
        /// It is all the types that are mapped by <see cref="FromSqlDbTypeToNetType"/> except <see cref="object"/> 
        /// plus <see cref="char"/> and enum (provided their underlying type is mapped) and 
        /// any <see cref="Nullable{T}"/> where T is mapped.
        /// </summary>
        /// <param name="t">Type to challenge.</param>
        /// <returns>True if this type can be mapped to a basic Sql type.</returns>
        static public bool IsNetTypeMapped(Type t)
        {
            if (t == null) throw new ArgumentNullException();
            if (t == typeof(object)) return false;
            var nT = Nullable.GetUnderlyingType(t);
            if (nT != null) t = nT;
            if (t == typeof(char)) return true;
            if (t.GetTypeInfo().IsEnum) t = t.GetTypeInfo().GetEnumUnderlyingType();
            return SqlHelper.HasDirectMapping(t);
        }

    }
}
