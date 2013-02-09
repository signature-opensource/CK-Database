#region LGPL License
/*----------------------------------------------------------------------------
* This file (CK.Core\ActivityLogger\Impl\ActivityLoggerClientBridge.cs) is part of CiviKey. 
*  
* CiviKey is free software: you can redistribute it and/or modify 
* it under the terms of the GNU Lesser General Public License as published 
* by the Free Software Foundation, either version 3 of the License, or 
* (at your option) any later version. 
*  
* CiviKey is distributed in the hope that it will be useful, 
* but WITHOUT ANY WARRANTY; without even the implied warranty of
* MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the 
* GNU Lesser General Public License for more details. 
* You should have received a copy of the GNU Lesser General Public License 
* along with CiviKey.  If not, see <http://www.gnu.org/licenses/>. 
*  
* Copyright © 2007-2012, 
*     Invenietis <http://www.invenietis.com>,
*     In’Tech INFO <http://www.intechinfo.fr>,
* All rights reserved. 
*-----------------------------------------------------------------------------*/
#endregion

using System;
using System.Collections.Generic;

namespace CK.Core
{
    /// <summary>
    /// A <see cref="IActivityLoggerClient"/> and <see cref="IMuxActivityLoggerClient"/> that marshals what it receives into another <see cref="IActivityLogger"/>
    /// by reproducing the log calls.
    /// </summary>
    public class ActivityLoggerClientBridge : MarshalByRefObject, IActivityLoggerClient, IMuxActivityLoggerClient
    {
        IActivityLogger _logger;

        /// <summary>
        /// Initialize a new <see cref="ActivityLoggerClientBridge"/> bound to an existing <see cref="IActivityLogger"/>.
        /// </summary>
        public ActivityLoggerClientBridge( IActivityLogger logger )
        {
            if( logger == null ) throw new ArgumentNullException( "logger" );
            _logger = logger;
        }

        void IActivityLoggerClient.OnFilterChanged( LogLevelFilter current, LogLevelFilter newValue )
        {
            _logger.Filter = newValue;
        }

        void IActivityLoggerClient.OnUnfilteredLog( LogLevel level, string text )
        {
            _logger.UnfilteredLog( level, text );
        }

        void IActivityLoggerClient.OnOpenGroup( IActivityLogGroup group )
        {
            _logger.OpenGroup( group.GroupLevel, group.Exception, group.GroupText );
        }

        void IActivityLoggerClient.OnGroupClosing( IActivityLogGroup group, IList<ActivityLogGroupConclusion> conclusions )
        {
            // Does nothing.
        }

        [Serializable]
        class MultiConclusion
        {
            string[] _conclusions;

            internal MultiConclusion( IReadOnlyList<ActivityLogGroupConclusion> c )
            {
                _conclusions = new string[c.Count];
                for( int i = 0; i < c.Count; ++i )
                {
                    _conclusions[i] = c[i].ToString();
                }
            }

            public object[] Conclusions { get { return _conclusions; } }

            public override string ToString()
            {
                return String.Join( ", ", _conclusions );
            }
        }

        void IActivityLoggerClient.OnGroupClosed( IActivityLogGroup group, IReadOnlyList<ActivityLogGroupConclusion> conclusions )
        {
            if( conclusions.Count == 0 ) _logger.CloseGroup();
            else if( conclusions.Count == 1 ) _logger.CloseGroup( conclusions[0].ToString() );
            else _logger.CloseGroup( new MultiConclusion( conclusions ) );
        }

        #region IMuxActivityLoggerClient relayed to protected implementation.

        void IMuxActivityLoggerClient.OnFilterChanged( IActivityLogger sender, LogLevelFilter current, LogLevelFilter newValue )
        {
            // Filter changed on a multiplexed logger: ignore it.
        }

        void IMuxActivityLoggerClient.OnUnfilteredLog( IActivityLogger sender, LogLevel level, string text )
        {
            _logger.UnfilteredLog( level, text );
        }

        void IMuxActivityLoggerClient.OnOpenGroup( IActivityLogger sender, IActivityLogGroup group )
        {
            _logger.OpenGroup( group.GroupLevel, group.Exception, group.GroupText );
        }

        void IMuxActivityLoggerClient.OnGroupClosing( IActivityLogger sender, IActivityLogGroup group, IList<ActivityLogGroupConclusion> conclusions )
        {
            // Does nothing.
        }

        void IMuxActivityLoggerClient.OnGroupClosed( IActivityLogger sender, IActivityLogGroup group, IReadOnlyList<ActivityLogGroupConclusion> conclusions )
        {
            if( conclusions.Count == 0 ) _logger.CloseGroup();
            else if( conclusions.Count == 1 ) _logger.CloseGroup( conclusions[0].ToString() );
            else _logger.CloseGroup( new MultiConclusion( conclusions ) );
        }

        #endregion
    }
}
