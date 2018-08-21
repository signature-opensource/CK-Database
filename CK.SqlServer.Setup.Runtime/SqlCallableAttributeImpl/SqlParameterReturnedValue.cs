using CK.SqlServer.Parser;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CK.SqlServer.Setup
{
    /// <summary>
    /// Models the returned parameter for scalar functions. 
    /// It is always the first one in the parameter list: the SqlCommand is generated with this RETURN_VALUE
    /// as its first value and the executor takes it as the first parameter.
    /// </summary>
    class SqlParameterReturnedValue : ISqlServerParameter
    {
        public SqlParameterReturnedValue( ISqlServerUnifiedTypeDecl type )
        {
            SqlType = type;
        }

        public ISqlServerParameterDefaultValue DefaultValue => null;

        public bool IsInput => false;

        public bool IsInputOutput => false;

        public bool IsOutput => true;

        public bool IsPureInput => false;

        public bool IsPureOutput => true;

        public bool IsReadOnly => false;

        public string Name => "RETURN_VALUE";

        public ISqlServerUnifiedTypeDecl SqlType { get; }

        public bool IsNotNull => false;

        public string ToStringClean() => "(return)";
    }

}
