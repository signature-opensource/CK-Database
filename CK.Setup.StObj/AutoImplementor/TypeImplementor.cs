//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Reflection;
//using System.IO;
//using System.Reflection.Emit;
//using System.Threading;
//using CK.Reflection;

//namespace CK.Core
//{

//    /// <summary>
//    /// Centralizes code generation for a <see cref="Type"/> that uses multiple <see cref="IAutoImplementorMethod"/>.
//    /// </summary>
//    public class TypeImplementor
//    {
//        readonly DynamicAssembly _assembly;
//        readonly Type _baseType;
//        readonly KeyValuePair<MethodInfo,IAutoImplementorMethod>[] _methodImplementors;

//        /// <summary>
//        /// Initializes a new type implementor for a type.
//        /// </summary>
//        /// <param name="assembly">Dynamic assembly into which the new type will be generated.</param>
//        /// <param name="baseType">Base type to implement.</param>
//        /// <param name="methodImplementors">Set of <see cref="MethodInfo"/> with an associated <see cref="IAutoImplementorMethod"/>.</param>
//        public TypeImplementor( DynamicAssembly assembly, Type baseType, KeyValuePair<MethodInfo,IAutoImplementorMethod>[] methodImplementors )
//        {
//            if( assembly == null ) throw new ArgumentNullException( "assembly" );
//            if( baseType == null ) throw new ArgumentNullException( "baseType" );
//            if( methodImplementors == null ) throw new ArgumentNullException( "methodImplementors" );
//            _assembly = assembly;
//            _baseType = baseType;
//            _methodImplementors = methodImplementors;
//        }

//        /// <summary>
//        /// Implements a new <see cref="Type"/> in a dynamic assembly and returns it.
//        /// </summary>
//        /// <param name="logger">Logger to use.</param>
//        /// <returns>The newly created type in the dynamic assembly.</returns>
//        public virtual Type CreateType( IActivityLogger logger )
//        {
//            TypeAttributes tA = TypeAttributes.Class | TypeAttributes.Public;
//            TypeBuilder b = _assembly.ModuleBuilder.DefineType( _baseType.Name + _assembly.NextUniqueNumber(), tA, _baseType );
//            foreach( var am in _methodImplementors )
//            {
//                try
//                {
//                    if( !am.Value.Implement( logger, am.Key, b ) )
//                        return null;
//                }
//                catch( Exception ex )
//                {
//                    logger.Error( ex, "While implementing method '{0}.{1}'.", am.Key.DeclaringType.FullName, am.Key.Name );
//                    return null;
//                }
//            }
//            try
//            {
//                return b.CreateType();
//            }
//            catch( Exception ex )
//            {
//                logger.Error( ex, "While implementing Type '{0}'.", _baseType.FullName );
//                return null;
//            }
//        }

//        /// <summary>
//        /// Implements an empty method with an <see cref="EmptyStubAttribute"/> that memorizes the <see cref="IAutoImplementorMethod"/> that will be used.
//        /// </summary>
//        /// <param name="tB">The <see cref="TypeBuilder"/> to use.</param>
//        /// <param name="method">Method to implement.</param>
//        /// <param name="later">The implementor to associate.</param>
//        /// <returns>The built <see cref="MethodBuilder"/>.</returns>
//        public static MethodBuilder ImplementTemporaryImpl( TypeBuilder tB, MethodInfo method, IAutoImplementorMethod later )
//        {
//            MethodBuilder m = EmitHelper.ImplementEmptyStubMethod( tB, method, true );
//            EmptyStubAttribute.AttachTo( m, later );
//            return m;
//        }

//        static public KeyValuePair<MethodInfo, IAutoImplementorMethod>[] GetAutoImplementMethodsFromAttributes( IActivityLogger logger, Type abstractType )
//        {
//            if( logger == null ) throw new ArgumentNullException( "logger" );
//            if( abstractType == null ) throw new ArgumentNullException( "abstractType" );
//            if( !abstractType.IsAbstract ) throw new ArgumentException( "Type must be abstract.", "abstractType" );
            
//            if( abstractType.IsDefined( typeof( PreventAutoImplementationAttribute ), false ) ) return null;

//            var candidates = abstractType.GetMethods( BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public ).Where( m => !m.IsSpecialName && m.IsAbstract );
//            int nbUncovered = 0;
//            List<KeyValuePair<MethodInfo,IAutoImplementorMethod>> implMap = new List<KeyValuePair<MethodInfo, IAutoImplementorMethod>>();
//            foreach( var m in candidates )
//            {
//                ++nbUncovered;
//                var c = (IAutoImplementorMethod[])m.GetCustomAttributes( typeof( IAutoImplementorMethod ), false );
//                if( c.Length > 0 )
//                {
//                    --nbUncovered;
//                    if( c.Length > 1 )
//                    {
//                        logger.Error( "Multiple attributes are IAutoImplementorMethod ('{0}'). Only one can be defined.", String.Join( "', '", c.Select( a => a.ToString() ) ) );
//                        return null;
//                    }
//                    else implMap.Add( new KeyValuePair<MethodInfo, IAutoImplementorMethod>( m, c[0] ) );
//                }
//            }
//            if( nbUncovered > 0 ) return null;
//            return implMap.ToArray();
//        }

//    }
//}
