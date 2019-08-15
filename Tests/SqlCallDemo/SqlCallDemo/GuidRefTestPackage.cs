using System;
using System.Data.SqlClient;
using CK.Core;

namespace SqlCallDemo
{
    /// <summary>
    /// See <see cref="GuidRefTestPackage.ReturnedWrapperWithContext"/> below: a returned wrapper can use the object that defines the procedure.
    /// </summary>
    public interface IAmTheClassThatDefinesTheProcedure
    {
        string ThisIsUsableByReturnedWrapper();
    }

    [SqlPackage( Schema = "CK", ResourcePath = "Res" ), Versions( "2.11.25" )]
    public abstract partial class GuidRefTestPackage : SqlPackage, IAmTheClassThatDefinesTheProcedure
    {
        /// <summary>
        /// Creating a SqlCommand object. All parameters are added to the command and input parameters are set to the values of the parameters. 
        /// </summary>
        [SqlProcedureNoExecute( "sGuidRefTest" )]
        public abstract SqlCommand CmdGuidRefTest( bool replaceInAndOut, Guid inOnly, ref Guid inAndOut, out string textResult );

        /// <summary>
        /// Nullable support: all value types can be nullable. 
        /// </summary>
        [SqlProcedureNoExecute( "sGuidRefTest" )]
        public abstract SqlCommand CmdGuidRefTest( Nullable<bool> replaceInAndOut, Nullable<Guid> inOnly, ref Nullable<Guid> inAndOut, out string textResult );

        /// <summary>
        /// Output only parameters are optional. (They however actually appears in the SqlCommand's parameters in order to be able to call the procedure.) 
        /// </summary>
        [SqlProcedureNoExecute( "sGuidRefTest" )]
        public abstract SqlCommand CmdGuidRefTestWithoutTextResult( bool replaceInAndOut, Guid inOnly, ref Guid inAndOut );

        /// <summary>
        /// Reusing a SqlCommand object: this MUST be the first parameter of the method and inital call must be made with a null reference.
        /// When null, the command is initalized with the parameters, when not null, the command is reconfigured with the values of the parameters. 
        /// </summary>
        [SqlProcedureNoExecute( "sGuidRefTest" )]
        public abstract void CmdGuidRefTest( ref SqlCommand cmd, bool replaceInAndOut, Guid inOnly, ref Guid inAndOut, out string textResult );


        /// <summary>
        /// Any object that is used in a method signature with the <see cref="ParameterSourceAttribute"/> can be used to 
        /// inject parameters.
        /// </summary>
        public class GuidRefTestContext
        {
            public bool ReplaceInAndOut { get; set; }
            public Guid InOnly { get; set; }
        }

        /// <summary>
        /// Using a [ParameterSource] attribute on parameter to provide values to input parameters.
        /// </summary>
        [SqlProcedureNoExecute( "sGuidRefTest" )]
        public abstract SqlCommand CmdGuidRefTest( [ParameterSource]GuidRefTestContext context, ref Guid inAndOut, out string textResult );

        /// <summary>
        /// Any object that is used in a method signature with the <see cref="ParameterSourceAttribute"/> can be used to 
        /// inject parameters.
        /// </summary>
        public class GuidRefTestInOutContext
        {
            public bool ReplaceInAndOut { get; set; }
            public Guid InOnly { get; set; }
            public Guid InAndOut { get; set; }
        }

        /// <summary>
        /// Using a [ParameterSource] attribute on parameter to provide values to input parameters.
        /// </summary>
        [SqlProcedureNoExecute( "sGuidRefTest" )]
        public abstract SqlCommand CmdGuidRefTest( [ParameterSource]GuidRefTestInOutContext context, out string textResult );

        /// <summary>
        /// Since output parameters are optional, this works.
        /// </summary>
        [SqlProcedureNoExecute( "sGuidRefTest" )]
        public abstract SqlCommand CmdGuidRefTest( [ParameterSource]GuidRefTestInOutContext context );


        /// <summary>
        /// When all parameters' values are specified, any object with a constructor on a SqlCommand
        /// can be used as the returned object.
        /// </summary>
        public class ReturnedWrapper
        {
            public readonly SqlCommand Command;

            public ReturnedWrapper( SqlCommand c )
            {
                Command = c;
            }
        }

        /// <summary>
        /// Any returned type with a constructor accepting a SqlCommand can be created.
        /// </summary>
        [SqlProcedureNoExecute( "sGuidRefTest" )]
        public abstract ReturnedWrapper CmdGuidRefTestReturnsWrapper( bool replaceInAndOut, Guid inOnly, ref Guid inAndOut );


        /// <summary>
        /// A wrapper must handle all parameters that are not procedure parameters (otherwise an error is raised during db setup).
        /// </summary>
        public class ReturnedWrapperWithParameters
        {
            public readonly SqlCommand Command;
            public readonly string Parameter1;
            public readonly string Parameter2;
            public readonly int Parameter3;

            public ReturnedWrapperWithParameters( SqlCommand c, string stringParameter, string anotherParameter, int yetAnotherOne )
            {
                Command = c;
                Parameter1 = stringParameter;
                Parameter2 = anotherParameter;
                Parameter3 = yetAnotherOne;
            }
        }

        /// <summary>
        /// Returned wrapper's constructor must be able to handle all non-parameters.
        /// </summary>
        [SqlProcedureNoExecute( "sGuidRefTest" )]
        public abstract ReturnedWrapperWithParameters CmdGuidRefTestReturnsWrapperWithParameters( bool replaceInAndOut, string stringParameter, Guid inOnly, string anotherParameter, int yetAnotherOne, ref Guid inAndOut );


        string IAmTheClassThatDefinesTheProcedure.ThisIsUsableByReturnedWrapper()
        {
            return "Data from prodedure definer.";
        }

        /// <summary>
        /// When using a returned wrapper, any parameters that can not be bound to method parameters but are assignable to 
        /// the type that defines the procedure (ie. the "package") can be used: the wrapper may then use the package information.
        /// </summary>
        public class ReturnedWrapperWithContext
        {
            public readonly SqlCommand Command;
            public readonly GuidRefTestInOutContext Context;
            public readonly string FromPackage;

            public ReturnedWrapperWithContext( SqlCommand c, GuidRefTestInOutContext context, IAmTheClassThatDefinesTheProcedure package )
            {
                Command = c;
                Context = context;
                FromPackage = package.ThisIsUsableByReturnedWrapper();
            }
        }

        /// <summary>
        /// Returned wrapper's constructor will have this package as a IAmTheClassThatDefinesTheProcedure interface.
        /// </summary>
        [SqlProcedureNoExecute( "sGuidRefTest" )]
        public abstract ReturnedWrapperWithContext CmdGuidRefTestReturnsWrapperWithContext( [ParameterSource]GuidRefTestInOutContext context );

    }
}
