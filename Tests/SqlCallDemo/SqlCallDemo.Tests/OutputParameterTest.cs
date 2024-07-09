using CK.Core;
using CK.SqlServer;
using CK.Testing;
using NUnit.Framework;
using System.Threading.Tasks;
using static CK.Testing.SqlServerTestHelper;

namespace SqlCallDemo.Tests
{
    [TestFixture]
    public class OutputParameterTest
    {
        [Test]
        public void calling_an_input_output_with_default_parameter_value_uses_the_Sql_default_value()
        {
            var p = SharedEngine.Map.StObjs.Obtain<OutputParameterPackage>();
            using( var ctx = new SqlStandardCallContext() )
            {
                string result = p.OutputInputParameterWithDefault( ctx );
                Assert.That( result, Is.EqualTo( "Return: The Sql Default." ) );
            }
        }

        [Test]
        public void calling_an_input_output_with_default_parameter_value_can_also_specify_the_input_value()
        {
            var p = SharedEngine.Map.StObjs.Obtain<OutputParameterPackage>();
            using( var ctx = new SqlStandardCallContext() )
            {
                string result = p.OutputInputParameterWithDefault( ctx, "It F... Works!" );
                Assert.That( result, Is.EqualTo( "Return: It F... Works!" ) );
            }
        }

        [Test]
        public void calling_a_pure_output_with_default_parameter_value_emits_a_warning_but_the_Sql_default_value_applies()
        {
            var p = SharedEngine.Map.StObjs.Obtain<OutputParameterPackage>();
            using( var ctx = new SqlStandardCallContext() )
            {
                string result = p.OutputParameterWithDefault( ctx );
                Assert.That( result, Is.EqualTo( "Return: The Sql Default." ) );
            }
        }

        [Test]
        public void calling_a_pure_output_with_default_parameter_value_emits_a_warning_but_one_can_also_specify_the_input_value()
        {
            var p = SharedEngine.Map.StObjs.Obtain<OutputParameterPackage>();
            using( var ctx = new SqlStandardCallContext() )
            {
                string result = p.OutputParameterWithDefault( ctx, "What a mess..." );
                Assert.That( result, Is.EqualTo( "Return: What a mess..." ) );
            }
        }

        [Test]
        public void calling_a_pure_output_with_default_parameter_value_emits_a_warning_but_one_can_also_specify_the_input_value_that_is_null()
        {
            var p = SharedEngine.Map.StObjs.Obtain<OutputParameterPackage>();
            using( var ctx = new SqlStandardCallContext() )
            {
                string result = p.OutputParameterWithDefault( ctx, null );
                Assert.That( result, Is.EqualTo( "Return: NULL input for @TextResult!" ) );
            }
        }


        [Test]
        public async Task async_calling_a_pure_output_with_default_parameter_Async()
        {
            var p = SharedEngine.Map.StObjs.Obtain<OutputParameterPackage>();
            using( var ctx = new SqlStandardCallContext() )
            {
                string result = await p.OutputParameterWithDefaultAsync( ctx, null ).ConfigureAwait( false );
                Assert.That( result, Is.EqualTo( "Return: NULL input for @TextResult!" ) );
            }
        }


    }
}
