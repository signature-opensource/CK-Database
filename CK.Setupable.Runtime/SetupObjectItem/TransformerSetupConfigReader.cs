using CK.Core;
using CK.Text;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace CK.Setup
{
    /// <summary>
    /// Helper class (that can be specialized: see <see cref="OnUnknownProperty"/>)
    /// that applies a textual configuration '"SetupConfig": {...}' from a string to a target setup item
    /// or to a transformer and its target object.
    /// </summary>
    public class TransformerSetupConfigReader : SetupConfigReader
    {
        /// <summary>
        /// Initializes a new <see cref="SetupConfigReader"/>.
        /// </summary>
        /// <param name="transformer">Transformer to configure.</param>
        /// <param name="targetConfigReader">A configuration reader for the transformer's target.</param>
        public TransformerSetupConfigReader( ISetupObjectTransformerItem transformer, SetupConfigReader targetConfigReader )
            : base( transformer )
        {
            if( transformer == null ) throw new ArgumentNullException( nameof( transformer ) );
            if( targetConfigReader == null ) throw new ArgumentNullException( nameof( targetConfigReader ) );
            if( transformer.Target != targetConfigReader.Item ) throw new ArgumentException( "targetConfigReader must be a configuration reader of transformer's target." );
            TargetConfigReader = targetConfigReader;
        }

        /// <summary>
        /// Gets the transformer item to configure.
        /// </summary>
        public new ISetupObjectTransformerItem Item => (ISetupObjectTransformerItem)base.Item;

        /// <summary>
        /// Gets the configuration reader of the target to setup.
        /// </summary>
        public SetupConfigReader TargetConfigReader { get; }

        /// <summary>
        /// First handles Add/RemoveRequires, Add/RemoveRequiredBy, Add/RemoveGroups, Add/RemoveChildren, TargetContainer
        /// and TargetGeneralization.
        /// If none of these match and if the property starts with 'Transformer' it is transfered to the 
        /// base <see cref="SetupConfigReader.ApplyProperty(StringMatcher, string)"/> with the corresponding 
        /// property (the 'Transformer' prefix is removed).
        /// At last, if the property starts withs 'Target', it is transfered to the 
        /// <see cref="TargetConfigReader"/> with the corresponding property (the 'Target' prefix is removed).
        /// </summary>
        /// <param name="m">The <see cref="StringMatcher"/>.</param>
        /// <param name="propName">The property name.</param>
        /// <returns>
        /// True if <paramref name="propName"/> has been applied, false 
        /// otherwise or if an error occurred (<see cref="StringMatcher.IsError"/> is true in such case).
        /// </returns>
        protected internal override bool ApplyProperty( StringMatcher m, string propName )
        {
            switch( propName )
            {
                case "AddRequires": ApplyProperties( m, s => TargetConfigReader.Item.Requires.Add( s ) ); break;
                case "AddRequiredBy": ApplyProperties( m, s => TargetConfigReader.Item.RequiredBy.Add( s ) ); break;
                case "AddGroups": ApplyProperties( m, s => TargetConfigReader.Item.Groups.Add( s ) ); break;
                case "AddChildren": TargetConfigReader.ApplyChildren( m, true ); break;
                case "RemoveRequires": ApplyProperties( m, s => TargetConfigReader.Item.Requires.Remove( s ) ); break;
                case "RemoveRequiredBy": ApplyProperties( m, s => TargetConfigReader.Item.RequiredBy.Remove( s ) ); break;
                case "RemoveGroups": ApplyProperties( m, s => TargetConfigReader.Item.Groups.Remove( s ) ); break;
                case "RemoveChildren": TargetConfigReader.ApplyChildren( m, false ); break;
                case "TargetContainer": ApplyProperty( m, s => TargetConfigReader.Item.Container = new NamedDependentItemContainerRef( s ) ); break;
                case "TargetGeneralization": TargetConfigReader.ApplyGeneralization( m ); break;
                default:
                    {
                        if( propName.StartsWith( "Transformer" ) ) return base.ApplyProperty( m, propName.Substring( 11 ) );
                        if( propName.StartsWith( "Target" ) ) return TargetConfigReader.ApplyProperty( m, propName.Substring( 6 ) );
                        return false;
                    }
            }
            return true;
        }

    }

}
