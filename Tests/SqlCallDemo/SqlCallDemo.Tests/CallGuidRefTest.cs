using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CK.Core;
using NUnit.Framework;
using FluentAssertions;

namespace SqlCallDemo.Tests
{
    [TestFixture]
    public partial class CallGuidRefTest
    {
        static readonly Guid Guid1 = Guid.NewGuid();
        static readonly Guid Guid2 = Guid.NewGuid();

        [Test]
        public void when_the_method_returns_a_new_SqlCommand_parameters_are_configured()
        {
            var p = TestHelper.StObjMap.Default.Obtain<GuidRefTestPackage>();
            Guid inOut = Guid2;
            string result;
            SqlCommand cmd = p.CmdGuidRefTest( true, Guid1, ref inOut, out result );
            cmd.Parameters.Should().HaveCount( 4 );

            cmd.Parameters[0].ParameterName.Should().Be( "@ReplaceInAndOut" );
            cmd.Parameters[0].Direction.Should().Be( ParameterDirection.Input );
            cmd.Parameters[0].Value.Should().Be( true );

            cmd.Parameters[1].ParameterName.Should().Be( "@InOnly" );
            cmd.Parameters[1].Direction.Should().Be( ParameterDirection.Input );
            cmd.Parameters[1].Value.Should().Be( Guid1 );

            cmd.Parameters[2].ParameterName.Should().Be( "@InAndOut" );
            cmd.Parameters[2].Direction.Should().Be( ParameterDirection.InputOutput );
            cmd.Parameters[2].Value.Should().Be( Guid2 );

            cmd.Parameters[3].ParameterName.Should().Be( "@TextResult" );
            cmd.Parameters[3].Direction.Should().Be( ParameterDirection.Output );
            cmd.Parameters[3].Value.Should().BeNull();

            p.Database.ExecuteNonQuery( cmd );
            cmd.Parameters[2].Value.Should().NotBe( Guid2, "Since ReplaceInAndOut was true." );
            cmd.Parameters[3].Value.Should().Be( "@InOnly is not null, @InAndOut is not null." );
        }

        [Test]
        public void all_value_types_parameters_can_be_nullable()
        {
            var p = TestHelper.StObjMap.Default.Obtain<GuidRefTestPackage>();
            Nullable<bool> replaceInAndOut = true;
            Nullable<Guid> inOnly = null;
            Nullable<Guid> inOut = null;
            string result;
            SqlCommand cmd = p.CmdGuidRefTest( replaceInAndOut, inOnly, ref inOut, out result );
            Assert.That( cmd.Parameters.Count, Is.EqualTo( 4 ) );

            Assert.That( cmd.Parameters[0].ParameterName, Is.EqualTo( "@ReplaceInAndOut" ) );
            Assert.That( cmd.Parameters[0].Direction, Is.EqualTo( ParameterDirection.Input ) );
            Assert.That( cmd.Parameters[0].Value, Is.EqualTo( true ) );

            Assert.That( cmd.Parameters[1].ParameterName, Is.EqualTo( "@InOnly" ) );
            Assert.That( cmd.Parameters[1].Direction, Is.EqualTo( ParameterDirection.Input ) );
            Assert.That( cmd.Parameters[1].Value, Is.EqualTo( DBNull.Value ) );
            
            Assert.That( cmd.Parameters[2].ParameterName, Is.EqualTo( "@InAndOut" ) );
            Assert.That( cmd.Parameters[2].Direction, Is.EqualTo( ParameterDirection.InputOutput ) );
            Assert.That( cmd.Parameters[2].Value, Is.EqualTo( DBNull.Value ) );

            Assert.That( cmd.Parameters[3].ParameterName, Is.EqualTo( "@TextResult" ) );
            Assert.That( cmd.Parameters[3].Direction, Is.EqualTo( ParameterDirection.Output ) );
            Assert.That( cmd.Parameters[3].Value, Is.Null );

            p.Database.ExecuteNonQuery( cmd );
            Assert.That( cmd.Parameters[2].Value, Is.Not.Null.And.Not.EqualTo( Guid.Empty ), "Since ReplaceInAndOut was true." );
            Assert.That( cmd.Parameters[3].Value, Is.EqualTo( "@InOnly is null, @InAndOut is null." ) );

        }

        [Test]
        public void output_only_parameters_are_optionals_in_signature_but_such_parameters_have_to_be_in_the_SqlCommand_to_be_able_to_execute_it()
        {
            var p = TestHelper.StObjMap.Default.Obtain<GuidRefTestPackage>();
            Guid inOut = Guid2;
            SqlCommand cmd = p.CmdGuidRefTestWithoutTextResult( false, Guid1, ref inOut );
            Assert.That( cmd.Parameters.Count, Is.EqualTo( 4 ) );

            Assert.That( cmd.Parameters[0].ParameterName, Is.EqualTo( "@ReplaceInAndOut" ) );
            Assert.That( cmd.Parameters[0].Direction, Is.EqualTo( ParameterDirection.Input ) );
            Assert.That( cmd.Parameters[0].Value, Is.EqualTo( false ) );

            Assert.That( cmd.Parameters[1].ParameterName, Is.EqualTo( "@InOnly" ) );
            Assert.That( cmd.Parameters[1].Direction, Is.EqualTo( ParameterDirection.Input ) );
            Assert.That( cmd.Parameters[1].Value, Is.EqualTo( Guid1 ) );

            Assert.That( cmd.Parameters[2].ParameterName, Is.EqualTo( "@InAndOut" ) );
            Assert.That( cmd.Parameters[2].Direction, Is.EqualTo( ParameterDirection.InputOutput ) );
            Assert.That( cmd.Parameters[2].Value, Is.EqualTo( Guid2 ) );

            Assert.That( cmd.Parameters[3].ParameterName, Is.EqualTo( "@TextResult" ) );
            Assert.That( cmd.Parameters[3].Direction, Is.EqualTo( ParameterDirection.Output ) );
            Assert.That( cmd.Parameters[3].Value, Is.Null );

            p.Database.ExecuteNonQuery( cmd );
            Assert.That( cmd.Parameters[2].Value, Is.EqualTo( Guid2 ), "Since ReplaceInAndOut was false." );
        }

        [Test]
        public void SqlCommand_by_reference_must_be_the_first_parameter_and_passed_in_as_null_to_initialize_it_then_it_can_be_reused()
        {
            var p = TestHelper.StObjMap.Default.Obtain<GuidRefTestPackage>();
            Guid inOut = Guid2;
            string result;
            SqlCommand cmd = null;
            p.CmdGuidRefTest( ref cmd, false, Guid1, ref inOut, out result );
            p.Database.ExecuteNonQuery( cmd );
            Assert.That( cmd.Parameters[2].Value, Is.EqualTo( Guid2 ), "Since ReplaceInAndOut was false." );
            Assert.That( cmd.Parameters[3].Value, Is.EqualTo( "@InOnly is not null, @InAndOut is not null." ) );

            inOut = Guid1;
            p.CmdGuidRefTest( ref cmd, false, Guid1, ref inOut, out result );
            p.Database.ExecuteNonQuery( cmd );
            Assert.That( cmd.Parameters[2].Value, Is.EqualTo( Guid1 ), "Since ReplaceInAndOut was false." );
        }

        [Test]
        public void using_a_ISqlCallContext_parameter_to_provide_values_to_input_parameters()
        {
            var p = TestHelper.StObjMap.Default.Obtain<GuidRefTestPackage>();
            var ctx = new GuidRefTestPackage.GuidRefTestContext() { ReplaceInAndOut = false, InOnly = Guid1 };
            string result;
            Guid inOut = Guid2;
            var cmd = p.CmdGuidRefTest( ctx, ref inOut, out result );
            p.Database.ExecuteNonQuery( cmd );
            Assert.That( cmd.Parameters[2].Value, Is.EqualTo( Guid2 ), "Since ReplaceInAndOut was false." );
            Assert.That( cmd.Parameters[3].Value, Is.EqualTo( "@InOnly is not null, @InAndOut is not null." ) );
        }

        [Test]
        public void using_a_ISqlCallContext_parameter_to_provide_values_to_input_and_input_output_parameters()
        {
            var p = TestHelper.StObjMap.Default.Obtain<GuidRefTestPackage>();
            var ctx = new GuidRefTestPackage.GuidRefTestInOutContext() { ReplaceInAndOut = false, InOnly = Guid1, InAndOut = Guid2 };
            string result;
            var cmd = p.CmdGuidRefTest( ctx, out result );
            p.Database.ExecuteNonQuery( cmd );
            Assert.That( cmd.Parameters[2].Value, Is.EqualTo( Guid2 ), "Since ReplaceInAndOut was false." );
            Assert.That( cmd.Parameters[3].Value, Is.EqualTo( "@InOnly is not null, @InAndOut is not null." ) );
        }

        [Test]
        public void since_output_are_optionals_using_a_unique_ISqlCallContext_parameter_works_as_long_as_it_provides_all_the_required_input_values()
        {
            var p = TestHelper.StObjMap.Default.Obtain<GuidRefTestPackage>();
            var ctx = new GuidRefTestPackage.GuidRefTestInOutContext() { ReplaceInAndOut = false, InOnly = Guid1, InAndOut = Guid2 };
            var cmd = p.CmdGuidRefTest( ctx );
            p.Database.ExecuteNonQuery( cmd );
            Assert.That( cmd.Parameters[2].Value, Is.EqualTo( Guid2 ), "Since ReplaceInAndOut was false." );
            Assert.That( cmd.Parameters[3].Value, Is.EqualTo( "@InOnly is not null, @InAndOut is not null." ) );
        }

        [Test]
        public void returning_a_wrapper_object_only_requires_it_to_have_a_public_constructor_with_a_SqlCommand_as_long_as_all_parameters_values_are_provided()
        {
            var p = TestHelper.StObjMap.Default.Obtain<GuidRefTestPackage>();
            Guid inOut = Guid2;
            var wrapper = p.CmdGuidRefTestReturnsWrapper( false, Guid1, ref inOut );
            p.Database.ExecuteNonQuery( wrapper.Command );
            Assert.That( wrapper.Command.Parameters[2].Value, Is.EqualTo( Guid2 ), "Since ReplaceInAndOut was false." );
            Assert.That( wrapper.Command.Parameters[3].Value, Is.EqualTo( "@InOnly is not null, @InAndOut is not null." ) );
        }

        [Test]
        public void returning_a_wrapper_object_with_extra_parameters()
        {
            var p = TestHelper.StObjMap.Default.Obtain<GuidRefTestPackage>();
            Guid inOut = Guid2;
            var wrapper = p.CmdGuidRefTestReturnsWrapperWithParameters( false, "This is a parameter for the wrapper!", Guid1, "Another", 3712, ref inOut );
            p.Database.ExecuteNonQuery( wrapper.Command );
            Assert.That( wrapper.Command.Parameters[2].Value, Is.EqualTo( Guid2 ), "Since ReplaceInAndOut was false." );
            Assert.That( wrapper.Command.Parameters[3].Value, Is.EqualTo( "@InOnly is not null, @InAndOut is not null." ) );
            Assert.That( wrapper.Parameter1, Is.EqualTo( "This is a parameter for the wrapper!" ) );
            Assert.That( wrapper.Parameter2, Is.EqualTo( "Another" ) );
            Assert.That( wrapper.Parameter3, Is.EqualTo( 3712 ) );
        }

        [Test]
        public void returning_a_wrapper_object_can_also_capture_the_instance_object_that_defines_the_procedure()
        {
            var p = TestHelper.StObjMap.Default.Obtain<GuidRefTestPackage>();
            var ctx = new GuidRefTestPackage.GuidRefTestInOutContext() { ReplaceInAndOut = false, InOnly = Guid1, InAndOut = Guid2 };
            var wrapper = p.CmdGuidRefTestReturnsWrapperWithContext( ctx );
            p.Database.ExecuteNonQuery( wrapper.Command );
            Assert.That( wrapper.Command.Parameters[2].Value, Is.EqualTo( Guid2 ), "Since ReplaceInAndOut was false." );
            Assert.That( wrapper.Command.Parameters[3].Value, Is.EqualTo( "@InOnly is not null, @InAndOut is not null." ) );
            Assert.That( wrapper.Context, Is.SameAs( ctx ) );
            Assert.That( wrapper.FromPackage, Is.EqualTo( "Data from prodedure definer." ) );
        }

    }
}
