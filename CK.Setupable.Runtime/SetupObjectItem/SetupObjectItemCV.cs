using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CK.Core;

namespace CK.Setup
{
    /// <summary>
    /// A setup object item implementation for items that can be containers or groups and for which 
    /// version must be supported.
    /// </summary>
    public abstract class SetupObjectItemCV : SetupObjectItemC, IVersionedItem
    {
        List<VersionedName> _previousNames;

        /// <summary>
        /// Initializes a <see cref="SetupObjectItemCV"/> without ContextLocName nor ItemType.
        /// Specialized class must take care of initializing them: having no name nor type is not valid.
        /// </summary>
        protected SetupObjectItemCV()
        {
        }
        
        /// <summary>
        /// Initializes a new <see cref="SetupObjectItemCV"/>.
        /// </summary>
        /// <param name="name">Initial name of this item. Can not be null.</param>
        /// <param name="itemType">Type of the item. Can not be null nor longer than 16 characters.</param>
        /// <param name="version">Version (if known).</param>
        /// <param name="previousNames">Optional initial list for <see cref="PreviousNames"/>.</param>
        /// <param name="containerName">
        /// Optional container name to which this item belongs. Its name will be used by the dependency sorter.
        /// If it is not the same as the actual container to which this object
        /// is added later, an error will be raised during the ordering. 
        /// </param>
        protected SetupObjectItemCV( ContextLocName name, string itemType, Version version = null, IEnumerable<VersionedName> previousNames = null, string containerName = null )
            : base( name, itemType, containerName )
        {
            Version = version;
            _previousNames = previousNames != null ? previousNames.ToList() : null;
        }

        public new SetupObjectItemCV TransformTarget => (SetupObjectItemCV)base.TransformTarget;

        protected override void OnTransformTargetCreated( IActivityMonitor monitor )
        {
            base.OnTransformTargetCreated( monitor );
            if( _previousNames != null ) TransformTarget._previousNames = new List<VersionedName>( _previousNames );
        }

        /// <summary>
        /// Gets or sets the version number. Can be null.
        /// </summary>
        /// <remarks>
        /// When code builds the object, it may be safer to let the version be null and to rewrite the object.
        /// </remarks>
        public Version Version { get; set; }

        /// <summary>
        /// Gets a mutable list of previous version name.
        /// </summary>
        public IList<VersionedName> PreviousNames => _previousNames ?? (_previousNames = new List<VersionedName>());

        IEnumerable<VersionedName> IVersionedItem.PreviousNames
        {
            get { return _previousNames.SetRefFullName( r => DefaultContextLocNaming.Resolve( r.FullName, ContextLocName.Context, ContextLocName.Location ) ); }
        }

    }



}
