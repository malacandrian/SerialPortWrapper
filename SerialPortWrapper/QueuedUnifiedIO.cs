using System;
using Optional;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using System.Timers;

namespace BetterSerial
{
    ///<summary>Sends messages one at a time, and returns the next
    /// received value as a response.</summary>
    public class QueuedUnifiedIO<T> : IUnifiedIO<T>
    {
        private Option<ReplyAwaitable<T>> AwaitingReply;
        private readonly ConcurrentQueue<ReplyAwaitable<T>> ToSend = new();
        private readonly Action<T> Tx;

        ///<summary>Unify an IO system that uses an event for receiving</summary>
        ///<param name="tx">Send a message over the disparate IO</param>
        ///<param name="rxSubscriber">Subscribes a handler to the disparate IO's message received event</param>
        public QueuedUnifiedIO(Action<T> tx, Action<Action<T>> rxSubscriber) {
            Tx = tx;
            rxSubscriber(LineReceived);
        }

        private void LineReceived(T response)
        {
            AwaitingReply.MatchSome(x=>x.PostReply(response));
            ManageQueue();
        }

        ///<inheritdoc />
        public Task<T> Exchange(T toSend)
        {
            var awaitable = new ReplyAwaitable<T>(toSend);

            ToSend.Enqueue(awaitable);
            ManageQueue();
            return  awaitable.GetTask();
        }

        private void ManageQueue() {
            // Dequeue expired awaiters
            while(ToSend.TryPeek(out var head) && head.IsCompleted) {
                ToSend.TryDequeue(out var _);
            }
            
            // If there's a currently active awaitable, do nothing
            if(AwaitingReply.Exists(x=>!x.IsCompleted)) { return; }

            // Send the next item in the queue, if possiblecd
            if(ToSend.TryDequeue(out var next)) {
                AwaitingReply = next.Some();
                next.OnTimeout += ManageQueue;
                Tx(next.ToSend);
            } else {
                AwaitingReply = Option.None<ReplyAwaitable<T>>();
            }
        }
    }

    class ReplyAwaitable<T> {
        public T ToSend { get; }
        private readonly TaskCompletionSource<T> Tcs = new();
        private readonly System.Timers.Timer Timeout;
        public event Action? OnTimeout;

        public ReplyAwaitable(T toSend, uint timeoutMs = 500) {
            Timeout = new() {
                AutoReset = false,
                Interval = timeoutMs,
                Enabled = true
            };
            Timeout.Elapsed += TryCancel;

            ToSend = toSend;
        }

        private void TryCancel(object? sender, ElapsedEventArgs e)
        {
            try 
            { 
                Tcs.SetCanceled(); 
                OnTimeout?.Invoke();
            }
            catch(Exception _) { }
        }

        public void PostReply(T reply) {
            try {Tcs.SetResult(reply);}
            catch(Exception _) { }
        }

        public Task<T> GetTask() => Tcs.Task;

        public bool IsCompleted => Tcs.Task.IsCompleted;
    }
}