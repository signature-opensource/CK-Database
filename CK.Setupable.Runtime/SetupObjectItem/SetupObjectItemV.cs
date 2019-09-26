using System;
using System.Collections.Generic;
using System.Linq;
using CK.Core;

namespace CK.Setup
{
    /// <summary>
    /// A setup object item base class for items for which version matters.
    /// </summary>
    public abstract class SetupObjectItemV : SetupObjectItem, IVersionedItem
    {
        List<VersionedName> _previousNames;

        /// <summary>
        /// Initializes a <see cref="SetupObjectItemV"/> without ContextLocName nor ItemType.
        /// Specialized class must take care of initializing them: having no name nor type is not valid.
        /// </summary>
        protected SetupObjectItemV()
        {
        }
        
        /// <summary>
        /// Initializes a new <see cref="SetupObjectItemV"/>.
        /// </summary>
        /// <param name="name">Initial name of this item. Can not be null.</param>
        /// <param name="itemType">Type of the item. Can not be null nor longer than 16 characters.</param>
        /// <param name="version">Version (if known).</param>
        /// <param name="previousNames">Optional initial list for <see cref="PreviousNames"/>.</param>
        protected SetupObjectItemV( ContextLocName name, string itemType, Version version = null, IEnumerable<VersionedName> previousNames = null )
            : base( name, itemType )
        {
            Version = version;
            _previousNames = previousNames != null ? previousNames.ToList() : null;
        }

        /// <summary>
        /// Gets the transform target item if this item has associated <see cref="SetupObjectItem.Transformers"/>.
        /// This object is created as a clone of this object by the first call 
        /// to this <see cref="SetupObjectItem.AddTransformer"/> method.
        /// </summary>
        public new SetupObjectItemV TransformTarget => (SetupObjectItemV)base.TransformTarget;

        /// <summary>
        /// Called by <see cref="SetupObjectItem.AddTransformer"/> to initialize the initial 
        /// transform target as a clone of this object.
        /// </summary>
        /// <param name="monitor">The monitor to use.</param>
        /// <returns>True on success, false if an error occured.</returns>
        protected override bool OnTransformTargetCreated( IActivityMonitor monitor )
        {
            if( !base.OnTransformTargetCreated( monitor ) ) return false;
            if( _previousNames != null ) TransformTarget._previousNames = new List<VersionedName>( _previousNames );
            return true;
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
