using System.Threading.Tasks;

namespace BetterSerial
{
    ///<summary>An abstraction that unifies disparate asynchronous communications.</summary>
    ///<details>A disparate asynchronous communication method is one
    ///where transmission and response happen on seperate methods/events.
    ///<see ref="IUnifiedLineIO" /> sends and receives on the same method.</details>
    public interface IUnifiedIO<T> {
        ///<summary>Asynchronously send a line and receive a response.</summary>
        Task<T> Exchange(T toSend);
    }
}