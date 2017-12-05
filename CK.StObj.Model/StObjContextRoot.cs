#region Proprietary License
/*----------------------------------------------------------------------------
* This file (CK.StObj.Model\StObjContextRoot.cs) is part of CK-Database. 
* Copyright © 2007-2014, Invenietis <http://www.invenietis.com>. All rights reserved. 
*-----------------------------------------------------------------------------*/
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.IO;
using System.Reflection.Emit;
using System.Threading;
using System.Diagnostics;

namespace CK.Core
{
    /// <summary>
    /// Abstract root object that is a <see cref="IStObjMap"/> and is able to build and load concrete maps thanks
    /// to static methods.
    /// </summary>
    public abstract partial class StObjContextRoot : IStObjMap
    {
        /// <summary>
        /// Holds the name of the root class.
        /// </summary>
        public static readonly string RootContextTypeName = "CK.StObj.GeneratedRootContext";
        /// <summary>
        /// Holds the name of 'Construct' method.
        /// </summary>
        public static readonly string ConstructMethodName = "StObjConstruct";

        /// <summary>
        /// Holds the name of 'Initialize' method.
        /// </summary>
        public static readonly string InitializeMethodName = "StObjInitialize";

        static readonly HashSet<Assembly> _alreadyLoaded = new HashSet<Assembly>();

        /// <summary>
        /// Loads a previously generated assembly.
        /// </summary>
        /// <param name="a">Already generated assembly.</param>
        /// <param name="runtimeBuilder">Runtime builder to use. When null, <see cref="DefaultStObjRuntimeBuilder"/> is used.</param>
        /// <param name="monitor">Optional monitor for loading operation.</param>
        /// <returns>A <see cref="IStObjMap"/> that provides access to the objects graph.</returns>
        public static IStObjMap Load( Assembly a, IStObjRuntimeBuilder runtimeBuilder = null, IActivityMonitor monitor = null )
        {
            if( a == null ) throw new ArgumentNullException( nameof( a ) );
            IActivityMonitor m = monitor ?? new ActivityMonitor( "CK.Core.StObjContextRoot.Load" );
            bool loaded;
            lock( _alreadyLoaded )
            {
                loaded = _alreadyLoaded.Contains( a );
                if( !loaded ) _alreadyLoaded.Add( a );
            }
            using( m.OpenInfo( loaded ? $"'{a.FullName}' is already loaded." : $"Loading dynamic '{a.FullName}'." ) )
            {
                try
                {
                    Type t = a.GetType( RootContextTypeName, true, false );
                    return (IStObjMap)Activator.CreateInstance( t, new object[] { m, runtimeBuilder ?? DefaultStObjRuntimeBuilder } );
                }
                catch( Exception ex )
                {
                    m.Error( "Unable to instanciate StObjMap.", ex );
                    return null;
                }
                finally
                {
                    m.CloseGroup();
                    if( monitor == null ) m.MonitorEnd();
                }
            }
        }

    }
}
