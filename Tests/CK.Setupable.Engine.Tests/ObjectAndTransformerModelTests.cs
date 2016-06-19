using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CK.Setupable.Engine.Tests
{

    class O
    {
        readonly object _content;
        readonly O _previousT;
        readonly O _transformSource;
        O _lastChildTransform;
        object _finalContent;

        public O( object content, O transformSource = null )
        {
            if( content == null ) throw new ArgumentNullException( nameof( content ) );
            _content = content;
            if( (_transformSource = transformSource) != null )
            {
                _previousT = transformSource.AddT( this );
            }
        }

        O AddT( O t )
        {
            O previous = _lastChildTransform;
            _lastChildTransform = t;
            return previous;
        }

        public O TransformationSource => _transformSource;

        public bool IsTransformation => _transformSource != null;

        public object OriginalContent => _content;

        public object FinalContent => _finalContent 
                                      ?? (_finalContent = _lastChildTransform != null
                                                          ? _lastChildTransform.Transform()
                                                          : _content);

        object Transform()
        {
            Debug.Assert( IsTransformation );
            return DoTransform( FinalContent, _previousT != null ? _previousT.Transform() : _transformSource.OriginalContent );
        }

        protected virtual object DoTransform( object content, object input ) => _content;
    }


    [TestFixture]
    public class ObjectAndTransformerModelTests
    {
        class R : O
        {
            public R( string prog, O source )
                : base( prog, source )
            {

            }
            protected override object DoTransform( object content, object input )
            {
                Console.Write( $"Applying: '{content}' to '{input}'." );
                object r = Apply( content, input );
                Console.WriteLine( $"==> '{r}'." );
                return r;
            }

            static object Apply( object content, object input )
            {
                string[] r = ((string)content).Split( ';' );
                string text = (string)input;
                switch( r[0] )
                {
                    case "Replace": return text.Replace( r[1], r[2] );
                    case "Append": return text + r[1];
                    case "Prepend": return r[1] + text;
                    default: throw new InvalidDataException();
                }
            }
        }

        [Test]
        public void applying_a_list_of_transformations()
        {
            var origin = new O( "the lazy dog" );
            var r1 = new R( "Prepend;The quick brown fox jumps over ", origin );
            var r2 = new R( "Append; - this is a pangram.", origin );
            var r3 = new R( "Append; - It contains the whole alphabet.", origin );
            Assert.That( origin.FinalContent, Is.EqualTo( "The quick brown fox jumps over the lazy dog - this is a pangram. - It contains the whole alphabet." ) );
        }

        [Test]
        public void transforming_a_transformation()
        {
            var origin = new O( "the lazy dog" );
            var r1 = new R( "Prepend;The quick brown fox jumps over ", origin );
            var r2 = new R( "Append; - this is a pangram.", origin );
            var r3 = new R( "Append; - It contains the whole alphabet.", origin );
            var r4 = new R( "Replace;Append;Prepend", r2 );
            var r5 = new R( "Replace; - this is a pangram.;PANGRAM-", origin );
            Assert.That( origin.FinalContent, Is.EqualTo( "PANGRAM-The quick brown fox jumps over the lazy dog - It contains the whole alphabet." ) );
        }


        [Test]
        public void transformation_of_transformation_occurs_before_their_applications()
        {
            var origin = new O( "the lazy dog" );
            var r1 = new R( "Prepend;The quick brown fox jumps over ", origin );
            var r2 = new R( "Append; - this is a pangram.", origin );
            var r3 = new R( "Append; - It contains the whole alphabet.", origin );
            var r4 = new R( "Replace;Append;Prepend", r2 );
            var r5 = new R( "Replace; - this is a pangram.;PANGRAM-", origin );
            var r6 = new R( "Replace; - It contains the whole alphabet.;-ALPHABET", r3 );
            Assert.That( origin.FinalContent, Is.EqualTo( "PANGRAM-The quick brown fox jumps over the lazy dog-ALPHABET" ) );
        }

        [Test]
        public void an_object_used_as_a_transformation_acts_as_a_replacement()
        {
            {
                var origin = new O( "the lazy dog" );
                var r1 = new R( "Prepend;The quick brown fox jumps over ", origin );
                var oR = new O( "Crash", origin );
                var r2 = new R( "Append; - this is a pangram.", origin );
                var r3 = new R( "Append; - It contains the whole alphabet.", origin );
                var r4 = new R( "Replace;Append;Prepend", r2 );
                var r5 = new R( "Replace; - this is a pangram.;PANGRAM-", origin );
                var r6 = new R( "Replace; - It contains the whole alphabet.;-ALPHABET", r3 );
                Assert.That( origin.FinalContent, Is.EqualTo( "PANGRAM-Crash-ALPHABET" ) );
            }
            {
                var origin = new O( "the lazy dog" );
                var r1 = new R( "Prepend;The quick brown fox jumps over ", origin );
                var r2 = new R( "Append; - this is a pangram.", origin );
                var oR = new O( "Crash", origin );
                var r3 = new R( "Append; - It contains the whole alphabet.", origin );
                var r4 = new R( "Replace;Append;Prepend", r2 );
                var r5 = new R( "Replace; - this is a pangram.;PANGRAM-", origin );
                var r6 = new R( "Replace; - It contains the whole alphabet.;-ALPHABET", r3 );
                Assert.That( origin.FinalContent, Is.EqualTo( "Crash-ALPHABET" ) );
            }
            {
                var origin = new O( "the lazy dog" );
                var r1 = new R( "Prepend;The quick brown fox jumps over ", origin );
                var r2 = new R( "Append; - this is a pangram.", origin );
                var r3 = new R( "Append; - It contains the whole alphabet.", origin );
                var oR = new O( "Crash", origin );
                var r4 = new R( "Replace;Append;Prepend", r2 );
                var r5 = new R( "Replace; - this is a pangram.;PANGRAM-", origin );
                var r6 = new R( "Replace; - It contains the whole alphabet.;-ALPHABET", r3 );
                Assert.That( origin.FinalContent, Is.EqualTo( "Crash" ) );
            }
        }
    }
}