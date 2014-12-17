#region Proprietary License
/*----------------------------------------------------------------------------
* This file (CK.Setupable.Engine\Scripts\MultiScriptBase.cs) is part of CK-Database. 
* Copyright © 2007-2014, Invenietis <http://www.invenietis.com>. All rights reserved. 
*-----------------------------------------------------------------------------*/
#endregion

using CK.Core;
using System.Collections.Generic;
using System;

namespace CK.Setup
{
    public abstract class MultiScriptBase
    {
        protected readonly IActivityMonitor Monitor;
        protected readonly ISetupScript Script;

        /// <summary>
        /// Initializes a new instance of <see cref="MultiScriptBase"/>.
        /// </summary>
        /// <param name="_monitor">The _monitor to use.</param>
        /// <param name="script">Script to execute.</param>
        public MultiScriptBase( IActivityMonitor monitor, ISetupScript script )
        {
            if( monitor == null ) throw new ArgumentNullException( "_monitor" );
            if( script == null ) throw new ArgumentNullException( "script" );

            Monitor = monitor;
            Script = script;
        }

        /// <summary>
        /// Executes the script: calls <see cref="SplitScripts"/>, <see cref="PreExecuteOneScript"/> and <see cref="ExecuteOneScript"/>
        /// and manages log structure. Can be overridden (for instance to skip execution if possible).
        /// </summary>
        /// <returns>True on success, false to stop the setup process (when false an error or a fatal error SHOULD be logged).</returns>
        public virtual bool ExecuteScript()
        {
            string scriptBody = Script.GetScript();
            if( String.IsNullOrWhiteSpace( scriptBody ) ) return true;
            string scriptName = Script.Name.FileName;

            var scripts = SplitScripts( scriptBody );
            if( scripts == null ) return false;
            if( scripts.Count == 0 ) return true;

            using( scripts.Count > 1 ? Monitor.OpenInfo().Send( "Script '{0}' split in {1} scripts.", scriptName, scripts.Count ) : null )
            {
                int numScript = 0;
                foreach( var oneScript in scripts )
                {
                    using( scripts.Count > 1
                            ? Monitor.OpenTrace().Send( "Executing script n°{0}/{1}.", numScript + 1, scripts.Count )
                            : Monitor.OpenTrace().Send( "Executing '{0}'.", scriptName ) )
                    {
                        bool ok;
                        string finalScript = null;
                        try
                        {
                            finalScript = PreExecuteOneScript( numScript, oneScript );
                            ok = finalScript != null ? ExecuteOneScript( numScript, finalScript ) : true;
                            if( ok ) OnOneScriptSucceed( numScript, finalScript );
                            ++numScript;
                        }
                        catch( Exception ex )
                        {
                            Monitor.Error().Send( ex );
                            ok = false;
                        }
                        if( !ok )
                        {
                            OnScriptFailed( numScript, finalScript );
                            return false;
                        }
                    }
                }
                OnScriptSucceed();
            }
            return true;
        }

        /// <summary>
        /// Optionaly pre processes the script that can be split into multiple fragments.
        /// This default implementation does not split the script (it returns an enumerable of one script).
        /// On error, specialized implementation should return null (and log an error).
        /// </summary>
        /// <param name="scriptBody">Script text to execute.</param>
        /// <returns>Zero, one or multiple pre processed scripts. Null on error (an error or fatal error should have been logged).</returns>
        protected virtual IReadOnlyList<string> SplitScripts( string scriptBody )
        {
            return new CKReadOnlyListMono<string>( scriptBody );
        }

        /// <summary>
        /// Optionaly pre processes one script before calling <see cref="ExecuteOneScript"/>.
        /// This default implementation simply returns the <param name="scriptBody"/> unchanged.
        /// Returning null enables to ignore the script: the script execution is skipped without 
        /// stopping the setup process.
        /// </summary>
        /// <param name="numScript">Zero based script index (for multiple scripts).</param>
        /// <param name="scriptBody">Script text to pre process.</param>
        /// <returns>The processed script or null to skip it.</returns>
        protected virtual string PreExecuteOneScript( int numScript, string scriptBody )
        {
            return scriptBody;
        }

        /// <summary>
        /// Executes the given script. This default implementation calls <see cref="Executor"/>.
        /// </summary>
        /// <param name="numScript">Zero based script index (for multiple scripts).</param>
        /// <param name="scriptBody">Script text to execute.</param>
        /// <returns>False to stop the setup (in such case, an error or a fatal error SHOULD be logged).</returns>
        protected abstract bool ExecuteOneScript( int numScript, string scriptBody );

        /// <summary>
        /// Called on every successful execution (called even if <paramref name="scriptBody"/> is null 
        /// because <see cref="PreExecuteOneScript"/> returned a null script).
        /// </summary>
        /// <param name="numScript">The script index.</param>
        /// <param name="scriptBody">The successful script that can be null.</param>
        protected virtual void OnOneScriptSucceed( int numScript, string scriptBody )
        {
        }

        /// <summary>
        /// Called when an error occured.
        /// </summary>
        /// <param name="numScript">The script index of the culprit.</param>
        /// <param name="scriptBody">The culprit script.</param>
        protected virtual void OnScriptFailed( int numScript, string scriptBody )
        {
        }

        /// <summary>
        /// Called when the whole script succeed.
        /// </summary>
        /// <returns>An optional script to execute (the finalizer). Null when there is no script to execute.</returns>
        protected virtual void OnScriptSucceed()
        {
        }

    }

}
