using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection.Emit;
using System.Reflection;

namespace CK.Core
{
    public interface ITypeImplementationContributor
    {
        IEnumerable<MemberInfo> GetHandledImplementations( IActivityLogger logger, Type abstractType );

        void ContributeToImplementation( IActivityLogger logger, Type abstractType, TypeBuilder b ); 
    }
}
