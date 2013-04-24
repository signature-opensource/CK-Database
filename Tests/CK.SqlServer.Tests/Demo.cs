//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;

//namespace CK.SqlServer.Tests
//{

//    public abstract class CodeOp
//    {
//        readonly CodeOpRoot _root;

//        internal CodeOp( CodeOpRoot root )
//        {
//            _root = root;
//        }

//        /// <summary>
//        /// Gets the <see cref="CodeOpRoot"/> that created this operation.
//        /// </summary>
//        public CodeOpRoot Root { get { return _root; } }

//        public string OpName { get { return _root.Name; } }

//        public abstract void Execute( ExecutionEnvironment env );
//    }

//    public class SourceCode
//    {
//    }

//    public class CompiledCode
//    {
//        public IReadOnlyList<OpCode> Code { get; }
//    }

//    public class CompilerResult
//    {

//        public bool IsSuccess { get { return ErrorMessages.Count == 0; } }

//        public IReadOnlyList<string> ErrorMessages { get; internal set; }

//        /// <summary>
//        /// Null if at least one error exists in <see cref="ErrorMessages"/>.
//        /// </summary>
//        public CompiledCode Code { get; }
//    }

//    public class Compiler
//    {
//        public CompilerResult Compile( SourceCode source )
//        {

//        }

//    }

//    class Demo
//    {
//    }
//}
