using CK.Core;
using System;
using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;

namespace CK.Setup;

/// <summary>
/// Helper class (that can be specialized: see <see cref="ApplyProperty(ref ROSpanCharMatcher, string)"/>)
/// that applies a textual configuration '"SetupConfig": {...}' from a string to a setup item.
/// </summary>
public class SetupConfigReader
{
    static Regex _ckConfig = new Regex( @"""?SetupConfig""?\s*:", RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture );

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
    /// <param name="text">The text to analyze.</param>
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
        var m = new ROSpanCharMatcher( text.AsSpan( match.Index ) );
        m.Head.TryMatch( '"' );
        m.Head.TryMatch( "SetupConfig" );
        m.Head.TryMatch( '"' );
        m.SkipWhiteSpaces();
        m.Head.TryMatch( ':' );
        m.SkipWhiteSpaces();
        if( m.TryMatch( '{' ) ) ParseContent( ref m );
        if( m.HasError )
        {
            using( monitor.OpenError( "Invalid SetupConfig (in JSON-like syntax):" ) )
            {
                monitor.Trace( text.Substring( match.Index ) );
                monitor.Error( m.GetErrorMessage() );
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
    internal protected virtual bool ApplyProperty( ref ROSpanCharMatcher m, string propName )
    {
        switch( propName )
        {
            case "Requires": ApplyProperties( ref m, s => Item.Requires.Add( s ) ); break;
            case "RequiredBy": ApplyProperties( ref m, s => Item.RequiredBy.Add( s ) ); break;
            case "Groups": ApplyProperties( ref m, s => Item.Groups.Add( s ) ); break;
            case "Children": ApplyChildren( ref m, true ); break;
            case "Container": ApplyProperty( ref m, s => Item.Container = new NamedDependentItemContainerRef( s ) ); break;
            case "Generalization": ApplyGeneralization( ref m ); break;
            default: return false;
        }
        return true;
    }

    /// <summary>
    /// Extension point: called when <see cref="ApplyProperty(ref ROSpanCharMatcher, string)"/> failed.
    /// By default adds a "Known property" expectation on the <see cref="ROSpanCharMatcher"/>.
    /// </summary>
    /// <param name="m">The string matcher.</param>
    /// <param name="propName">The unknown property name.</param>
    protected virtual void OnUnknownProperty( ref ROSpanCharMatcher m, string propName )
    {
        m.AddExpectation( $"Known property ('{propName}' is not known)" );
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

    void ParseContent( ref ROSpanCharMatcher m )
    {
        while( !m.HasError
                && !m.Head.IsEmpty
                && m.Head.SkipWhiteSpacesAndJSComments()
                && !m.Head.TryMatch( '}' ) )
        {
            string propName;
            if( !m.TryMatchJSONQuotedString( out propName )
                || !m.Head.SkipWhiteSpacesAndJSComments()
                || !m.Head.TryMatch( ':' )
                || !m.Head.SkipWhiteSpacesAndJSComments() ) m.AddExpectation( @"""Identifier"" : ..." );
            else
            {
                if( !ApplyProperty( ref m, propName ) )
                {
                    OnUnknownProperty( ref m, propName );
                }

            }
            if( !m.HasError )
            {
                // Allow trailing comma.
                m.Head.SkipWhiteSpacesAndJSComments();
                m.Head.TryMatch( ',' );
            }
        }
    }

    internal void ApplyChildren( ref ROSpanCharMatcher m, bool add )
    {
        var g = Item as IMutableSetupItemGroup;
        if( g == null ) m.AddExpectation( $"No Children since Object is not a group." );
        else ApplyProperties( ref m, add ? (Action<string>)(s => g.Children.Add( s )) : s => g.Children.Remove( s ) );
    }

    internal void ApplyGeneralization( ref ROSpanCharMatcher m )
    {
        var o = Item as IMutableSetupItem;
        if( o == null ) m.AddExpectation( $"No Generalization Object does not support it." );
        else ApplyProperty( ref m, s => o.Generalization = new NamedDependentItemRef( s ) );
    }

    internal void ApplyProperties( ref ROSpanCharMatcher m, Action<string> a )
    {
        string content;
        if( m.TryMatchJSONQuotedString( out content ) )
        {
            a( content );
        }
        else if( m.TryMatch( '[' ) )
        {
            do
            {
                m.Head.SkipWhiteSpacesAndJSComments();
                if( m.Head.TryMatch( ']' ) ) break;
                if( !m.TryMatchJSONQuotedString( out content ) ) m.AddExpectation( @"""full name"" in [""full name 1"", ...]." );
                else
                {
                    a( content );
                    // Allow trailing comma.
                    m.Head.SkipWhiteSpacesAndJSComments();
                    m.Head.TryMatch( ',' );
                }
            }
            while( !m.Head.IsEmpty && !m.HasError );
        }
        else m.AddExpectation( @"""full name"" in [""full name 1"", ...]." );
    }

    internal void ApplyProperty( ref ROSpanCharMatcher m, Action<string> a )
    {
        string s;
        if( !m.TryMatchJSONQuotedString( out s, allowNull: true ) ) m.AddExpectation( @"Expected ""full name""." );
        else a( s );
    }
}
