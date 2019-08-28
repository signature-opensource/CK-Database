using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using CK.Core;
using System.Diagnostics;

namespace CK.SqlServer
{
    /// <summary>
    /// Parses scripts string to handle 'go', 'begin/endsp' and 'begin/endscript'.
    /// </summary>
    public class SimpleScriptTagHandler
    {
        static Regex _rTag = new Regex( @"^go(\s+|$)|(?<!^\s*--.*)\s*--\[(?<1>(=/?)?[a-z]{3,})(\s+(?<2>\w+)\s*)?]\s*", RegexOptions.Multiline | RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture | RegexOptions.CultureInvariant | RegexOptions.Compiled );

        [Flags]
        enum TokenType
        {
            None = 0,

            IsGO = 1,
            IsText = 2,
            IsScript = 4,
            IsSP = 8,

            IsExpanded = 32,
            StartExpanded = IsExpanded | 64,
            StopExpanded = IsExpanded | 128,

            IsBegin = 256,
            IsEnd = 512,
        }

        static string TokenTypeDisplayName( TokenType type )
        {
            switch( type )
            {
                case TokenType.IsScript: return "script";
                case TokenType.IsSP: return "sp";
                default: return type.ToString();
            }
        }

        class Token
        {
            internal Token( Match m, TokenType type )
            {
                Index = m.Index;
                Length = m.Length;
                Type = type;
                string label = m.Groups[2].Value;
                if( !string.IsNullOrWhiteSpace( label ) ) Label = label;
            }

            internal Token( int idxText, int lenText )
            {
                Index = idxText;
                Length = lenText;
                Type = TokenType.IsText;
            }

            public int Index { get; set; }
            public int Length { get; set; }
            public TokenType Type { get; set; }
            public string Label { get; set; }
            public bool IsExpanded => (Type & TokenType.IsExpanded) != 0;
            public bool IsBegin => (Type & TokenType.IsBegin) != 0;
            public bool IsEnd => (Type & TokenType.IsEnd) != 0;
            public bool IsGo => (Type & TokenType.IsGO) != 0;
            public bool IsScript => (Type & TokenType.IsScript) != 0;
            public bool IsSP => (Type & TokenType.IsSP) != 0;
            public bool IsText => (Type & TokenType.IsText) != 0;
            public bool IsScriptBegin => (Type & (TokenType.IsScript | TokenType.IsBegin)) == (TokenType.IsScript | TokenType.IsBegin);
            public bool IsScriptEnd => (Type & (TokenType.IsScript | TokenType.IsEnd)) == (TokenType.IsScript | TokenType.IsEnd);

            internal void MergeExpanded( Token t )
            {
                Debug.Assert( (Type & TokenType.StartExpanded) == TokenType.StartExpanded );
                Debug.Assert( (t.Type & TokenType.StopExpanded) == TokenType.StopExpanded );
                Length += (t.Index - Index) + t.Length;
                Type = (Type & ~TokenType.StartExpanded) | TokenType.IsExpanded;
            }
        }

        string _text;
        StringBuilder _sb;
        int _nbScripts;
        List<Token> _tokens;
        bool _expandSuccess;

        /// <summary>
        /// Encapsulates one script.
        /// </summary>
        public class Script
        {
            internal Script( string label, string body )
            {
                Label = label;
                Body = body;
            }

            /// <summary>
            /// The label of the script when <see cref="IsScriptTag"/> is true. 
            /// Null otherwise.
            /// </summary>
            public readonly string Label;

            /// <summary>
            /// The script body with expanded tags if any.
            /// </summary>
            public readonly string Body;

            /// <summary>
            /// Gets whether this script is included in a [beginscript]/[endscript] tag.
            /// When true, this script has a not null <see cref="Label"/> automatically computed like "AutoNum0" or sets in the script.
            /// </summary>
            public bool IsScriptTag => Label != null;
        }

        /// <summary>
        /// Initializes a new <see cref="SimpleScriptTagHandler"/> on a script.
        /// </summary>
        /// <param name="script"></param>
        public SimpleScriptTagHandler( string script )
        {
            if( script == null ) throw new ArgumentNullException( "script" );
            _text = script.Trim();
        }

        /// <summary>
        /// Gets the original script (the  constructor parameter but trimmed).
        /// </summary>
        public string OriginalScript => _text;

        /// <summary>
        /// Processes the script: <see cref="SplitScript"/> must be called to retrieve the script parts.
        /// Returns false on error: detailed error(s) information are logged in <paramref name="monitor"/>.
        /// </summary>
        /// <param name="monitor">Required monitor.</param>
        /// <param name="scriptAllowed">True to allow --[beginscript] / --[endscript] tags.</param>
        /// <param name="goInsideScriptAllowed">True to allow GO separator inside scripts.</param>
        /// <returns>True on success, false on error(s).</returns>
        public bool Expand( IActivityMonitor monitor, bool scriptAllowed, bool goInsideScriptAllowed = false )
        {
            if( monitor == null ) throw new ArgumentNullException( "monitor" );
            if( _sb != null ) throw new InvalidOperationException();

            if( !ParseTokens( monitor ) ) return false;
            if( !HandleExpanded( monitor ) ) return false;
            if( !CheckNested( monitor, TokenType.IsScript, scriptAllowed, goInsideScriptAllowed ) ) return false;
            if( !CheckNested( monitor, TokenType.IsSP, true, false ) ) return false;
            return (_expandSuccess = _tokens.Count > 0 ? DoExpand( monitor ) : true);
        }

        /// <summary>
        /// Gets the number of scripts (available after a call to <see cref="Expand"/>).
        /// </summary>
        public int ScriptCount => _nbScripts;

        /// <summary>
        /// Gets the list of expanded <see cref="Script"/> objects.
        /// </summary>
        /// <returns>List of scripts (possibly empty).</returns>
        public List<Script> SplitScript()
        {
            if( !_expandSuccess ) throw new InvalidOperationException();
            List<Script> result = new List<Script>();
            if( _tokens.Count == 0 ) return result;

            Debug.Assert( _tokens[0].Index == 0 && _tokens[_tokens.Count - 1].Index + _tokens[_tokens.Count - 1].Length == _sb.Length );
            bool wasGo = false;
            int lenT;
            int idxText = 0;
            foreach( Token t in _tokens )
            {
                if( t.IsGo || t.IsScriptBegin )
                {
                    // Pushes the text before.
                    lenT = t.Index - idxText;
                    if( lenT > 0 ) result.Add( new Script( null, _sb.ToString( idxText, lenT ) ) );

                    idxText = t.Index;
                    // Skips the GO itself.
                    if( (wasGo = t.IsGo) ) idxText += t.Length;
                }
                else if( t.IsScriptEnd )
                {
                    // On end script, injects the token content itself in the current text flow.
                    lenT = (t.Index + t.Length) - idxText;
                    if( lenT > 0 ) result.Add( new Script( wasGo ? null : t.Label, _sb.ToString( idxText, lenT ) ) );
                    idxText = t.Index + t.Length;
                }
            }
            lenT = _sb.Length - idxText;
            if( lenT > 0 ) result.Add( new Script( null, _sb.ToString( idxText, lenT ) ) );

            return result;
        }

        bool HandleExpanded( IActivityMonitor monitor )
        {
            Token currentStart = null;
            for( int i = 0; i < _tokens.Count; ++i )
            {
                Token t = _tokens[i];
                if( (t.Type & TokenType.StopExpanded) == TokenType.StopExpanded )
                {
                    if( currentStart == null )
                    {
                        LogError( monitor, $"Unexpected {_text.Substring( t.Index, t.Length )}: missing expanded start marker." );
                        return false;
                    }
                    if( (currentStart.Type & ~TokenType.StartExpanded) != (t.Type & ~TokenType.StopExpanded) )
                    {
                        LogError( monitor, $"Expanded markers mismatch: {_text.Substring( currentStart.Index, currentStart.Length )} / {_text.Substring( t.Index, t.Length )}." );
                        return false;
                    }
                    currentStart.MergeExpanded( t );
                    _tokens.RemoveAt( i-- );
                    currentStart = null;
                }
                else if( (t.Type & TokenType.StartExpanded) == TokenType.StartExpanded )
                {
                    if( currentStart != null )
                    {
                        LogError( monitor, $"Unexpected {_text.Substring( t.Index, t.Length )}: duplicate expanded start marker." );
                        return false;
                    }
                    currentStart = t;
                }
                else if( currentStart != null )
                {
                    LogError( monitor, $"Expected {_text.Substring( currentStart.Index, currentStart.Length )} stop marker instead of {_text.Substring( t.Index, t.Length )}." );
                    return false;
                }
            }
            if( currentStart != null )
            {
                LogError( monitor, $"Expected {_text.Substring( currentStart.Index, currentStart.Length )} stop marker." );
                return false;
            }
            return true;
        }

        void LogError( IActivityMonitor monitor, string msg )
        {
            using( monitor.OpenError( msg ) )
            {
                monitor.Trace( _text );
            }
        }

        bool CheckNested( IActivityMonitor monitor, TokenType type, bool allowed, bool allowInnerGo )
        {
            int scriptLevel = 0;
            List<string> labels = new List<string>();
            for( int i = 0; i < _tokens.Count; ++i )
            {
                Token t = _tokens[i];
                if( !allowInnerGo && t.IsGo && scriptLevel > 0 )
                {
                    LogError( monitor, $"Invalid GO batch separator inside --[begin{TokenTypeDisplayName( type )}] ... --[end{TokenTypeDisplayName( type )}]." );
                    return false;
                }
                if( (t.Type & type) != 0 )
                {
                    if( !allowed )
                    {
                        LogError( monitor, $"Invalid {_text.Substring( t.Index, t.Length ).Trim()} in this context." );
                        return false;
                    }
                    bool ignored = false;
                    if( t.IsBegin )
                    {
                        if( ++scriptLevel > 1 )
                        {
                            if( t.IsScript ) --_nbScripts;
                            ignored = true;
                        }
                        else
                        {
                            if( t.Label == null ) t.Label = String.Format( "AutoNum{0}", i );
                            // Adds the label to the list: the "current" label is always the last one.
                            if( t.Label != null )
                            {
                                if( labels.Contains( t.Label ) )
                                {
                                    LogError( monitor, $"Label '{t.Label}' is already used: labels must be unique." );
                                    return false;
                                }
                                labels.Add( t.Label );
                            }
                        }
                    }
                    else
                    {
                        Debug.Assert( t.IsEnd );
                        if( scriptLevel == 0 )
                        {
                            LogError( monitor, $"Unexpected {_text.Substring( t.Index, t.Length ).Trim()} found." );
                            return false;
                        }
                        if( scriptLevel-- > 1 )
                        {
                            ignored = true;
                        }
                        else
                        {
                            string current = labels.LastOrDefault();
                            if( t.Label != null )
                            {
                                if( current == null )
                                {
                                    LogError( monitor, $"Unknown label: label '{t.Label}' has never be defined." );
                                    return false;
                                }
                                if( current != t.Label )
                                {
                                    LogError( monitor, $"Label mismatch: label '{t.Label}' does not match current one '{current}'." );
                                    return false;
                                }
                                labels.Add( t.Label );
                            }
                            if( current != null ) t.Label = current;
                        }
                    }
                    if( ignored )
                    {
                        monitor.Warn( $"Nested {_text.Substring( t.Index, t.Length ).Trim()} found. It is ignored." );
                        _tokens.RemoveAt( i-- );
                    }
                }
            }
            if( scriptLevel > 0 )
            {
                LogError( monitor, $"Unbalanced --[begin{TokenTypeDisplayName( type )}] ... --[end{TokenTypeDisplayName( type )}] found." );
                return false;
            }
            return true;
        }

        bool ParseTokens( IActivityMonitor monitor )
        {
            _nbScripts = 0;
            _tokens = new List<Token>();
            int idxText = 0;
            int lenText;
            Match m = _rTag.Match( _text );
            while( m.Success )
            {
                lenText = m.Index - idxText;
                if( lenText > 0 ) _tokens.Add( new Token( idxText, lenText ) );
                idxText = m.Index + m.Length;

                if( _text[m.Index] == 'g' || _text[m.Index] == 'G' ) _tokens.Add( new Token( m, TokenType.IsGO ) );
                else
                {
                    TokenType t = TokenType.None;
                    int idxTag = m.Groups[1].Index;
                    // Handles --[= or --[=/.
                    if( _text[idxTag] == '=' )
                    {
                        ++idxTag;
                        if( _text[idxTag] == '/' )
                        {
                            ++idxTag;
                            t |= TokenType.StopExpanded;
                        }
                        else t |= TokenType.StartExpanded;
                    }
                    // Handles beginXXX or endXXX.
                    bool isBeginOrEnd = false;
                    if( String.Compare( _text, idxTag, "end", 0, 3, StringComparison.OrdinalIgnoreCase ) == 0 )
                    {
                        idxTag += 3;
                        t |= TokenType.IsEnd;
                        isBeginOrEnd = true;
                    }
                    else if( String.Compare( _text, idxTag, "begin", 0, 5, StringComparison.OrdinalIgnoreCase ) == 0 )
                    {
                        idxTag += 5;
                        t |= TokenType.IsBegin;
                        isBeginOrEnd = true;
                    }
                    // Handles XXX.
                    bool knownMark = false;
                    if( isBeginOrEnd )
                    {
                        if( String.Compare( _text, idxTag, "script", 0, 6, StringComparison.OrdinalIgnoreCase ) == 0 )
                        {
                            if( (t & TokenType.IsBegin) != 0 ) ++_nbScripts;
                            t |= TokenType.IsScript;
                            knownMark = true;
                        }
                        else if( String.Compare( _text, idxTag, "sp", 0, 2, StringComparison.OrdinalIgnoreCase ) == 0 )
                        {
                            t |= TokenType.IsSP;
                            knownMark = true;
                        }
                    }
                    if( knownMark )
                    {
                        _tokens.Add( new Token( m, t ) );
                    }
                    else
                    {
                        monitor.Warn( $"Unrecognized sql mark '{m.Value.Trim()}'. It is ignored." );
                    }
                }
                m = m.NextMatch();
            }
            lenText = _text.Length - idxText;
            if( lenText > 0 ) _tokens.Add( new Token( idxText, lenText ) );
            return true;
        }

        #region Replacements
        static internal readonly string BeginScript =
@"--[=beginscript#SLABELS#]
set nocount on; declare @CKScTC int = @@TRANCOUNT;
beginsp: if @CKScTC = 0 begin tran; else begin save transaction ck#LABEL#; end begin try
--[=/beginscript]
";

        static internal readonly string EndScript =
@"--[=endscript#SLABELS#]
end try begin catch if @CKScTC = 0 rollback; else if XACT_STATE() = 1 rollback transaction ck#LABEL#; exec CKCore.sErrorRethrow @@ProcId; set @CKScTC = 1; end catch;
endscript: if @CKScTC = 0 commit;
--[=/endscript]
";
        static internal readonly string BeginSP =
@"
--[=beginsp]
set nocount on; declare @SPCallTC int = @@TRANCOUNT, @SPCallId sysname; 
beginsp: if @SPCallTC = 0 begin tran; else begin set @SPCallId = cast(32*cast(@@PROCID as bigint)+@@NESTLEVEL as varchar); save transaction @SPCallId; end begin try
--[=/beginsp]
";
        static internal readonly string EndSPWithoutRecoverableError =
@"
--[=endsp]
end try
begin catch if @SPCallTC = 0 rollback; else if XACT_STATE() = 1 rollback transaction @SPCallId; exec CKCore.sErrorRethrow @@ProcId; return -1; end catch;
endsp: if @SPCallTC = 0 commit; return 0;
--[=/endsp]
";
        #endregion

        bool DoExpand( IActivityMonitor monitor )
        {
            Debug.Assert( _sb == null );
            Debug.Assert( _tokens[0].Index == 0 && _tokens[_tokens.Count - 1].Index + _tokens[_tokens.Count - 1].Length == _text.Length );
            _sb = new StringBuilder( _text );
            for( int i = 0; i < _tokens.Count; ++i )
            {
                Token t = _tokens[i];
                if( !t.IsExpanded )
                {
                    if( t.IsScript )
                    {
                        if( t.IsBegin )
                        {
                            Replace( i, t, BeginScript );
                        }
                        else
                        {
                            Debug.Assert( t.IsEnd );
                            Replace( i, t, EndScript );
                        }
                    }
                    else if( t.IsSP )
                    {
                        if( t.IsBegin )
                        {
                            Replace( i, t, BeginSP );
                        }
                        else
                        {
                            Debug.Assert( t.IsEnd );
                            Replace( i, t, EndSPWithoutRecoverableError );
                        }
                    }
                }
            }
            Debug.Assert( _tokens[0].Index == 0 && _tokens[_tokens.Count - 1].Index + _tokens[_tokens.Count - 1].Length == _sb.Length );
            return true;
        }

        void Replace( int i, Token t, string expansion )
        {
            Debug.Assert( t.Label == null || !String.IsNullOrWhiteSpace( t.Label ) );
            expansion = expansion.Replace( "#SLABELS#", t.Label != null ? " " + t.Label + " " : String.Empty );
            expansion = expansion.Replace( "#LABEL#", t.Label != null ? t.Label : String.Empty );

            int deltaLength = expansion.Length - t.Length;
            _sb.Remove( t.Index, t.Length );
            _sb.Insert( t.Index, expansion );
            t.Length = expansion.Length;
            t.Type = t.Type | TokenType.IsExpanded;
            while( ++i < _tokens.Count ) _tokens[i].Index += deltaLength;
        }

    }

}
