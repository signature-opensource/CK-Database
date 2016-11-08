using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CK.Core;

namespace CK.SqlServer.Setup
{
    /// <summary>
    /// Base class for package objects. 
    /// Sincer this class supports <see cref="IAmbientContractDefiner"/>, direct specializations
    /// are de facto ambient contracts.
    /// This class does not bring no more than the base <see cref="SqlPackageBase"/>.
    /// </summary>
    public class SqlPackage : SqlPackageBase, IAmbientContractDefiner
    {
    }
}
