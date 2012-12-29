using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;

namespace CK.Setup
{
    internal interface ICapturedBuild
    {
        void PushSetupLogger();
        void PushStObj( MutableItem o );
        void PushValue( object o );
        void PushCall( MutableItem o, MethodInfo m );
    }

}
