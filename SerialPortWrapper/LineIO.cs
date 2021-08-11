using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BetterSerial
{
    /// <summary>
    /// Performs asynchronous line-by-line communication over a <see cref="Stream"/>
    /// </summary>
    public sealed class LineIO : IDisposable, IAsyncDisposable
    {
        private readonly Stream Stream;
        private readonly StreamWriter Writer;
        private readonly StreamReader Reader;
        private readonly char[] ReadBuffer;

        private readonly char? LineStart;
        private readonly char LineEnd;

        private readonly StringBuilder LineBuilder;

        private bool LineActive;
        private bool disposedValue;

        private bool AutoStartLine => !LineStart.HasValue;

        public bool IsOpen { get; private set; }

        /// <summary>
        /// Wrap a <see cref="Stream"/> for asynchronous line-by-line communication
        /// </summary>
        /// <param name="stream">The stream to wrap</param>
        /// <param name="lineEnd">The character that marks the end of a line</param>
        /// <param name="lineStart">The character that marks the start of the line (or null, if there isn't one)</param>
        /// <param name="bufferSize">The maximum number of characters to read from the stream at once.
        /// This can be shorter than the expected line length.</param>
        public LineIO(Stream stream, char lineEnd, char? lineStart = null, int bufferSize = 10)
        {
            Stream = stream;
            Writer = new StreamWriter(Stream);
            Reader = new StreamReader(Stream);
            ReadBuffer = new char[bufferSize];

            LineEnd = lineEnd;
            LineStart = lineStart;

            LineBuilder = new();

            LineActive = AutoStartLine;

            IsOpen = true;

            Task.Run(Scan);
        }

        /// <summary>
        /// Fires when a full line has been received on the <see cref="Stream"/>
        /// </summary>
        public event Action<string>? LineReceived;

        #region Stream Scanning
        private async Task<IEnumerable<char>> GetChars()
        {
            var count = await Reader.ReadAsync(ReadBuffer);
            return ReadBuffer.Take(count);
        }

        private async void Scan()
        {
            while (true)
            {
                try
                {
                    foreach (var c in await GetChars())
                    {
                        if (!LineActive && c == LineStart) // Start of line detected
                        {
                            LineActive = true;
                        }
                        else if (LineActive && c == LineEnd) // End of line detected
                        {
                            // Publish the completed line
                            LineReceived?.Invoke(LineBuilder.ToString());

                            // Reset the buffer to a new line
                            LineBuilder.Clear();
                            LineActive = AutoStartLine;
                        }
                        else if (LineActive)
                        {
                            LineBuilder.Append(c);
                        }
                        
                    }

                }
                catch (OperationCanceledException)
                {
                    // The Channel closed while waiting for a message
                    // Stop checking for new messages
                    break;
                }
                catch (InvalidOperationException)
                {
                    // The Channel closed in between checking IsActive and
                    // trying to read the next line
                    // Stop checking for new messages
                    break;
                }
            }

            await DisposeAsync();
        }
        #endregion

        /// <summary>
        /// Publish a line to the <see cref="Stream"/>
        /// </summary>
        /// <param name="line">The text to publish to the <see cref="Stream"/></param>
        /// <returns>Whether the line was successfully published</returns>
        public async Task<bool> WriteLine(string line)
        {
            try
            {
                var withMarkers =
                    LineStart.HasValue
                    ? $"{LineStart}{line}{LineEnd}"
                    : $"{line}{LineEnd}";

                await Writer.WriteAsync(withMarkers);
                await Writer.FlushAsync();

                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        #region Dispose
        protected async virtual void Dispose(bool disposing) => await DisposeAsync(disposing: disposing);

        ///<inheritdoc />
        public ValueTask DisposeAsync() => DisposeAsync(disposing: true);

        private async ValueTask DisposeAsync(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    await Stream.DisposeAsync();
                }

                disposedValue = true;
                IsOpen = false;
            }
        }

        ~LineIO()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: false);
        }

        ///<inheritdoc/>
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        #endregion
    }
}
