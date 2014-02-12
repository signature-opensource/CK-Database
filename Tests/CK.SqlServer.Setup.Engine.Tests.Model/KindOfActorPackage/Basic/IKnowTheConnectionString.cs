using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SqlActorPackage.Basic
{
    /// <summary>
    /// This interface is implemented by Basic.Package to show the injection of the declaring type into the command wrapper constructor.
    /// </summary>
    public interface IKnowTheConnectionString
    {
        string GetConnectionString();
    }
}
