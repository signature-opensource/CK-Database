using FluentAssertions;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace CKSetup.Tests
{
    [TestFixture]
    public class SHA1Tests
    {
        static string GetFilePath( [CallerFilePath]string p = null ) => p;

        [Test]
        public void SHA1_ToString_and_Parse()
        {
            var sha = SHA1Value.ComputeFileSHA1( GetFilePath() );
            var s = sha.ToString();
            var shaBis = SHA1Value.Parse( s );
            shaBis.Should().Be( sha );
        }

    }
}
