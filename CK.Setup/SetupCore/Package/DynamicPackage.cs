using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using CK.Core;

namespace CK.Setup.Database
{
    public class DynamicPackage : PackageBase, IDependentItemDiscoverer
    {
        string _fullName;
        DynamicPackageModel _model;

        public DynamicPackage()
        {
        }

        /// <summary>
        /// Gets the optional <see cref="DynamicPackageModel"/> for this <see cref="DynamicPackage"/>.
        /// It is null if this package has no Model: use <see cref="EnsureModel"/> to
        /// create the Model if needed.
        /// </summary>
        public DynamicPackageModel Model
        {
            get { return _model; }
        }

        public DynamicPackageModel EnsureModel()
        {
            return _model ?? (_model = new DynamicPackageModel( this ));
        }

        public void SupressModel()
        {
            _model = null;
        }

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

