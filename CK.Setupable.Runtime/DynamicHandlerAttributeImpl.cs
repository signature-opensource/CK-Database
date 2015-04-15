using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using CK.Core;

namespace CK.Setup
{
    /// <summary>
    /// 
    /// </summary>
    public class DynamicHandlerAttributeImpl : IAttributeAmbientContextBoundInitializer
    {
        readonly DynamicHandlerAttribute Attribute;
        MemberInfo _m;

        public DynamicHandlerAttributeImpl( DynamicHandlerAttribute a )
        {
            Attribute = a;
        }

        void IAttributeAmbientContextBoundInitializer.Initialize( ICKCustomAttributeTypeMultiProvider owner, MemberInfo m )
        {
            _m = m;
        }
    }
}
