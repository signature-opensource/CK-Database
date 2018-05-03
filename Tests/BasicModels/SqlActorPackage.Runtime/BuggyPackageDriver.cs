using CK.Core;
using CK.Setup;
using CK.SqlServer.Setup;
using CK.Text;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using System.Xml.Linq;

namespace SqlActorPackage.Runtime
{
    public class BuggyPackageDriver : SqlPackageBaseItemDriver
    {
        readonly bool ReturnError;
        readonly SetupCallGroupStep ErrorStep;
        readonly bool ErrorBeforeHandlers;
        readonly bool ErrorFromOnStep;

        static NormalizedPath GetThisFile( [CallerFilePath]string p = null ) => p;

        public BuggyPackageDriver( BuildInfo info, IActivityMonitor monitor )
            : base( info )
        {
            var cfgFile = GetThisFile().RemoveLastPart().AppendPart( "BuggyPackageDriver.xml" );
            if( File.Exists( cfgFile ) )
            {
                ReturnError = true;
                XElement c = XDocument.Load( cfgFile ).Root;
                ErrorStep = c.AttributeEnum( "ErrorStep", SetupCallGroupStep.None );
                ErrorBeforeHandlers = (bool?)c.Attribute( "ErrorBeforeHandlers" ) ?? false;
                ErrorFromOnStep = (bool?)c.Attribute( "ErrorFromOnStep" ) ?? false;
                monitor.Info( $"BuggyPackageDriver: {c.ToString()}." );
            }
            else monitor.Info( $"BuggyPackageDriver: No Error." );
        }

        protected override bool ExecutePreInit( IActivityMonitor monitor )
        {
            // SqlPackageBaseItemDriver.ExecutePreInit handles scripts loading
            // and creates SetupHandlers for them (for the Package itself, its Model and its Objects
            // if they exist).
            if( !base.ExecutePreInit( monitor ) ) return false;

            monitor.Info( $"BuggyPackageDriver:ExecutePreInit" );
            return !(ReturnError && ErrorStep == SetupCallGroupStep.None);
        }

        protected override bool Init( IActivityMonitor monitor, bool beforeHandlers )
        {
            monitor.Info( $"BuggyPackageDriver:Init ({beforeHandlers})" );
            return !(ReturnError && !ErrorFromOnStep && ErrorStep == SetupCallGroupStep.Init && ErrorBeforeHandlers == beforeHandlers);
        }

        protected override bool InitContent( IActivityMonitor monitor, bool beforeHandlers )
        {
            monitor.Info( $"BuggyPackageDriver:InitContent ({beforeHandlers})" );
            return !(ReturnError && !ErrorFromOnStep && ErrorStep == SetupCallGroupStep.InitContent && ErrorBeforeHandlers == beforeHandlers);
        }

        protected override bool Install( IActivityMonitor monitor, bool beforeHandlers )
        {
            monitor.Info( $"BuggyPackageDriver:Install ({beforeHandlers})" );
            return !(ReturnError && !ErrorFromOnStep && ErrorStep == SetupCallGroupStep.Install && ErrorBeforeHandlers == beforeHandlers);
        }

        protected override bool InstallContent( IActivityMonitor monitor, bool beforeHandlers )
        {
            monitor.Info( $"BuggyPackageDriver:InstallContent ({beforeHandlers})" );
            return !(ReturnError && !ErrorFromOnStep && ErrorStep == SetupCallGroupStep.InstallContent && ErrorBeforeHandlers == beforeHandlers);
        }

        protected override bool Settle( IActivityMonitor monitor, bool beforeHandlers )
        {
            monitor.Info( $"BuggyPackageDriver:Settle ({beforeHandlers})" );
            return !(ReturnError && !ErrorFromOnStep && ErrorStep == SetupCallGroupStep.Settle && ErrorBeforeHandlers == beforeHandlers);
        }

        protected override bool SettleContent( IActivityMonitor monitor, bool beforeHandlers )
        {
            monitor.Info( $"BuggyPackageDriver:SettleContent ({beforeHandlers})" );
            return !(ReturnError && !ErrorFromOnStep && ErrorStep == SetupCallGroupStep.SettleContent && ErrorBeforeHandlers == beforeHandlers);
        }

        protected override bool OnStep( IActivityMonitor monitor, SetupCallGroupStep step, bool beforeHandlers )
        {
            monitor.Info( $"BuggyPackageDriver:OnStep - {step} ({beforeHandlers})" );
            return !(ReturnError && ErrorFromOnStep && ErrorStep == step && ErrorBeforeHandlers == beforeHandlers);
        }

    }
}
