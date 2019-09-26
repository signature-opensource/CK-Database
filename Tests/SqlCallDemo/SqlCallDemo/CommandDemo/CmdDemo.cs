using System;

namespace SqlCallDemo.CommandDemo
{
    /// <summary>
    /// Demo command POCO (do not look for anything that makes sense).
    /// Two different results are nested here: a mutable POCO and
    /// an immutable object, CmdDemo.ResultReadOnly.
    /// </summary>
    public class CmdDemo
    {
        public int ActorId { get; set; }

        public string CompanyName { get; set; }

        public DateTime LaunchDate { get; set; }

        /// <summary>
        /// Result POCO as a mutable object.
        /// </summary>
        public class ResultPOCO
        {
            public int Delay { get; set; }

            public string ActualCompanyName { get; set; }
        }

        /// <summary>
        /// Immutable result POCO (often better).
        /// </summary>
        public class ResultReadOnly
        {
            public ResultReadOnly( int delay, string actualCompanyName )
            {
                Delay = delay;
                ActualCompanyName = actualCompanyName;
            }

            public int Delay { get; }

            public string ActualCompanyName { get; }
        }
    }
}
