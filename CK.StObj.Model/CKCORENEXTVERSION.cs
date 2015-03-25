using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CK.Core;

namespace CK.Setup
{
    public static class CKCORENEXTVERSION
    {
        class ErrorTracker : IActivityMonitorClient
        {
            readonly Action _onError;

            public ErrorTracker( Action onError )
            {
                _onError = onError;
            }

            public void OnUnfilteredLog( ActivityMonitorLogData data )
            {
                if( data.MaskedLevel >= LogLevel.Error )
                {
                    _onError();
                }
            }

            public void OnOpenGroup( IActivityLogGroup group )
            {
                if( group.MaskedGroupLevel >= LogLevel.Error )
                {
                    _onError();
                }
            }

            public void OnGroupClosing( IActivityLogGroup group, ref List<ActivityLogGroupConclusion> conclusions )
            {
            }

            public void OnGroupClosed( IActivityLogGroup group, IReadOnlyList<ActivityLogGroupConclusion> conclusions )
            {
            }

            public void OnTopicChanged( string newTopic, string fileName, int lineNumber )
            {
            }

            public void OnAutoTagsChanged( CKTrait newTrait )
            {
            }
        }

        /// <summary>
        /// Enables simple "using" syntax to easily detect <see cref="LogLevel.Fatal"/> or <see cref="LogLevel.Error"/>.
        /// </summary>
        /// <param name="this">This <see cref="IActivityMonitor"/>.</param>
        /// <param name="onError">An action that is called whenever an Error or Fatal error occurs.</param>
        /// <returns>A <see cref="IDisposable"/> object used to manage the scope of this handler.</returns>
        public static IDisposable OnError( this IActivityMonitor @this, Action onError )
        {
            if( @this == null ) throw new NullReferenceException( "this" );
            if( onError == null ) throw new ArgumentNullException( "onError" );
            ErrorTracker tracker = new ErrorTracker( onError );
            @this.Output.RegisterClient( tracker );
            return Util.CreateDisposableAction( () => @this.Output.UnregisterClient( tracker ) );
        }
    }
}
