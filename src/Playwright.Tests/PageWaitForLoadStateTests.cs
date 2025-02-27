using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Playwright.NUnit;
using NUnit.Framework;

namespace Microsoft.Playwright.Tests
{
    ///<playwright-file>page-wait-for-load-state.ts</playwright-file>
    [Parallelizable(ParallelScope.Self)]
    public class PageWaitForLoadStateTests : PageTestEx
    {
        [PlaywrightTest("page-wait-for-load-state.ts", "should pick up ongoing navigation")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldPickUpOngoingNavigation()
        {
            var responseTask = new TaskCompletionSource<bool>();
            var waitForRequestTask = Server.WaitForRequest("/one-style.css");

            Server.SetRoute("/one-style.css", async (ctx) =>
            {
                if (await responseTask.Task)
                {
                    ctx.Response.StatusCode = 404;
                    await ctx.Response.WriteAsync("File not found");
                }
            });

            var navigationTask = Page.GotoAsync(Server.Prefix + "/one-style.html");
            await waitForRequestTask;
            var waitLoadTask = Page.WaitForLoadStateAsync();
            responseTask.TrySetResult(true);
            await waitLoadTask;
            await navigationTask;
        }

        [PlaywrightTest("page-wait-for-load-state.ts", "should respect timeout")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldRespectTimeout()
        {
            Server.SetRoute("/one-style.css", _ => Task.Delay(Timeout.Infinite));
            await Page.GotoAsync(Server.Prefix + "/one-style.html", new() { WaitUntil = WaitUntilState.DOMContentLoaded });
            var exception = await PlaywrightAssert.ThrowsAsync<TimeoutException>(() => Page.WaitForLoadStateAsync(LoadState.Load, new() { Timeout = 1 }));
            StringAssert.Contains("Timeout 1ms exceeded", exception.Message);
        }

        [PlaywrightTest("page-wait-for-load-state.ts", "should resolve immediately if loaded")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldResolveImmediatelyIfLoaded()
        {
            await Page.GotoAsync(Server.Prefix + "/one-style.html");
            await Page.WaitForLoadStateAsync();
        }

        [PlaywrightTest("page-wait-for-load-state.ts", "should throw for bad state")]
        [Test, Ignore("We don't need this test")]
        public void ShouldTthrowForBadState()
        {
        }

        [PlaywrightTest("page-wait-for-load-state.ts", "should resolve immediately if load state matches")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldResolveImmediatelyIfLoadStateMatches()
        {
            var responseTask = new TaskCompletionSource<bool>();
            var waitForRequestTask = Server.WaitForRequest("/one-style.css");

            Server.SetRoute("/one-style.css", async (ctx) =>
            {
                if (await responseTask.Task)
                {
                    ctx.Response.StatusCode = 404;
                    await ctx.Response.WriteAsync("File not found");
                }
            });

            var navigationTask = Page.GotoAsync(Server.Prefix + "/one-style.html");
            await waitForRequestTask;
            await Page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);
            responseTask.TrySetResult(true);
            await navigationTask;
        }

        [PlaywrightTest("page-wait-for-load-state.ts", "should work with pages that have loaded before being connected to")]
        [Test, SkipBrowserAndPlatform(skipFirefox: true, skipWebkit: true)]
        public async Task ShouldWorkWithPagesThatHaveLoadedBeforeBeingConnectedTo()
        {
            await Page.GotoAsync(Server.EmptyPage);
            await Page.EvaluateAsync(@"async () => {
                const child = window.open(document.location.href);
                while (child.document.readyState !== 'complete' || child.document.location.href === 'about:blank')
                  await new Promise(f => setTimeout(f, 100));
            }");
            var pages = Context.Pages;
            Assert.AreEqual(2, pages.Count);

            // order is not guaranteed
            var mainPage = pages.FirstOrDefault(p => ReferenceEquals(Page, p));
            var connectedPage = pages.Single(p => !ReferenceEquals(Page, p));

            Assert.NotNull(mainPage);
            Assert.AreEqual(Server.EmptyPage, mainPage.Url);

            Assert.AreEqual(Server.EmptyPage, connectedPage.Url);
            await connectedPage.WaitForLoadStateAsync();
            Assert.AreEqual(Server.EmptyPage, connectedPage.Url);
        }

        [PlaywrightTest("page-wait-for-load-state.spec.ts", "should work for frame")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldWorkForFrame()
        {
            await Page.GotoAsync(Server.Prefix + "/frames/one-frame.html");
            var frame = Page.Frames.ElementAt(1);

            TaskCompletionSource<bool> requestTask = new TaskCompletionSource<bool>();
            TaskCompletionSource<bool> routeReachedTask = new TaskCompletionSource<bool>();
            await Page.RouteAsync(Server.Prefix + "/one-style.css", async (route) =>
            {
                routeReachedTask.TrySetResult(true);
                await requestTask.Task;
                await route.ContinueAsync();
            });

            await frame.GotoAsync(Server.Prefix + "/one-style.html", new() { WaitUntil = WaitUntilState.DOMContentLoaded });

            await routeReachedTask.Task;
            var loadTask = frame.WaitForLoadStateAsync();
            await Page.EvaluateAsync("1");
            Assert.False(loadTask.IsCompleted);
            requestTask.TrySetResult(true);
            await loadTask;
        }
    }
}
