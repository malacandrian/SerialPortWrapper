module Tests

open System
open FsCheck
open FsCheck.Xunit
open BetterSerial

type Echoer<'a>() =
    let echo = new Event<'a>()

    [<CLIEvent>]
    member this.Echo = echo.Publish

    member this.Send x = echo.Trigger x

let inline unify<'a> (echoer : Echoer<'a>) =
    new QueuedUnifiedIO<'a>(
        Action<'a>(echoer.Send), 
        fun f -> echoer.Echo.Add(fun x -> f.Invoke(x))
        )

[<Property>]
let ``Responses should be returned by the Exchange function`` (message : string) =
    async {
        let echoer = new Echoer<string> () |> unify
        
        let! reply = echoer.Exchange message |> Async.AwaitTask

        return message = reply
    } |> Async.RunSynchronously
    

     
