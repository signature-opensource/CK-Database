using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CKSetup
{
    internal class SHA1Stream : Stream
    {
        SHA1Managed _sha1;
        readonly Stream _reader;
        readonly Stream _writer;
        readonly bool _leaveOpen;

        public SHA1Stream()
        {
            _sha1 = new SHA1Managed();
        }

        public SHA1Stream( Stream inner, bool read, bool leaveOpen )
        {
            _sha1 = new SHA1Managed();
            _leaveOpen = leaveOpen;
            if( read ) _reader = inner;
            else _writer = inner;
        }

        public SHA1Value GetFinalResult()
        {
            _sha1.TransformFinalBlock( Array.Empty<byte>(), 0, 0 );
            return new SHA1Value( _sha1.Hash );
        }

        protected override void Dispose( bool disposing )
        {
            if( disposing && _sha1 != null )
            {
                _sha1.Dispose();
                _sha1 = null;
                if( !_leaveOpen )
                {
                    _reader?.Dispose();
                    _writer?.Dispose();
                }
            }
        }

        public override bool CanRead => _reader != null;

        public override bool CanSeek => false;

        public override bool CanWrite => _reader == null;

        public override void Flush()
        {
            _reader?.Flush();
            _writer?.Flush();
        }

        public override long Length => throw new NotSupportedException();

        public override long Position
        {
            get { throw new NotSupportedException(); }
            set { throw new NotSupportedException(); }
        }

        public override int Read( byte[] buffer, int offset, int count )
        {
            if( _reader == null ) throw new InvalidOperationException();
            int r = _reader.Read( buffer, offset, count );
            _sha1.TransformBlock( buffer, offset, r, null, 0 );
            return r;
        }

        public override long Seek( long offset, SeekOrigin origin ) => throw new NotSupportedException();

        public override void SetLength( long value ) => throw new NotSupportedException();

        public override void Write( byte[] buffer, int offset, int count )
        {
            if( _reader != null ) throw new InvalidOperationException();
            _sha1.TransformBlock( buffer, offset, count, null, 0 );
            _writer?.Write( buffer, offset, count );
        }

        public override Task WriteAsync( byte[] buffer, int offset, int count, CancellationToken cancellationToken )
        {
            if( _reader != null ) return Task.FromException( new InvalidOperationException() );
            _sha1.TransformBlock( buffer, offset, count, null, 0 );
            return _writer != null ? _writer.WriteAsync( buffer, offset, count ) : Task.CompletedTask;
        }

        public override Task<int> ReadAsync( byte[] buffer, int offset, int count, CancellationToken cancellationToken )
        {
            if( _reader == null ) return Task.FromException<int>( new InvalidOperationException() );
            return _reader.ReadAsync( buffer, offset, count, cancellationToken )
                          .ContinueWith( x =>
                          {
                              _sha1.TransformBlock( buffer, offset, x.Result, null, 0 );
                              return x.Result;
                          }, TaskContinuationOptions.OnlyOnRanToCompletion );
        }
    }
}
