using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using CK.Core;
using System.Reflection;
using System.Reflection.Emit;

namespace CK.Setup.StObj.Tests
{

    [AttributeUsage( AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = true, Inherited = false )]
    public class AutoImplementMethodAttribute : Attribute, IAutoImplementorMethod
    {
        public bool Implement( IActivityLogger logger, MethodInfo m, TypeBuilder b )
        {
            return false;
        }
    }

    [TestFixture]
    public class AutoImplementationTests
    {

        abstract class ABase
        {
            protected abstract int OneProperty { get; set; }

            [AutoImplementMethod]
            protected abstract int FirstMethod( int i );
        }

        abstract class A : ABase, IAmbientContract
        {
            [AutoImplementMethod]
            public abstract string SecondMethod( int i );
        }

        abstract class A2 : A
        {
            [AutoImplementMethod]
            public abstract A ThirdMethod( int i, string s );
        }

        [Test]
        public void AbstractDetection()
        {
            Type t = typeof( A2 );

            var candidates = t.GetMethods( BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public ).Where( m => !m.IsSpecialName && m.IsAbstract );
            int nbUncovered = 0;
            List<KeyValuePair<MethodInfo,IAutoImplementorMethod>> implMap = new List<KeyValuePair<MethodInfo, IAutoImplementorMethod>>();
            foreach( var m in candidates )
            {
                ++nbUncovered;
                var c = (IAutoImplementorMethod[])m.GetCustomAttributes( typeof( IAutoImplementorMethod ), false );
                if( c.Length > 0 )
                {
                    --nbUncovered;
                    for( int i = 0; i < c.Length; ++i  )
                    {
                        implMap.Add( new KeyValuePair<MethodInfo,IAutoImplementorMethod>( m, c[i] ) );
                    }
                }
            }
        }

    }
}
