using CSemVer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace CKSetup
{
    public class CKVersionInfo
    {
        static Regex _r = new Regex( @"^(?<1>.*?) \((?<2>.*?)\) - SHA1: (?<3>.*?) - CommitDate: (?<4>.*?)$" );

        /// <summary>
        /// Initializes a new <see cref="CKVersionInfo"/>.
        /// </summary>
        /// <param name="informationalVersion">Informational version. Can be null.</param>
        public CKVersionInfo( string informationalVersion )
        {
            if( (InformationalVersion = informationalVersion) != null )
            {
                Match m = _r.Match( informationalVersion );
                if( m.Success )
                {
                    SemVersion = m.Groups[1].Value;
                    NuGetVersion = m.Groups[2].Value;
                    SHA1 = m.Groups[3].Value;
                    CommitDate = DateTime.Parse( m.Groups[4].Value );
                    CSVersion v = CSVersion.TryParse( SemVersion );
                    if( v.IsValid ) Version = v;
                }
            }
        }

        /// <summary>
        /// Gets the InformationalVersion if any.
        /// Null otherwise.
        /// </summary>
        public string InformationalVersion { get; }

        /// <summary>
        /// Gets the semantic version string. 
        /// Null if the InformationalVersion attribute was not standard.
        /// </summary>
        public string SemVersion { get; }

        /// <summary>
        /// Gets the <see cref="SemVersion"/> if successfully parsed (ie. <see cref="CSVersion.IsValid"/> is true).
        /// Null otherwise.
        /// </summary>
        public CSVersion Version { get; }

        /// <summary>
        /// Gets the NuGet version extracted from the <see cref="InformationalVersion"/>.
        /// Null if the InformationalVersion attribute was not standard.
        /// </summary>
        public string NuGetVersion { get; }

        /// <summary>
        /// Gets the SHA1 extracted from the <see cref="InformationalVersion"/>.
        /// Null if the InformationalVersion attribute was not standard.
        /// </summary>
        public string SHA1 { get; }

        /// <summary>
        /// Gets the commit date  extracted from the <see cref="InformationalVersion"/>.
        /// Null if the InformationalVersion attribute was not standard.
        /// </summary>
        public DateTime CommitDate { get; }

        public override string ToString() => InformationalVersion ?? "<No valid version>"; 

    }
}
