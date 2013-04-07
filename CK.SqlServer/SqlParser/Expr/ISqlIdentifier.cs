using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CK.Core;

namespace CK.SqlServer
{

    public interface ISqlIdentifier : IReadOnlyList<SqlTokenIdentifier>, IAbstractExpr
    {
        bool IsVariable { get; }
    }
}
