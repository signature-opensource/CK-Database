using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CK.SqlServer.Setup;

namespace SqlColumns
{
    [SqlPackage( Schema="Prd", ResourcePath="Res" )]
    public class ProductPackage : SqlPackage
    {
    }

}
