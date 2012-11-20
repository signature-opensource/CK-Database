using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CK.Setup
{

    /// <summary>
    /// Exposes extension methods on <see cref="IDependentItem"/> and <see cref="IDependentItemContainer"/>.
    /// </summary>
    public static class DependentItemExtension
    {
        /// <summary>
        /// Returns a collection of <see cref="IDependentItemRef"/> with potentially different <see cref="IDependentItem.FullName"/>.
        /// Can safely be called on null source reference.
        /// </summary>
        /// <param name="source">This enumerable.</param>
        /// <param name="fullNameProvider">Name provider. By returning null, the reference does not appear in the resulting enumerable.</param>
        /// <returns>A collection of <see cref="IDependentItemRef"/> whose FullName of named references may have changed.</returns>
        public static IEnumerable<IDependentItemRef> SetRefFullName( this IEnumerable<IDependentItemRef> source, Func<IDependentItemNamedRef, string> fullNameProvider )
        {
            return source != null ? CallSetRefFullName( source, fullNameProvider ) : source;
        }

        /// <summary>
        /// Returns a collection of <see cref="IDependentItemGroupRef"/> with potentially different <see cref="IDependentItem.FullName"/>.
        /// Can safely be called on null source reference.
        /// </summary>
        /// <param name="source">This enumerable.</param>
        /// <param name="fullNameProvider">Name provider. By returning null, the reference does not appear in the resulting enumerable.</param>
        /// <returns>A collection of <see cref="IDependentItemGroupRef"/> whose FullName of named references may have changed.</returns>
        public static IEnumerable<IDependentItemGroupRef> SetRefFullName( this IEnumerable<IDependentItemGroupRef> source, Func<IDependentItemNamedRef, string> fullNameProvider )
        {
            return source != null ? CallSetRefFullName( source, fullNameProvider ) : source;
        }

        /// <summary>
        /// Returns a collection of <see cref="IDependentItemContainerRef"/> with potentially different <see cref="IDependentItem.FullName"/>.
        /// Can safely be called on null source reference.
        /// </summary>
        /// <param name="source">This enumerable.</param>
        /// <param name="fullNameProvider">Name provider. By returning null, the reference does not appear in the resulting enumerable.</param>
        /// <returns>A collection of <see cref="IDependentItemContainerRef"/> whose FullName of named references may have changed.</returns>
        public static IEnumerable<IDependentItemContainerRef> SetRefFullName( this IEnumerable<IDependentItemContainerRef> source, Func<IDependentItemNamedRef,string> fullNameProvider )
        {
            return source != null ? CallSetRefFullName( source, fullNameProvider ) : source;
        }

        /// <summary>
        /// Returns an updated reference if this is a <see cref="IDependentItemNamedRef"/>.
        /// Can safely be called on null reference and on reference that are not <see cref="IDependentItemNamedRef"/> (this unifies the API of extension 
        /// methods SetRefFullName on <see cref="IEnumerable{T}"/> of <see cref="IDependentItem"/> and its specializations).
        /// </summary>
        /// <param name="@this">This item reference.</param>
        /// <param name="fullNameProvider">Name provider. When null is returned, it is ignored.</param>
        /// <returns>The reference with an updated name.</returns>
        public static IDependentItemRef SetRefFullName( this IDependentItemRef @this, Func<IDependentItemNamedRef, string> fullNameProvider )
        {
            IDependentItemNamedRef r = @this as IDependentItemNamedRef;
            return r != null ? (IDependentItemRef)r.SetFullName( fullNameProvider( r ) ) : @this;
        }

        /// <summary>
        /// Returns an updated reference if this is a <see cref="IDependentItemNamedRef"/>.
        /// Can safely be called on null reference and on reference that are not <see cref="IDependentItemNamedRef"/> (this unifies the API of extension 
        /// methods SetRefFullName on <see cref="IEnumerable{T}"/> of <see cref="IDependentItem"/> and its specializations).
        /// </summary>
        /// <param name="@this">This group reference.</param>
        /// <param name="fullNameProvider">Name provider. When null is returned, it is ignored.</param>
        /// <returns>The reference with an updated name.</returns>
        public static IDependentItemGroupRef SetRefFullName( this IDependentItemGroupRef @this, Func<IDependentItemNamedRef, string> fullNameProvider )
        {
            IDependentItemNamedRef r = @this as IDependentItemNamedRef;
            return r != null ? (IDependentItemGroupRef)r.SetFullName( fullNameProvider( r ) ) : @this;
        }

        /// <summary>
        /// Returns an updated reference if this is a <see cref="IDependentItemNamedRef"/>.
        /// Can safely be called on null reference and on reference that are not <see cref="IDependentItemNamedRef"/> (this unifies the API of extension 
        /// methods SetRefFullName on <see cref="IEnumerable{T}"/> of <see cref="IDependentItem"/> and its specializations).
        /// </summary>
        /// <param name="@this">This container reference.</param>
        /// <param name="fullNameProvider">Name provider. When null is returned, it is ignored.</param>
        /// <returns>The reference with an updated name.</returns>
        public static IDependentItemContainerRef SetRefFullName( this IDependentItemContainerRef @this, Func<IDependentItemNamedRef, string> fullNameProvider )
        {
            IDependentItemNamedRef r = @this as IDependentItemNamedRef;
            return r != null ? (IDependentItemContainerRef)r.SetFullName( fullNameProvider( r ) ) : @this;
        }

        /// <summary>
        /// Private implementation to avoid duplicated code or IEnumerable{T} extension methods pollution.
        /// </summary>
        static IEnumerable<T> CallSetRefFullName<T>( this IEnumerable<T> source, Func<IDependentItemNamedRef, string> fullNameProvider )
        {
            foreach( var e in source )
            {
                IDependentItemNamedRef named = e as IDependentItemNamedRef;
                if( named != null )
                {
                    string newName = fullNameProvider( named );
                    if( newName != null )
                    {
                        if( newName == named.FullName ) yield return e;
                        else yield return (T)named.SetFullName( newName );
                    }
                }
                else yield return e;
            }
        }

        /// <summary>
        /// Private base class that hosts a reference to a dependent items.
        /// </summary>
        class DirectRef
        {
            public IDependentItem Item { get; protected set; }
        }

        class ItemRef : DirectRef, IDependentItemContainerRef
        {
            public ItemRef( IDependentItem item )
            {
                Item = item;
            }

            public string FullName
            {
                get { return Item.FullName; }
            }

            public bool Optional
            {
                get { return false; }
            }

            public override bool Equals( object obj )
            {
                if( obj is IDependentItemRef )
                {
                    IDependentItemRef o = (IDependentItemRef)obj;
                    return !o.Optional && o.FullName == Item.FullName;
                }
                return false;
            }

            public override int GetHashCode()
            {
                return Item.FullName.GetHashCode();
            }
        }

        class OptItemRef : DirectRef, IDependentItemContainerRef
        {
            public OptItemRef( IDependentItem item )
            {
                Item = item;
            }

            public string FullName
            {
                get { return Item.FullName; }
            }

            public bool Optional
            {
                get { return true; }
            }

            public override bool Equals( object obj )
            {
                if( obj is IDependentItemRef )
                {
                    IDependentItemRef o = (IDependentItemRef)obj;
                    return Optional && o.FullName == Item.FullName;
                }
                return false;
            }

            public override int GetHashCode()
            {
                return -Item.FullName.GetHashCode();
            }
        }

        #region GetReference and GetOptionalReference on IDependentItem, IDependentItemGroup and IDependentItemContainer

        public static IDependentItemRef GetReference( this IDependentItem @this )
        {
            IDependentItemRef r = @this as IDependentItemRef;
            if( r == null ) r = @this != null ? new ItemRef( @this ) : null;
            return r;
        }

        public static IDependentItemGroupRef GetReference( this IDependentItemGroup @this )
        {
            IDependentItemGroupRef r = @this as IDependentItemGroupRef;
            if( r == null ) r = @this != null ? new ItemRef( @this ) : null;
            return r;
        }

        public static IDependentItemContainerRef GetReference( this IDependentItemContainer @this )
        {
            IDependentItemContainerRef r = @this as IDependentItemContainerRef;
            if( r == null ) r = @this != null ? new ItemRef( @this ) : null;
            return r;
        }

        public static IDependentItemRef GetOptionalReference( this IDependentItem @this, bool optional = true )
        {
            if( !optional ) return GetReference( @this );
            return @this != null ? new OptItemRef( @this ) : null;
        }

        public static IDependentItemGroupRef GetOptionalReference( this IDependentItemGroup @this, bool optional = true )
        {
            if( !optional ) return GetReference( @this );
            return @this != null ? new OptItemRef( @this ) : null;
        }

        public static IDependentItemContainerRef GetOptionalReference( this IDependentItemContainer @this, bool optional = true )
        {
            if( !optional ) return GetReference( @this );
            return @this != null ? new OptItemRef( @this ) : null;
        }
        
        #endregion

        #region GetReference and GetOptionalReference on IDependentItemRef, IDependentItemGroupRef and IDependentItemContainerRef

        public static IDependentItemRef GetReference( this IDependentItemRef @this )
        {
            if( @this != null && @this.Optional )
            {
                DirectRef d = @this as DirectRef;
                if( d != null ) return GetReference( d.Item );
                return new NamedDependentItemRef( @this.FullName );
            }
            return @this;
        }

        public static IDependentItemGroupRef GetReference( this IDependentItemGroupRef @this )
        {
            if( @this != null && @this.Optional )
            {
                DirectRef d = @this as DirectRef;
                if( d != null ) return GetReference( (IDependentItemGroup)d.Item );
                return new NamedDependentItemGroupRef( @this.FullName );
            }
            return @this;
        }

        public static IDependentItemContainerRef GetReference( this IDependentItemContainerRef @this )
        {
            if( @this != null && @this.Optional )
            {
                DirectRef d = @this as DirectRef;
                if( d != null ) return GetReference( (IDependentItemContainer)d.Item );
                return new NamedDependentItemContainerRef( @this.FullName );
            }
            return @this;
        }

        public static IDependentItemRef GetOptionalReference( this IDependentItemRef @this, bool optional = true )
        {
            if( !optional ) return GetReference( @this );
            if( @this == null || @this.Optional ) return @this;
            DirectRef d = @this as DirectRef;
            if( d != null ) return new OptItemRef( d.Item );
            return new NamedDependentItemRef( @this.FullName, true );
        }

        public static IDependentItemGroupRef GetOptionalReference( this IDependentItemGroupRef @this, bool optional = true )
        {
            if( !optional ) return GetReference( @this );
            if( @this == null || @this.Optional ) return @this;
            DirectRef d = @this as DirectRef;
            if( d != null ) return new OptItemRef( d.Item );
            return new NamedDependentItemGroupRef( @this.FullName, true );
        }

        public static IDependentItemContainerRef GetOptionalReference( this IDependentItemContainerRef @this, bool optional = true )
        {
            if( !optional ) return GetReference( @this );
            if( @this == null || @this.Optional ) return @this;
            DirectRef d = @this as DirectRef;
            if( d != null ) return new OptItemRef( d.Item );
            return new NamedDependentItemContainerRef( @this.FullName, true );
        }

        #endregion


    }
}
