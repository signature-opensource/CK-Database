using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CK.Setup
{
    public interface ISetupableItemContainer : ISetupableItem, IDependentItemContainer
    {
    }
}
