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
    /// or to a transformer and a target object.
    /// </summary>
    public class SetupConfigReader
    {
        static Regex _ckConfig = new Regex( @"""?SetupConfig""?\s*:\s*", RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture );

        /// <summary>
        /// Applies a configuration parsed from a 'SetupConfig: {...}' object definition 
        /// in the given string to a setup item or to a transformer and its target setup item.
        /// </summary>
        /// <param name="monitor">The monitor that will receive errors.</param>
        /// <param name="text">The text to analyse: this is the SetupConfig of the <paramref name="transformer"/> if it is not null.</param>
        /// <param name="transformer">Transformer: when not null, the <paramref name="text"/> is the transformer's config.</param>
        /// <param name="target">The target must never be null: it must be the <see cref="ISetupObjectTransformerItem.Target"/> if <paramref name="transformer"/> is not null.</param>
        /// <param name="foundConfig">True if the SetupConfig has been found, false otherwise.</param>
        /// <returns>True on success, false if an error occurred.</returns>
        public bool Apply( IActivityMonitor monitor, string text, ISetupObjectTransformerItem transformer, IMutableSetupBaseItem target, out bool foundConfig )
        {
            if( target == null ) throw new ArgumentNullException( nameof( target ) );
            if( transformer != null && transformer.Target != target ) throw new ArgumentException( $"{nameof(target)} must be {nameof( transformer )}'s Target." );

            foundConfig = true;
            Match match = _ckConfig.Match( text );
            if( !match.Success )
            {
                foundConfig = false;
                return true;
            }
            StringMatcher m = new StringMatcher( text, match.Index + match.Length );
            if( m.MatchChar( '{' ) ) ParseContent( m, transformer, target );
            if( m.IsError )
            {
                monitor.Error().Send( m.ErrorMessage );
                return false;
            }
            return true;
        }

        void ParseContent( StringMatcher m, ISetupObjectTransformerItem transformer, IMutableSetupBaseItem target )
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
                    if( transformer != null )
                    {
                        if( propName.StartsWith( "Transformer" ) )
                        {
                            if( !ParseItemProperties( m, propName.Substring( 11 ), transformer ) )
                            {
                                OnUnknownProperty( m, propName, transformer, target );
                            }
                        }
                        else
                        {
                            switch( propName )
                            {
                                case "AddRequires": ApplyProperties( m, s => target.Requires.Add( s ) ); break;
                                case "AddRequiredBy": ApplyProperties( m, s => target.RequiredBy.Add( s ) ); break;
                                case "AddGroups": ApplyProperties( m, s => target.Groups.Add( s ) ); break;
                                case "AddChildren": ApplyChildren( m, target, true ); break;
                                case "RemoveRequires": ApplyProperties( m, s => target.Requires.Remove( s ) ); break;
                                case "RemoveRequiredBy": ApplyProperties( m, s => target.RequiredBy.Remove( s ) ); break;
                                case "RemoveGroups": ApplyProperties( m, s => target.Groups.Remove( s ) ); break;
                                case "RemoveChildren": ApplyChildren( m, target, false ); break;
                                case "TargetContainer": ApplyProperty( m, s => target.Container = new NamedDependentItemContainerRef( s ) ); break;
                                case "TargetGeneralization": ApplyGeneralization( m, target ); break;
                                default: OnUnknownProperty( m, propName, transformer, target ); break;
                            }
                        }
                    }
                    else if( !ParseItemProperties( m, propName, target ) )
                    {
                        OnUnknownProperty( m, propName, null, target );
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

        /// <summary>
        /// Extension point: called when an unknown property name is found with the matcher head on 
        /// the start of the property's content.
        /// By default sets a "Unknown property" error on the <see cref="StringMatcher"/>.
        /// </summary>
        /// <param name="m">The string matcher.</param>
        /// <param name="propName">The unknown property name.</param>
        /// <param name="transformer">The transformer. Null when applying to actual target.</param>
        /// <param name="target">The target of the apply call.</param>
        protected virtual void OnUnknownProperty( StringMatcher m, string propName, ISetupObjectTransformerItem transformer, IMutableSetupBaseItem target )
        {
            m.SetError( "Unknown property: " + propName );
        }

        bool ParseItemProperties( StringMatcher m, string propName, IMutableSetupBaseItem target )
        {
            switch( propName )
            {
                case "Requires": ApplyProperties( m, s => target.Requires.Add( s ) ); break;
                case "RequiredBy": ApplyProperties( m, s => target.RequiredBy.Add( s ) ); break;
                case "Groups": ApplyProperties( m, s => target.Groups.Add( s ) ); break;
                case "Children": ApplyChildren( m, target, true ); break;
                case "Container": ApplyProperty( m, s => target.Container = new NamedDependentItemContainerRef( s ) ); break;
                case "Generalization": ApplyGeneralization( m, target ); break;
                default: return false;
            }
            return true;
        }
        void ApplyChildren( StringMatcher m, IMutableSetupBaseItem target, bool add )
        {
            var g = target as IMutableSetupItemGroup;
            if( g == null ) m.SetError( $"Object is not a group, it can not have Children." );
            else ApplyProperties( m, add ? (Action<string>)(s => g.Children.Add( s )) : s => g.Children.Remove( s ) );
        }

        void ApplyGeneralization( StringMatcher m, IMutableSetupBaseItem target )
        {
            var o = target as IMutableSetupItem;
            if( o == null ) m.SetError( $"Object does not support Generalization." );
            else ApplyProperty( m, s => o.Generalization = new NamedDependentItemRef( s ) );
        }

        void ApplyProperties( StringMatcher m, Action<string> a )
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
                    if( !m.TryMatchJSONQuotedString( out content ) ) m.SetError( @"Expected ""full name""." );
                    else
                    {
                        a( content );
                        // Allow trailing comma.
                        m.SkipWhiteSpacesAndJSComments();
                        m.TryMatchChar( ',' );
                    }
                }
                while( !m.IsEnd && !m.IsError && !m.TryMatchChar( ']' ) );
            }
            else m.SetError( @"Expected ""full name"" or [""full name 1"", ...]." );
        }

        void ApplyProperty( StringMatcher m, Action<string> a )
        {
            string s;
            if( !m.TryMatchJSONQuotedString( out s, allowNull: true ) ) m.SetError( @"Expected ""full name""." );
            else a( s );
        }
    }

}
