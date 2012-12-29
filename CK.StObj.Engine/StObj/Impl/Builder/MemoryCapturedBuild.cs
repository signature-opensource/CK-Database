using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Runtime.Serialization;
using System.Reflection;

namespace CK.Setup
{
    class MemoryCapturedBuild : ICapturedBuild
    {
        interface IBuildOp
        {
            void Write( RawOutStream op );
        }

        class CPushSetupLogger : IBuildOp
        {
            public void Write( RawOutStream op )
            {
                op.WriteCode( KOpCode.PushLogger );
            }
        }

        class CPushStObj : IBuildOp
        {
            public MutableItem Object;
            
            public void Write( RawOutStream op )
            {
                op.WriteCode( KOpCode.PushStObj );
                op.WriteRef( Object );
            }
        }

        class CPushValue : IBuildOp
        {
            public object Object;

            public void Write( RawOutStream op )
            {
                op.WriteCode( KOpCode.PushValue );
                op.Serialize( Object );
            }
        }

        class CPushCall : IBuildOp
        {
            public MutableItem Object;
            public MethodInfo Method;

            public void Write( RawOutStream op )
            {
                op.WriteCode( KOpCode.PushCall );
                op.WriteRef( Object );
                op.Serialize( Method );
            }
        }

        readonly List<IBuildOp> _operations;

        public MemoryCapturedBuild()
        {
            _operations = new List<IBuildOp>();
        }

        public void PushSetupLogger()
        {
            _operations.Add( new CPushSetupLogger() );
        }

        public void PushStObj( MutableItem o )
        {
            _operations.Add( new CPushStObj() { Object = o } );
        }

        public void PushValue( object o )
        {
            _operations.Add( new CPushValue() { Object = o } );
        }

        public void PushCall( MutableItem o, MethodInfo m )
        {
            _operations.Add( new CPushCall() { Object = o, Method = m } );
        }
    }
}
