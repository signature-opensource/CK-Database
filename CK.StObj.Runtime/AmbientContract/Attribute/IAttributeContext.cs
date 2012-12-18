using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CK.Core
{
    /// <summary>
    /// Interface that unifies <see cref="AddContextAttribute"/> and <see cref="RemoveContextAttribute"/>.
    /// </summary>
    public interface IAttributeContext
    {
        string Context { get; }
    }
}
