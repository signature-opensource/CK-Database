using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CK.Core
{
    /// <summary>
    /// Internal interface that unifies <see cref="AddContextAttribute"/> and <see cref="RemoveContextAttribute"/>.
    /// </summary>
    internal interface IContextDefiner
    {
        string Context { get; }
    }
}
