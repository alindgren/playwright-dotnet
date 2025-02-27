using System.Threading.Tasks;
using Microsoft.Playwright.Helpers;
using Microsoft.Playwright.Transport.Channels;

namespace Microsoft.Playwright
{
    internal partial class Mouse : IMouse
    {
        private readonly PageChannel _channel;

        public Mouse(PageChannel channel)
        {
            _channel = channel;
        }

        public Task ClickAsync(float x, float y, MouseButton? button, int? clickCount, float? delay)
            => _channel.MouseClickAsync(x, y, delay, button, clickCount);

        public Task DblClickAsync(float x, float y, MouseButton? button, float? delay)
            => _channel.MouseClickAsync(x, y, delay, button, 2);

        public Task DownAsync(MouseButton? button, int? clickCount)
            => _channel.MouseDownAsync(button, clickCount);

        public Task MoveAsync(float x, float y, int? steps)
            => _channel.MouseMoveAsync(x, y, steps ?? 1);

        public Task UpAsync(MouseButton? button, int? clickCount)
            => _channel.MouseUpAsync(button, clickCount);
    }
}
