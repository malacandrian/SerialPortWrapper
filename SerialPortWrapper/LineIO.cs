﻿using System;
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
        private readonly Func<ValueTask> CloseStream;
        private readonly StreamWriter Writer;
        private readonly StreamReader Reader;
        private readonly char[] ReadBuffer;

        private readonly char? LineStart;
        private readonly char LineEnd;

        private readonly StringBuilder LineBuilder;

        private bool LineActive;
        private bool disposedValue;

        private bool AutoStartLine => !LineStart.HasValue;

        /// <summary>
        /// Whether the LineIO is currently active and able to Read/Write
        /// </summary>
        public bool IsOpen { get; private set; }

        /// <summary>
        /// Wrap a <see cref="Stream"/> for asynchronous line-by-line communication
        /// </summary>
        /// <param name="stream">The stream to wrap</param>
        /// <param name="lineEnd">The character that marks the end of a line</param>
        /// <param name="lineStart">The character that marks the start of the line (or null, if there isn't one)</param>
        /// <param name="bufferSize">The maximum number of characters to read from the stream at once.
        /// This can be shorter than the expected line length.</param>
        /// <param name="closeStream">Close the Stream. Defaults to <see cref="Stream.DisposeAsync()"/></param>
        public LineIO(Stream stream, char lineEnd, char? lineStart = null, int bufferSize = 10, Func<ValueTask>? closeStream = null)
        {
            Stream = stream;
            CloseStream = closeStream ?? Stream.DisposeAsync;
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

        private bool IsStartOfLine(char c) => !LineActive && c == LineStart;
        private bool IsEndOfLine(char c) => LineActive && c == LineEnd;

        private async void Scan()
        {
            while (IsOpen)
            {
                try
                {
                    foreach (var c in await GetChars())
                    {
                        if (IsStartOfLine(c))
                        {
                            LineActive = true;
                        }
                        else if (IsEndOfLine(c))
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
                catch (UnauthorizedAccessException)
                {
                    // The stream is no longer available
                    // e.g. COM Port unplugged
                    // Stop checking for new messages
                    break;
                }
                catch (OperationCanceledException)
                {
                    // The Stream closed while waiting for a message
                    // Stop checking for new messages
                    break;
                }
                catch (InvalidOperationException)
                {
                    // The Stream closed in between checking IsOpen and
                    // trying to read the next line
                    // Stop checking for new messages
                    break;
                }
                
            }

            // If scanning has failed, but the object hasn't been disposed,
            // Dispose the object
            if(IsOpen) { await DisposeAsync(); }
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

        private void Dispose(bool disposing) => Task.Run(async () => await DisposeAsync(disposing: disposing)).Wait();

        ///<inheritdoc />
        public ValueTask DisposeAsync() => DisposeAsync(disposing: true);

        private async ValueTask DisposeAsync(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    try
                    {
                        await CloseStream();
                    } catch (UnauthorizedAccessException)
                    {
                        // The device is unplugged, therefore the stream is gone
                        // Do nothing
                    } 
                    
                }

                disposedValue = true;
                IsOpen = false;
            }
        }

        /// <inheritdoc />
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
