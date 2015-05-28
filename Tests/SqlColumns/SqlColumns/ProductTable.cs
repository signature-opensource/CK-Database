using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CK.Setup;
using CK.SqlServer.Setup;

namespace SqlColumns
{
    [SqlTable( "tProduct", Package=typeof(ProductPackage) ), Versions( "1.0.0" )]
    public class ProductTable : SqlTable
    {
    }
}
