using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CK.Core;

namespace CK.SqlServer.Setup
{
    /// <summary>
    /// Base class for package objects. 
    /// Unless marked with <see cref="IAmbientDefiner{T}"/>, direct specializations are de facto ambient objects.
    /// This class doesn't bring more than the base <see cref="SqlPackageBase"/>.
    /// </summary>
    public class SqlPackage : SqlPackageBase, IAmbientObject, IAmbientDefiner<SqlPackage>
    {
    }
}
