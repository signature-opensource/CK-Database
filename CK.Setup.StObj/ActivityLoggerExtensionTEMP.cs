//using System;
//using System.Linq;
//using System.Collections.Generic;
//using System.Text;

//namespace CK.Core
//{
//    public static class ActivityLoggerExtensionTEMP
//    {
//        /// <summary>
//        /// Enables simple "using" syntax to easily detect <see cref="LogLevel.Fatal"/>, <see cref="LogLevel.Error"/> or <see cref="LogLevel.Warn"/>.
//        /// </summary>
//        /// <param name="this">This <see cref="IActivityLogger"/>.</param>
//        /// <param name="fatalErrorWarnCount">An action that accepts three counts for fatals, errors and warnings.</param>
//        /// <param name="asMuxClient">Optionaly registers the handler to also catch entries emitted by other loggers that are bound to this one.</param>
//        /// <returns>A <see cref="IDisposable"/> object used to manage the scope of this handler.</returns>
//        public static IDisposable CatchCounter( this IActivityLogger @this, Action<int, int, int> fatalErrorWarnCount, bool asMuxClient = false )
//        {
//            if( fatalErrorWarnCount == null ) throw new ArgumentNullException( "fatalErrorWarnCount" );
//            ActivityLoggerErrorCounter errorCounter = new ActivityLoggerErrorCounter() { GenerateConclusion = false };
//            if( asMuxClient )
//                @this.Output.RegisterMuxClient( errorCounter );
//            else @this.Output.RegisterClient( errorCounter );
//            return Util.CreateDisposableAction( () =>
//            {
//                if( asMuxClient )
//                    @this.Output.UnregisterMuxClient( errorCounter );
//                else @this.Output.UnregisterClient( errorCounter );
//                if( errorCounter.Current.HasWarnOrError ) fatalErrorWarnCount( errorCounter.Current.FatalCount, errorCounter.Current.ErrorCount, errorCounter.Current.WarnCount );
//            } );
//        }

//        /// <summary>
//        /// Enables simple "using" syntax to easily detect <see cref="LogLevel.Fatal"/> and <see cref="LogLevel.Error"/>.
//        /// </summary>
//        /// <param name="this">This <see cref="IActivityLogger"/>.</param>
//        /// <param name="fatalErrorWarnCount">An action that accepts two counts for fatals and errors.</param>
//        /// <param name="asMuxClient">Optionaly registers the handler to also catch entries emitted by other loggers that are bound to this one.</param>
//        /// <returns>A <see cref="IDisposable"/> object used to manage the scope of this handler.</returns>
//        public static IDisposable CatchCounter( this IActivityLogger @this, Action<int, int> fatalErrorCount, bool asMuxClient = false )
//        {
//            if( fatalErrorCount == null ) throw new ArgumentNullException( "fatalErrorCount" );
//            ActivityLoggerErrorCounter errorCounter = new ActivityLoggerErrorCounter() { GenerateConclusion = false };
//            if( asMuxClient )
//                @this.Output.RegisterMuxClient( errorCounter );
//            else @this.Output.RegisterClient( errorCounter );
//            return Util.CreateDisposableAction( () =>
//            {
//                if( asMuxClient )
//                    @this.Output.UnregisterMuxClient( errorCounter );
//                else @this.Output.UnregisterClient( errorCounter );
//                if( errorCounter.Current.HasError ) fatalErrorCount( errorCounter.Current.FatalCount, errorCounter.Current.ErrorCount );
//            } );
//        }
//        /// <summary>
//        /// Enables simple "using" syntax to easily detect <see cref="LogLevel.Fatal"/> or <see cref="LogLevel.Error"/>.
//        /// </summary>
//        /// <param name="this">This <see cref="IActivityLogger"/>.</param>
//        /// <param name="fatalErrorWarnCount">An action that accepts one count that sums fatals and errors.</param>
//        /// <param name="asMuxClient">Optionaly registers the handler to also catch entries emitted by other loggers that are bound to this one.</param>
//        /// <returns>A <see cref="IDisposable"/> object used to manage the scope of this handler.</returns>
//        public static IDisposable CatchCounter( this IActivityLogger @this, Action<int> fatalOrErrorCount, bool asMuxClient = false )
//        {
//            if( fatalOrErrorCount == null ) throw new ArgumentNullException( "fatalErrorCount" );
//            ActivityLoggerErrorCounter errorCounter = new ActivityLoggerErrorCounter() { GenerateConclusion = false };
//            if( asMuxClient )
//                @this.Output.RegisterMuxClient( errorCounter );
//            else @this.Output.RegisterClient( errorCounter );
//            return Util.CreateDisposableAction( () =>
//            {
//                if( asMuxClient )
//                    @this.Output.UnregisterMuxClient( errorCounter );
//                else @this.Output.UnregisterClient( errorCounter );
//                if( errorCounter.Current.HasError ) fatalOrErrorCount( errorCounter.Current.FatalCount + errorCounter.Current.ErrorCount );
//            } );
//        }
//    }
//}
