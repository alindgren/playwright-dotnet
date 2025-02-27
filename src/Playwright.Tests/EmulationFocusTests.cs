using System.Threading.Tasks;
using Microsoft.Playwright.NUnit;
using NUnit.Framework;

namespace Microsoft.Playwright.Tests
{
    [Parallelizable(ParallelScope.Self)]
    public class EmulationFocusTests : PageTestEx
    {
        [PlaywrightTest("emulation-focus.spec.ts", "should think that it is focused by default")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldThinkThatItIsFocusedByDefault()
        {
            Assert.True(await Page.EvaluateAsync<bool>("document.hasFocus()"));
        }

        [PlaywrightTest("emulation-focus.spec.ts", "should think that all pages are focused")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldThinkThatAllPagesAreFocused()
        {
            var page2 = await Page.Context.NewPageAsync();
            Assert.True(await Page.EvaluateAsync<bool>("document.hasFocus()"));
            Assert.True(await page2.EvaluateAsync<bool>("document.hasFocus()"));
        }

        [PlaywrightTest("emulation-focus.spec.ts", "should focus popups by default")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldFocusPopupsByDefault()
        {
            await Page.GotoAsync(Server.EmptyPage);
            var popupTask = Page.WaitForPopupAsync();

            await TaskUtils.WhenAll(
                popupTask,
                Page.EvaluateAsync("url => window.open(url)", Server.EmptyPage));

            var popup = popupTask.Result;

            Assert.True(await Page.EvaluateAsync<bool>("document.hasFocus()"));
            Assert.True(await popup.EvaluateAsync<bool>("document.hasFocus()"));
        }

        [PlaywrightTest("emulation-focus.spec.ts", "should provide target for keyboard events")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldProvideTargetForKeyboardEvents()
        {
            var page2 = await Page.Context.NewPageAsync();

            await TaskUtils.WhenAll(
                Page.GotoAsync(Server.Prefix + "/input/textarea.html"),
                page2.GotoAsync(Server.Prefix + "/input/textarea.html"));

            await TaskUtils.WhenAll(
                Page.FocusAsync("input"),
                page2.FocusAsync("input"));

            string text = "first";
            string text2 = "second";

            await TaskUtils.WhenAll(
                Page.Keyboard.TypeAsync(text),
                page2.Keyboard.TypeAsync(text2));

            var results = await TaskUtils.WhenAll(
                Page.EvaluateAsync<string>("result"),
                page2.EvaluateAsync<string>("result"));

            Assert.AreEqual(text, results.Item1);
            Assert.AreEqual(text2, results.Item2);
        }

        [PlaywrightTest("emulation-focus.spec.ts", "should not affect mouse event target page")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldNotAffectMouseEventTargetPage()
        {
            var page2 = await Page.Context.NewPageAsync();
            string clickCounter = @"function clickCounter() {
              document.onclick = () => window.clickCount  = (window.clickCount || 0) + 1;
            }";

            await TaskUtils.WhenAll(
                Page.EvaluateAsync(clickCounter),
                page2.EvaluateAsync(clickCounter),
                Page.FocusAsync("body"),
                page2.FocusAsync("body"));

            await TaskUtils.WhenAll(
                Page.Mouse.ClickAsync(1, 1),
                page2.Mouse.ClickAsync(1, 1));

            var counters = await TaskUtils.WhenAll(
                Page.EvaluateAsync<int>("window.clickCount"),
                page2.EvaluateAsync<int>("window.clickCount"));

            Assert.AreEqual(1, counters.Item1);
            Assert.AreEqual(1, counters.Item2);
        }

        [PlaywrightTest("emulation-focus.spec.ts", "should change document.activeElement")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldChangeDocumentActiveElement()
        {
            var page2 = await Page.Context.NewPageAsync();

            await TaskUtils.WhenAll(
                Page.GotoAsync(Server.Prefix + "/input/textarea.html"),
                page2.GotoAsync(Server.Prefix + "/input/textarea.html"));

            await TaskUtils.WhenAll(
                Page.FocusAsync("input"),
                page2.FocusAsync("textArea"));

            var results = await TaskUtils.WhenAll(
                Page.EvaluateAsync<string>("document.activeElement.tagName"),
                page2.EvaluateAsync<string>("document.activeElement.tagName"));

            Assert.AreEqual("INPUT", results.Item1);
            Assert.AreEqual("TEXTAREA", results.Item2);
        }

        [PlaywrightTest("emulation-focus.spec.ts", "should not affect screenshots")]
        [Test, Ignore("We need screenshot features first")]
        public void ShouldNotAffectScreenshots()
        {
        }

        [PlaywrightTest("emulation-focus.spec.ts", "should change focused iframe")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldChangeFocusedIframe()
        {
            await Page.GotoAsync(Server.EmptyPage);

            var (frame1, frame2) = await TaskUtils.WhenAll(
                FrameUtils.AttachFrameAsync(Page, "frame1", Server.Prefix + "/input/textarea.html"),
                FrameUtils.AttachFrameAsync(Page, "frame2", Server.Prefix + "/input/textarea.html"));

            string logger = @"function logger() {
              self._events = [];
              const element = document.querySelector('input');
              element.onfocus = element.onblur = (e) => self._events.push(e.type);
            }";

            await TaskUtils.WhenAll(
                frame1.EvaluateAsync(logger),
                frame2.EvaluateAsync(logger));

            var focused = await TaskUtils.WhenAll(
                frame1.EvaluateAsync<bool>("document.hasFocus()"),
                frame2.EvaluateAsync<bool>("document.hasFocus()"));

            Assert.False(focused.Item1);
            Assert.False(focused.Item2);

            await frame1.FocusAsync("input");
            var events = await TaskUtils.WhenAll(
                frame1.EvaluateAsync<string[]>("self._events"),
                frame2.EvaluateAsync<string[]>("self._events"));

            Assert.AreEqual(new[] { "focus" }, events.Item1);
            Assert.IsEmpty(events.Item2);

            focused = await TaskUtils.WhenAll(
                frame1.EvaluateAsync<bool>("document.hasFocus()"),
                frame2.EvaluateAsync<bool>("document.hasFocus()"));

            Assert.True(focused.Item1);
            Assert.False(focused.Item2);

            await frame2.FocusAsync("input");
            events = await TaskUtils.WhenAll(
                frame1.EvaluateAsync<string[]>("self._events"),
                frame2.EvaluateAsync<string[]>("self._events"));

            Assert.AreEqual(new[] { "focus", "blur" }, events.Item1);
            Assert.AreEqual(new[] { "focus" }, events.Item2);

            focused = await TaskUtils.WhenAll(
                frame1.EvaluateAsync<bool>("document.hasFocus()"),
                frame2.EvaluateAsync<bool>("document.hasFocus()"));

            Assert.False(focused.Item1);
            Assert.True(focused.Item2);
        }
    }
}
