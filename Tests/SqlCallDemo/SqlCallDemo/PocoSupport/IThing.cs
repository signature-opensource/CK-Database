using CK.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SqlCallDemo
{
    public interface IThing : IPoco
    {
        string Name { get; set; }

        Guid FromBatabaseOnly { get; }
    }
}
