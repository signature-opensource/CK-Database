using CK.Core;
using CK.Setup;
using CK.SqlServer.Setup;
using System;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace SqlActorPackage.Engine;

public class BuggyPackageDriver : SqlPackageBaseItemDriver
{
    readonly bool ReturnError;
    readonly SetupCallGroupStep ErrorStep;
    readonly bool ErrorBeforeHandlers;
    readonly bool ErrorFromOnStep;

    public BuggyPackageDriver( BuildInfo info, IActivityMonitor monitor )
        : base( info )
    {
        using( monitor.OpenInfo( $"Reading '{AppContext.BaseDirectory}\\BuggyPackageDriver.xml'." ) )
        {
            var path = Path.Combine( AppContext.BaseDirectory, "BuggyPackageDriver.xml" );
            if( File.Exists( path ) )
            {
                monitor.Info( "File BuggyPackageDriver.xml found." );
                ReturnError = true;
                XElement c = XDocument.Load( path ).Root!;
                ErrorStep = c.AttributeEnum( "ErrorStep", SetupCallGroupStep.None );
                ErrorBeforeHandlers = (bool?)c.Attribute( "ErrorBeforeHandlers" ) ?? false;
                ErrorFromOnStep = (bool?)c.Attribute( "ErrorFromOnStep" ) ?? false;
                monitor.Info( $"BuggyPackageDriver: {c.ToString()}." );
            }
            else monitor.Info( $"File BuggyPackageDriver.xml not found: No Error." );
        }
    }

    protected override bool ExecutePreInit( IActivityMonitor monitor )
    {
        // SqlPackageBaseItemDriver.ExecutePreInit handles scripts loading
        // and creates SetupHandlers for them (for the Package itself, its Model and its Objects
        // if they exist).
        if( !base.ExecutePreInit( monitor ) ) return false;

        bool success = !(ReturnError && ErrorStep == SetupCallGroupStep.None);
        monitor.Info( $"BuggyPackageDriver:ExecutePreInit => {(success ? "ok" : "ERROR (returning false)")}" );
        return success;
    }

    protected override bool Init( IActivityMonitor monitor, bool beforeHandlers )
    {
        bool success = !(ReturnError && !ErrorFromOnStep && ErrorStep == SetupCallGroupStep.Init && ErrorBeforeHandlers == beforeHandlers);
        monitor.Info( $"BuggyPackageDriver:Init ({(beforeHandlers ? "before" : "after")} handler) => {(success ? "ok" : "ERROR (returning false)")}" );
        return success;
    }

    protected override bool InitContent( IActivityMonitor monitor, bool beforeHandlers )
    {
        bool success = !(ReturnError && !ErrorFromOnStep && ErrorStep == SetupCallGroupStep.InitContent && ErrorBeforeHandlers == beforeHandlers);
        monitor.Info( $"BuggyPackageDriver:InitContent ({(beforeHandlers ? "before" : "after")} handler) => {(success ? "ok" : "ERROR (returning false)")}" );
        return success;
    }

    protected override bool Install( IActivityMonitor monitor, bool beforeHandlers )
    {
        bool success = !(ReturnError && !ErrorFromOnStep && ErrorStep == SetupCallGroupStep.Install && ErrorBeforeHandlers == beforeHandlers);
        monitor.Info( $"BuggyPackageDriver:Install ({(beforeHandlers ? "before" : "after")} handler) => {(success ? "ok" : "ERROR (returning false)")}" );
        return success;
    }

    protected override bool InstallContent( IActivityMonitor monitor, bool beforeHandlers )
    {
        bool success = !(ReturnError && !ErrorFromOnStep && ErrorStep == SetupCallGroupStep.InstallContent && ErrorBeforeHandlers == beforeHandlers);
        monitor.Info( $"BuggyPackageDriver:InstallContent ({(beforeHandlers ? "before" : "after")} handler) => {(success ? "ok" : "ERROR (returning false)")}" );
        return success;
    }

    protected override bool Settle( IActivityMonitor monitor, bool beforeHandlers )
    {
        bool success = !(ReturnError && !ErrorFromOnStep && ErrorStep == SetupCallGroupStep.Settle && ErrorBeforeHandlers == beforeHandlers);
        monitor.Info( $"BuggyPackageDriver:Settle ({(beforeHandlers ? "before" : "after")} handler) => {(success ? "ok" : "ERROR (returning false)")}" );
        return success;
    }

    protected override bool SettleContent( IActivityMonitor monitor, bool beforeHandlers )
    {
        bool success = !(ReturnError && !ErrorFromOnStep && ErrorStep == SetupCallGroupStep.SettleContent && ErrorBeforeHandlers == beforeHandlers);
        monitor.Info( $"BuggyPackageDriver:SettleContent ({(beforeHandlers ? "before" : "after")} handler) => {(success ? "ok" : "ERROR (returning false)")}" );
        return success;
    }

    protected override bool OnStep( IActivityMonitor monitor, SetupCallGroupStep step, bool beforeHandlers )
    {
        bool success = !(ReturnError && ErrorFromOnStep && ErrorStep == step && ErrorBeforeHandlers == beforeHandlers);
        monitor.Info( $"BuggyPackageDriver:OnStep - {step} ({(beforeHandlers ? "before" : "after")} handler) => {(success ? "ok" : "ERROR (returning false)")}" );
        return success;
    }

}
