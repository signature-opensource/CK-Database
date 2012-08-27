using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using CK.Core;

namespace CK.Setup
{
    public class DynamicPackageItem : PackageItemBase, IDependentItemDiscoverer
    {
        string _fullName;
        PackageModelItem _model;

        public DynamicPackageItem( string itemType )
            : base( itemType )
        {
        }

        /// <summary>
        /// Gets the optional <see cref="PackageModelItem"/> for this <see cref="DynamicPackageItem"/>.
        /// It is null (the default) if this package has no Model: use <see cref="EnsureModel"/> to
        /// create the Model if needed.
        /// </summary>
        public PackageModelItem Model
        {
            get { return _model; }
        }

        /// <summary>
        /// Creates the associated <see cref="Model"/> package if it does not exist yet.
        /// </summary>
        /// <returns></returns>
        public PackageModelItem EnsureModel()
        {
            return _model ?? (_model = new PackageModelItem( this ));
        }

        /// <summary>
        /// Removes the <see cref="Model"/> (sets it to null).
        /// </summary>
        public void SupressModel()
        {
            _model = null;
        }

        /// <summary>
        /// Gets or sets the full name of this package.
        /// </summary>
        public string FullName
        {
            get { return _fullName; }
            set { _fullName = value ?? String.Empty; }
        }

        protected override string GetFullName()
        {
            return _fullName;
        }

        protected override object StartDependencySort()
        {
            return typeof(PackageDriver);
        }

        IEnumerable<IDependentItem> IDependentItemDiscoverer.GetOtherItemsToRegister()
        {
            return _model != null ? new ReadOnlyListMono<IDependentItem>( _model ) : null;
        }

    }


}

