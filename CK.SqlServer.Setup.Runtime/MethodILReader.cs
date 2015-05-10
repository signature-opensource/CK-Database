using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;

namespace Mono.Reflection
{

    public sealed class Instruction
    {

        readonly int _offset;
        readonly OpCode _opcode;
        object _operand;

        public int Offset
        {
            get { return _offset; }
        }

        public OpCode OpCode
        {
            get { return _opcode; }
        }

        public object Operand
        {
            get { return _operand; }
            internal set { _operand = value; }
        }

        internal Instruction( int offset, OpCode opcode )
        {
            _offset = offset;
            _opcode = opcode;
        }

        public int GetSize()
        {
            int size = _opcode.Size;

            switch( _opcode.OperandType )
            {
                case OperandType.InlineSwitch:
                    size += (1 + ((int[])_operand).Length) * 4;
                    break;
                case OperandType.InlineI8:
                case OperandType.InlineR:
                    size += 8;
                    break;
                case OperandType.InlineBrTarget:
                case OperandType.InlineField:
                case OperandType.InlineI:
                case OperandType.InlineMethod:
                case OperandType.InlineString:
                case OperandType.InlineTok:
                case OperandType.InlineType:
                case OperandType.ShortInlineR:
                    size += 4;
                    break;
                case OperandType.InlineVar:
                    size += 2;
                    break;
                case OperandType.ShortInlineBrTarget:
                case OperandType.ShortInlineI:
                case OperandType.ShortInlineVar:
                    size += 1;
                    break;
            }

            return size;
        }

        public override string ToString()
        {
            return _opcode.Name;
        }
    }

    class MethodBodyReader
    {

        static readonly OpCode[] one_byte_opcodes;
        static readonly OpCode[] two_bytes_opcodes;

        static MethodBodyReader()
        {
            one_byte_opcodes = new OpCode[0xe1];
            two_bytes_opcodes = new OpCode[0x1f];

            FieldInfo[] fields = typeof( OpCodes ).GetFields( BindingFlags.Public | BindingFlags.Static );
            for( int i = 0; i < fields.Length; i++ )
            {
                var opcode = (OpCode)fields[i].GetValue( null );
                if( opcode.OpCodeType == OpCodeType.Nternal ) continue;
                if( opcode.Size == 1 )
                    one_byte_opcodes[opcode.Value] = opcode;
                else
                    two_bytes_opcodes[opcode.Value & 0xff] = opcode;
            }
        }

        class ByteBuffer
        {
            internal byte [] _buffer;
            internal int _position;

            public ByteBuffer( byte[] buffer )
            {
                this._buffer = buffer;
            }

            public byte ReadByte()
            {
                CheckCanRead( 1 );
                return _buffer[_position++];
            }

            public byte[] ReadBytes( int length )
            {
                CheckCanRead( length );
                var value = new byte[length];
                Buffer.BlockCopy( _buffer, _position, value, 0, length );
                _position += length;
                return value;
            }

            public short ReadInt16()
            {
                CheckCanRead( 2 );
                short value = (short)(_buffer[_position]
                    | (_buffer[_position + 1] << 8));
                _position += 2;
                return value;
            }

            public int ReadInt32()
            {
                CheckCanRead( 4 );
                int value = _buffer[_position]
                    | (_buffer[_position + 1] << 8)
                    | (_buffer[_position + 2] << 16)
                    | (_buffer[_position + 3] << 24);
                _position += 4;
                return value;
            }

            public long ReadInt64()
            {
                CheckCanRead( 8 );
                uint low = (uint)(_buffer[_position]
                    | (_buffer[_position + 1] << 8)
                    | (_buffer[_position + 2] << 16)
                    | (_buffer[_position + 3] << 24));

                uint high = (uint)(_buffer[_position + 4]
                    | (_buffer[_position + 5] << 8)
                    | (_buffer[_position + 6] << 16)
                    | (_buffer[_position + 7] << 24));

                long value = (((long)high) << 32) | low;
                _position += 8;
                return value;
            }

            public float ReadSingle()
            {
                if( !BitConverter.IsLittleEndian )
                {
                    var bytes = ReadBytes( 4 );
                    Array.Reverse( bytes );
                    return BitConverter.ToSingle( bytes, 0 );
                }

                CheckCanRead( 4 );
                float value = BitConverter.ToSingle( _buffer, _position );
                _position += 4;
                return value;
            }

            public double ReadDouble()
            {
                if( !BitConverter.IsLittleEndian )
                {
                    var bytes = ReadBytes( 8 );
                    Array.Reverse( bytes );
                    return BitConverter.ToDouble( bytes, 0 );
                }

                CheckCanRead( 8 );
                double value = BitConverter.ToDouble( _buffer, _position );
                _position += 8;
                return value;
            }

            void CheckCanRead( int count )
            {
                if( _position + count > _buffer.Length )
                    throw new ArgumentOutOfRangeException();
            }
        }

        MethodBase method;
        MethodBody body;
        Module module;
        Type [] type_arguments;
        Type [] method_arguments;
        ByteBuffer il;
        ParameterInfo [] parameters;
        IList<LocalVariableInfo> locals;
        List<Instruction> instructions = new List<Instruction>();

        MethodBodyReader( MethodBase method )
        {
            this.method = method;

            this.body = method.GetMethodBody();
            if( this.body == null )
                throw new ArgumentException();

            var bytes = body.GetILAsByteArray();
            if( bytes == null )
                throw new ArgumentException();

            if( !(method is ConstructorInfo) )
                method_arguments = method.GetGenericArguments();

            if( method.DeclaringType != null )
                type_arguments = method.DeclaringType.GetGenericArguments();

            this.parameters = method.GetParameters();
            this.locals = body.LocalVariables;
            this.module = method.Module;
            this.il = new ByteBuffer( bytes );
        }

        void ReadInstructions()
        {
            while( il._position < il._buffer.Length )
            {
                var instruction = new Instruction( il._position, ReadOpCode() );
                ReadOperand( instruction );
                instructions.Add( instruction );
            }
        }

        void ReadOperand( Instruction instruction )
        {
            switch( instruction.OpCode.OperandType )
            {
                case OperandType.InlineNone:
                    break;
                case OperandType.InlineSwitch:
                    int length = il.ReadInt32();
                    int [] branches = new int[length];
                    int [] offsets = new int[length];
                    for( int i = 0; i < length; i++ )
                        offsets[i] = il.ReadInt32();
                    for( int i = 0; i < length; i++ )
                        branches[i] = il._position + offsets[i];

                    instruction.Operand = branches;
                    break;
                case OperandType.ShortInlineBrTarget:
                    instruction.Operand = (sbyte)(il.ReadByte() + il._position);
                    break;
                case OperandType.InlineBrTarget:
                    instruction.Operand = il.ReadInt32() + il._position;
                    break;
                case OperandType.ShortInlineI:
                    if( instruction.OpCode == OpCodes.Ldc_I4_S )
                        instruction.Operand = (sbyte)il.ReadByte();
                    else
                        instruction.Operand = il.ReadByte();
                    break;
                case OperandType.InlineI:
                    instruction.Operand = il.ReadInt32();
                    break;
                case OperandType.ShortInlineR:
                    instruction.Operand = il.ReadSingle();
                    break;
                case OperandType.InlineR:
                    instruction.Operand = il.ReadDouble();
                    break;
                case OperandType.InlineI8:
                    instruction.Operand = il.ReadInt64();
                    break;
                case OperandType.InlineSig:
                    instruction.Operand = module.ResolveSignature( il.ReadInt32() );
                    break;
                case OperandType.InlineString:
                    instruction.Operand = module.ResolveString( il.ReadInt32() );
                    break;
                case OperandType.InlineTok:
                    instruction.Operand = module.ResolveMember( il.ReadInt32(), type_arguments, method_arguments );
                    break;
                case OperandType.InlineType:
                    instruction.Operand = module.ResolveType( il.ReadInt32(), type_arguments, method_arguments );
                    break;
                case OperandType.InlineMethod:
                    instruction.Operand = module.ResolveMethod( il.ReadInt32(), type_arguments, method_arguments );
                    break;
                case OperandType.InlineField:
                    instruction.Operand = module.ResolveField( il.ReadInt32(), type_arguments, method_arguments );
                    break;
                case OperandType.ShortInlineVar:
                    instruction.Operand = GetVariable( instruction, il.ReadByte() );
                    break;
                case OperandType.InlineVar:
                    instruction.Operand = GetVariable( instruction, il.ReadInt16() );
                    break;
                default:
                    throw new NotSupportedException();
            }
        }

        object GetVariable( Instruction instruction, int index )
        {
            if( instruction.OpCode.Name.Contains( "loc" ) )
                return locals[index];
            return parameters[ method.IsStatic ? index-1 : index];
        }

        OpCode ReadOpCode()
        {
            byte op = il.ReadByte();
            return op != 0xfe
                ? one_byte_opcodes[op]
                : two_bytes_opcodes[il.ReadByte()];
        }

        public static List<Instruction> GetInstructions( MethodBase method )
        {
            var reader = new MethodBodyReader( method );
            reader.ReadInstructions();
            return reader.instructions;
        }
    }

    public static class MethodBaseRocks
    {

        public static IList<Instruction> GetInstructions( this MethodBase self )
        {
            return MethodBodyReader.GetInstructions( self ).AsReadOnly();
        }
    }
}