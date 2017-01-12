using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SqlCallDemo.PocoSupport
{
    public interface IThingWithAgeAndHeight : IThing
    {
        int Age { get; set; }

        int Height { get; set; }
    }
}
