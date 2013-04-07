//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;

//namespace CK.Core
//{
//    public static class EnumerableExtension_TEMP
//    {
//        public static int IndexOf<TSource>( this IEnumerable<TSource> @this, Func<TSource, bool> predicate )
//        {
//            int i = 0;
//            using( var e = @this.GetEnumerator() )
//            {
//                while( e.MoveNext() )
//                {
//                    if( predicate( e.Current ) ) return i;
//                    ++i;
//                }
//            }
//            return -1;
//        }

//        public static int IndexOf<TSource>( this IEnumerable<TSource> @this, Func<TSource, int, bool> predicate )
//        {
//            int i = 0;
//            using( var e = @this.GetEnumerator() )
//            {
//                while( e.MoveNext() )
//                {
//                    if( predicate( e.Current, i ) ) return i;
//                    ++i;
//                }
//            }
//            return -1;
//        }
//    }
//}    


