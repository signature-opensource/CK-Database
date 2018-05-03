using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SqlCallDemo.PocoSupport
{
    public interface IThingReadOnlyProp : IThing
    {
        int ReadOnlyProp { get; }
    }
}
