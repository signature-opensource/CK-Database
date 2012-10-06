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
                if( d != null ) return GetReference( (IDependentItemGroupRef)d.Item );
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
