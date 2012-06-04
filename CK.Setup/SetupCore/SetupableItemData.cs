using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CK.Setup
{
    public class SetupableItemData
    {
        public string FullName;
        
        public IDependentItemContainerRef Container;
        
        public IEnumerable<string> Requires;
        
        public IEnumerable<string> RequiredBy;
        
        public Version Version;
        
        public IEnumerable<VersionedName> PreviousNames;
    }
}
