using System.Threading.Tasks;
using Microsoft.Playwright.Helpers;
using Microsoft.Playwright.NUnit;
using NUnit.Framework;

namespace Microsoft.Playwright.Tests
{
    [Parallelizable(ParallelScope.Self)]
    public class JSHandlePropertiesTests : PageTestEx
    {
        [PlaywrightTest("jshandle-properties.spec.ts", "getProperties should work")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task GetPropertiesShouldWork()
        {
            var aHandle = await Page.EvaluateHandleAsync(@"() => ({
                foo: 'bar'
            })");
            var properties = await aHandle.GetPropertiesAsync();
            Assert.True(properties.TryGetValue("foo", out var foo));
            Assert.AreEqual("bar", await foo.JsonValueAsync<string>());
        }

        [PlaywrightTest("jshandle-properties.spec.ts", "should return empty map for non-objects")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldReturnEmptyMapForNonObjects()
        {
            var aHandle = await Page.EvaluateHandleAsync("() => 123");
            var properties = await aHandle.GetPropertiesAsync();
            Assert.IsEmpty(properties);
        }

        [PlaywrightTest("jshandle-properties.spec.ts", "should return even non-own properties")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldReturnEvenNonOwnProperties()
        {
            var aHandle = await Page.EvaluateHandleAsync(@"() => {
                class A
                {
                    constructor()
                    {
                        this.a = '1';
                    }
                }
                class B extends A
                {
                    constructor() {
                        super();
                        this.b = '2';
                    }
                }
                return new B();
            }");
            var properties = await aHandle.GetPropertiesAsync();
            Assert.AreEqual("1", await properties["a"].JsonValueAsync<string>());
            Assert.AreEqual("2", await properties["b"].JsonValueAsync<string>());
        }

        [PlaywrightTest("jshandle-properties.spec.ts", "should work")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldWork()
        {
            var aHandle = await Page.EvaluateHandleAsync(@"() => ({
                one: 1,
                two: 2,
                three: 3
            })");
            var twoHandle = await aHandle.GetPropertyAsync("two");
            Assert.AreEqual(2, await twoHandle.JsonValueAsync<int>());
        }

        [PlaywrightTest("jshandle-properties.spec.ts", "should work with undefined, null, and empty")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldWorkWithUndefinedNullAndEmpty()
        {
            var aHandle = await Page.EvaluateHandleAsync(@"() => ({
                undefined: undefined,
                null: null,
            })");
            var undefinedHandle = await aHandle.GetPropertyAsync("undefined");
            Assert.Null(await undefinedHandle.JsonValueAsync<string>());
            var nullHandle = await aHandle.GetPropertyAsync("null");
            Assert.Null(await nullHandle.JsonValueAsync<string>());
            var emptyHandle = await aHandle.GetPropertyAsync("emptyHandle");
            Assert.Null(await emptyHandle.JsonValueAsync<string>());
        }

        [PlaywrightTest("jshandle-properties.spec.ts", "should work with unserializable values")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldWorkWithUnserializableValues()
        {
            var aHandle = await Page.EvaluateHandleAsync(@"() => ({
                infinity: Infinity,
                nInfinity: -Infinity,
                nan: NaN,
                nzero: -0
            })");

            var infinityHandle = await aHandle.GetPropertyAsync("infinity");
            Assert.AreEqual(double.PositiveInfinity, await infinityHandle.JsonValueAsync<double>());
            var ninfinityHandle = await aHandle.GetPropertyAsync("nInfinity");
            Assert.AreEqual(double.NegativeInfinity, await ninfinityHandle.JsonValueAsync<double>());
            var nanHandle = await aHandle.GetPropertyAsync("nan");
            Assert.AreEqual(double.NaN, await nanHandle.JsonValueAsync<double>());
            var nzeroHandle = await aHandle.GetPropertyAsync("nzero");
            Assert.True((await nzeroHandle.JsonValueAsync<double>()).IsNegativeZero());
        }
    }
}
