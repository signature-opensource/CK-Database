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
    /// Helper class (that can be specialized: see <see cref="ApplyProperty(StringMatcher, string)"/>)
    /// that applies a textual configuration '"SetupConfig": {...}' from a string to a setup item.
    /// </summary>
    public class SetupConfigReader
    {
        static Regex _ckConfig = new Regex( @"""?SetupConfig""?\s*:\s*", RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture );

        /// <summary>
        /// Initializes a new <see cref="SetupConfigReader"/>.
        /// </summary>
        /// <param name="item">The item to configure. Never null.</param>
        public SetupConfigReader( IMutableSetupBaseItem item )
        {
            if( item == null ) throw new ArgumentNullException( nameof( item ) );
            Item = item;
        }

        /// <summary>
        /// Gets the item to setup.
        /// </summary>
        public IMutableSetupBaseItem Item { get; }

        /// <summary>
        /// Applies a configuration parsed from a 'SetupConfig: {...}' object definition 
        /// in the given string to the <see cref="Item"/>.
        /// </summary>
        /// <param name="monitor">The monitor that will receive errors.</param>
        /// <param name="text">The text to analyse.</param>
        /// <param name="foundConfig">True if the SetupConfig has been found, false otherwise.</param>
        /// <returns>True on success, false if an error occurred.</returns>
        public bool Apply( IActivityMonitor monitor, string text, out bool foundConfig )
        {
            foundConfig = true;
            Match match = _ckConfig.Match( text );
            if( !match.Success )
            {
                foundConfig = false;
                return true;
            }
            StringMatcher m = new StringMatcher( text, match.Index + match.Length );
            if( m.MatchChar( '{' ) ) ParseContent( m );
            if( m.IsError )
            {
                using( monitor.OpenError( "Invalid SetupConfig (expected JSON syntax): " + m.ErrorMessage ) )
                {
                    monitor.Trace( text );
                }
                return false;
            }
            return true;
        }

        /// <summary>
        /// Handles Requires, RequiredBy, Groups, Children, Container and Generalization properties.
        /// When this method returns false and <see cref="StringMatcher.IsError"/> is false, specialized 
        /// properties can be handled.
        /// </summary>
        /// <param name="m">The string matcher.</param>
        /// <param name="propName">The property name to apply.</param>
        /// <returns>
        /// True if <paramref name="propName"/> is one the basic properties, false 
        /// otherwise or if an error occurred (<see cref="StringMatcher.IsError"/> is true in such case).
        /// </returns>
        internal protected virtual bool ApplyProperty( StringMatcher m, string propName )
        {
            switch( propName )
            {
                case "Requires": ApplyProperties( m, s => Item.Requires.Add( s ) ); break;
                case "RequiredBy": ApplyProperties( m, s => Item.RequiredBy.Add( s ) ); break;
                case "Groups": ApplyProperties( m, s => Item.Groups.Add( s ) ); break;
                case "Children": ApplyChildren( m, true ); break;
                case "Container": ApplyProperty( m, s => Item.Container = new NamedDependentItemContainerRef( s ) ); break;
                case "Generalization": ApplyGeneralization( m ); break;
                default: return false;
            }
            return true;
        }

        /// <summary>
        /// Extension point: called when <see cref="ApplyProperty"/> failed.
        /// By default sets a "Unknown property" error on the <see cref="StringMatcher"/>.
        /// </summary>
        /// <param name="m">The string matcher.</param>
        /// <param name="propName">The unknown property name.</param>
        protected virtual void OnUnknownProperty( StringMatcher m, string propName )
        {
            m.SetError( "Unknown property: " + propName );
        }

        /// <summary>
        /// Creates a configuration reader for a transformer of this <see cref="Item"/>.
        /// This can be overridden to return a specialized <see cref="TransformerSetupConfigReader"/>.
        /// </summary>
        /// <param name="transfomer">
        /// The transformer item: its <see cref="ISetupObjectTransformerItem.Target"/>
        /// must be this <see cref="Item"/>.
        /// </param>
        /// <returns>A configuration reader.</returns>
        public virtual TransformerSetupConfigReader CreateTransformerConfigReader( ISetupObjectTransformerItem transfomer )
        {
            return new TransformerSetupConfigReader( transfomer, this );
        }

        void ParseContent( StringMatcher m )
        {
            while( !m.IsError
                    && !m.IsEnd
                    && m.SkipWhiteSpacesAndJSComments()
                    && !m.TryMatchChar( '}' ) )
            {
                string propName;
                if( !m.TryMatchJSONQuotedString( out propName )
                    || !m.SkipWhiteSpacesAndJSComments()
                    || !m.TryMatchChar( ':' )
                    || !m.SkipWhiteSpacesAndJSComments() ) m.SetError( @"""Identifier"" : ..." );
                else
                {
                    if( !ApplyProperty( m, propName ) )
                    {
                        OnUnknownProperty( m, propName );
                    }

                }
                if( !m.IsError )
                {
                    // Allow trailing comma.
                    m.SkipWhiteSpacesAndJSComments();
                    m.TryMatchChar( ',' );
                }
            }
        }

        internal void ApplyChildren( StringMatcher m, bool add )
        {
            var g = Item as IMutableSetupItemGroup;
            if( g == null ) m.SetError( $"Object is not a group, it can not have Children." );
            else ApplyProperties( m, add ? (Action<string>)(s => g.Children.Add( s )) : s => g.Children.Remove( s ) );
        }

        internal void ApplyGeneralization( StringMatcher m )
        {
            var o = Item as IMutableSetupItem;
            if( o == null ) m.SetError( $"Object does not support Generalization." );
            else ApplyProperty( m, s => o.Generalization = new NamedDependentItemRef( s ) );
        }

        internal void ApplyProperties( StringMatcher m, Action<string> a )
        {
            string content;
            if( m.TryMatchJSONQuotedString( out content ) )
            {
                a( content );
            }
            else if( m.MatchChar( '[' ) )
            {
                do
                {
                    m.SkipWhiteSpacesAndJSComments();
                    if( m.TryMatchChar( ']' ) ) break;
                    if( !m.TryMatchJSONQuotedString( out content ) ) m.SetError( @"Expected ""full name"" in [""full name 1"", ...]." );
                    else
                    {
                        a( content );
                        // Allow trailing comma.
                        m.SkipWhiteSpacesAndJSComments();
                        m.TryMatchChar( ',' );
                    }
                }
                while( !m.IsEnd && !m.IsError );
            }
            else m.SetError( @"Expected ""full name"" in [""full name 1"", ...]." );
        }

        internal void ApplyProperty( StringMatcher m, Action<string> a )
        {
            string s;
            if( !m.TryMatchJSONQuotedString( out s, allowNull: true ) ) m.SetError( @"Expected ""full name""." );
            else a( s );
        }
    }

}
