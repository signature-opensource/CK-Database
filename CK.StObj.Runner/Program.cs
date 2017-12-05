using System;
using System.Xml.Linq;
using System.IO;
using System.Reflection;
using CK.Setup;
using CK.Core;
using System.Diagnostics;
using System.Threading;
using System.Globalization;
using System.Text;


namespace CK.StObj.Runner
{
    public static partial class Program
    {
        static public int Main( string[] args )
        {
            // See https://github.com/dotnet/corefx/issues/23608
            CultureInfo.CurrentCulture
                = CultureInfo.CurrentUICulture
                = CultureInfo.DefaultThreadCurrentCulture
                = CultureInfo.DefaultThreadCurrentUICulture = new CultureInfo( "en-US" );

            string rawLogPath = Path.Combine( AppContext.BaseDirectory, "CK.StObj.Runner.RawLogs.txt" );
            if( File.Exists( rawLogPath ) ) File.Delete( rawLogPath );
            var rawLogText = new StringBuilder();
            try
            {
                ResolveEventHandler loadHook = ( sender, arg ) =>
                {
                    var failed = new AssemblyName( arg.Name );
                    var resolved = failed.Version != null && string.IsNullOrWhiteSpace( failed.CultureName )
                            ? Assembly.Load( new AssemblyName( failed.Name ) )
                            : null;
                    rawLogText.AppendLine( $"Load conflict: {arg.Name} => {(resolved != null ? resolved.FullName : "(null)")}" );
                    return resolved;
                };
                AppDomain.CurrentDomain.AssemblyResolve += loadHook;
                return StObjActualRunner.Run( rawLogText, args );
            }
            catch( Exception ex )
            {
                rawLogText.Append( "Exception: " ).AppendLine( ex.Message );
                while( ex.InnerException != null )
                {
                    ex = ex.InnerException;
                    rawLogText.Append( " => Inner: " ).AppendLine( ex.Message );
                }
                return 1;
            }
            finally
            {
                File.WriteAllText( rawLogPath, rawLogText.ToString() );
            }
        }

    }
}
