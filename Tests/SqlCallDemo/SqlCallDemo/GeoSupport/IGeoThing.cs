using CK.Core;
using Microsoft.SqlServer.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SqlCallDemo
{
    public interface IGeoThing : IPoco
    {
        string Name { get; set; }

        SqlGeography Geo { get; set; } 
    }
}
