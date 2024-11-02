module Program

open GossipProtocol

open System
open System.Threading
open Akka.Actor
open Akka.Configuration
open Akka.FSharp
open Akka.TestKit

[<EntryPoint>]
let main argv =
    let nodeCount = argv.[0] |> int
    let topology = argv.[1]
    let algo = argv.[2]
    Start topology nodeCount algo
    0 // Return an integer exit code