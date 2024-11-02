module GossipProtocol

#r "nuget: Akka"
#r "nuget: Akka.FSharp"
#r "nuget: Akka.TestKit"
#r "nuget: Akka.Remote"

open System
open System.Threading
open Akka.Actor
open Akka.Configuration
open Akka.FSharp
open Akka.TestKit

let randomNumber = System.Random(1)
let system = ActorSystem.Create("Gossip")
let receiveLimit = 10
let sendLimit = 1000


let timer = System.Diagnostics.Stopwatch()

type DispatcherMsg =
    | InitializeTopology of string
    | BuildTopologyCompleted
    | AllNodesExhausted
    | OutputPsumResults

type TopologyMsg =
    | CreateTopology of string * list<IActorRef>
    | TopologyCreationCompleted

type NodeMsg =
    | AddToTopology of string * IActorRef * list<IActorRef>
    | Rumor
    | Spread
    | Exhausted

type PNodeMsg =
    | AddToPNodeTopology of string * IActorRef * list<IActorRef>
    | Send
    | Receive of float * float
    | Forward of float * float
    | ExhaustedP
    | PrintRatio


let Start requestedTopology nodeCount requestedAlgo =
    let mutable givenNodeCount = nodeCount

    let Topology (mailbox: Actor<_>) =
        let mutable recorddone = 0
        let mutable dispatcherref = null

        let rec loop () =
            actor {
                let! message = mailbox.Receive()

                match message with
                | CreateTopology(requestedTopology, nodelist) ->
                    let mutable topologyNodeList = []
                    dispatcherref <- mailbox.Sender()

                    if (requestedTopology = "line") then
                        for i in 0 .. givenNodeCount - 1 do
                            topologyNodeList <- []

                            if i <> 0 then
                                topologyNodeList <- topologyNodeList @ [ nodelist.Item(i - 1) ]

                            if i <> givenNodeCount - 1 then
                                topologyNodeList <- topologyNodeList @ [ nodelist.Item(i + 1) ]

                            if requestedAlgo = "gossip" then
                                nodelist.Item(i)
                                <! AddToTopology(requestedTopology, dispatcherref, topologyNodeList)
                            else
                                nodelist.Item(i)
                                <! AddToPNodeTopology(requestedTopology, dispatcherref, topologyNodeList)
                    elif requestedTopology = "full" then
                        for i in 0 .. givenNodeCount - 1 do
                            //nodelist.Item(i)<!AddToTopology(requestedTopology,dispatcherref,[])
                            if requestedAlgo = "gossip" then
                                nodelist.Item(i) <! AddToTopology(requestedTopology, dispatcherref, [])
                            else
                                nodelist.Item(i) <! AddToPNodeTopology(requestedTopology, dispatcherref, [])
                    elif requestedTopology = "2D" then
                        let n = sqrt (givenNodeCount |> float) |> int
                        givenNodeCount <- n * n

                        for i in 0 .. givenNodeCount - 1 do
                            topologyNodeList <- []

                            if i % n <> 0 then
                                topologyNodeList <- topologyNodeList @ [ nodelist.Item(i - 1) ]

                            if i % n <> n - 1 then
                                topologyNodeList <- topologyNodeList @ [ nodelist.Item(i + 1) ]

                            if i / n <> 0 then
                                topologyNodeList <- topologyNodeList @ [ nodelist.Item(i - n) ]

                            if i / n <> n - 1 then
                                topologyNodeList <- topologyNodeList @ [ nodelist.Item(i + n) ]

                            if requestedAlgo = "gossip" then
                                nodelist.Item(i)
                                <! AddToTopology(requestedTopology, dispatcherref, topologyNodeList)
                            else
                                nodelist.Item(i)
                                <! AddToPNodeTopology(requestedTopology, dispatcherref, topologyNodeList)
                    elif requestedTopology = "imp3D" then
                        let edgelength = int (round (float givenNodeCount ** (1.0 / 3.0)))
                        let total3Dworkers = edgelength * edgelength * edgelength
                        givenNodeCount <- total3Dworkers
                        
                        for i in 0 .. total3Dworkers - 1 do
                            topologyNodeList <- []

                            if (i-1 >= 0) then
                                topologyNodeList <- topologyNodeList @ [nodelist.[i - 1]]

                            if (i+1 < total3Dworkers) then
                                topologyNodeList <- topologyNodeList @ [nodelist.[i + 1]]

                            if (i-edgelength >= 0) then
                                topologyNodeList <- topologyNodeList @ [nodelist.[i - edgelength]]

                            if (i+edgelength < total3Dworkers) then
                                topologyNodeList <- topologyNodeList @ [nodelist.[i + edgelength]]

                            if (i-(edgelength*edgelength) >= 0) then
                                topologyNodeList <- topologyNodeList @ [nodelist.[i - edgelength * edgelength]]

                            if (i+(edgelength*edgelength) < total3Dworkers) then
                                topologyNodeList <- topologyNodeList @ [nodelist.[i + edgelength * edgelength]]

                            
                            let rnd = randomNumber.Next(0,total3Dworkers-1)
                            topologyNodeList <- topologyNodeList @ [nodelist.[rnd]]

                            if requestedAlgo = "gossip" then
                                nodelist.[i]
                                <! AddToTopology(requestedTopology, dispatcherref, topologyNodeList)
                            else
                                nodelist.[i]
                                <! AddToPNodeTopology(requestedTopology, dispatcherref, topologyNodeList)            
                | TopologyCreationCompleted ->
                    if recorddone = givenNodeCount - 1 then
                        dispatcherref <! BuildTopologyCompleted

                    recorddone <- recorddone + 1

                return! loop ()
            }

        loop ()

    let topologyref = spawn system "topology" Topology
    let mutable Nodelist = []


    let Node (mailbox: Actor<_>) =
        let mutable neigbhours = []
        let mutable rumorheard = 0
        let mutable hadrumor = false
        let mutable spreadcnt = 0
        let mutable nodetopology = ""
        let mutable exhausted = false
        let mutable nexhausted = 0
        let mutable dispatcherref = null
        let id = mailbox.Self.Path.Name |> int

        let rec loop () =
            actor {
                let! message = mailbox.Receive()
                match message with
                | AddToTopology(requestedTopology, dref, nodelist) ->
                    neigbhours <- nodelist
                    nodetopology <- requestedTopology
                    dispatcherref <- dref
                    mailbox.Sender() <! TopologyCreationCompleted
                | Rumor ->
                    if not exhausted then
                        if rumorheard = 0 then
                            hadrumor <- true

                        rumorheard <- rumorheard + 1

                        if rumorheard = receiveLimit then
                            exhausted <- true
                            dispatcherref <! AllNodesExhausted

                            if requestedTopology = "full" then
                                for i in 0 .. givenNodeCount - 1 do
                                    if i <> id then
                                        Nodelist.Item(i) <! Exhausted
                            else
                                for i in 0 .. neigbhours.Length - 1 do
                                    neigbhours.Item(i) <! Exhausted
                        else
                            mailbox.Self <! Spread

                | Spread -> //printfn "Spread %i exhausted %b" id exhausted
                    if not exhausted then
                        let mutable next = randomNumber.Next()

                        if requestedTopology = "full" then
                            while next % givenNodeCount = id do
                                next <- randomNumber.Next()

                            Nodelist.Item(next % givenNodeCount) <! Rumor
                        else
                            neigbhours.Item(next % neigbhours.Length) <! Rumor

                        spreadcnt <- spreadcnt + 1

                        if spreadcnt = sendLimit then
                            exhausted <- true
                            dispatcherref <! AllNodesExhausted

                            if requestedTopology = "full" then
                                for i in 0 .. givenNodeCount - 1 do
                                    if i <> id then
                                        Nodelist.Item(i) <! Exhausted
                            else
                                for i in 0 .. neigbhours.Length - 1 do
                                    Nodelist.Item(i) <! Exhausted
                        else
                            mailbox.Self <! Spread

                | Exhausted ->
                    if not hadrumor then
                        mailbox.Self <! Rumor

                    if not exhausted then
                        nexhausted <- nexhausted + 1

                        if requestedTopology = "full" then
                            if nexhausted = givenNodeCount - 1 then
                                exhausted <- true
                                dispatcherref <! AllNodesExhausted
                        else if nexhausted = neigbhours.Length then
                            exhausted <- true
                            dispatcherref <! AllNodesExhausted

                return! loop ()
            }

        loop ()


    let PsumNode (mailbox: Actor<_>) =
        let mutable neigbhours = []
        let mutable nodetopology = ""
        let mutable exhausted = false
        let mutable nexhausted = 0
        let mutable ecnt = 0
        let mutable dispatcherref = null
        let id = mailbox.Self.Path.Name |> int
        let mutable s = id |> float
        let mutable w = 1.0

        let rec loop () =
            actor {
                let! message = mailbox.Receive()
                // printfn "%A %i" message id
                match message with
                | AddToPNodeTopology(topology, dref, nodelist) ->
                    neigbhours <- nodelist
                    nodetopology <- topology
                    dispatcherref <- dref
                    mailbox.Sender() <! TopologyCreationCompleted
                | Receive(rs, rw) ->
                    if not exhausted then
                        let ns = s + rs
                        let nw = w + rw

                        if abs ((ns / nw) - (s / w)) < 0.0000000001 then
                            ecnt <- ecnt + 1
                        else
                            ecnt <- 0

                        if ecnt = 3 then
                            exhausted <- true
                            dispatcherref <! AllNodesExhausted

                            if requestedTopology = "full" then
                                for i in 0 .. givenNodeCount - 1 do
                                    if i <> id then
                                        Nodelist.Item(i) <! ExhaustedP
                            else
                                for i in 0 .. neigbhours.Length - 1 do
                                    neigbhours.Item(i) <! ExhaustedP

                            mailbox.Self <! Forward(rs, rw)
                        else
                            s <- ns
                            w <- nw
                            mailbox.Self <! Send
                    else
                        mailbox.Self <! Forward(rs, rw)

                | Forward(rs, rw) ->
                    let mutable next = randomNumber.Next()

                    if requestedTopology = "full" then
                        while next % givenNodeCount = id do
                            next <- randomNumber.Next()

                        Nodelist.Item(next % givenNodeCount) <! Receive(rs, rw)
                    else
                        neigbhours.Item(next % neigbhours.Length) <! Receive(rs, rw)

                | Send ->
                    if not exhausted then
                        let mutable next = randomNumber.Next()
                        s <- s / 2.0
                        w <- w / 2.0

                        if requestedTopology = "full" then
                            while next % givenNodeCount = id do
                                next <- randomNumber.Next()

                            Nodelist.Item(next % givenNodeCount) <! Receive(s, w)
                        else
                            neigbhours.Item(next % neigbhours.Length) <! Receive(s, w)

                | ExhaustedP ->
                    if not exhausted then
                        nexhausted <- nexhausted + 1

                        if requestedTopology = "full" then
                            if nexhausted = givenNodeCount - 1 then
                                exhausted <- true
                                dispatcherref <! AllNodesExhausted
                        else if nexhausted = neigbhours.Length then
                            exhausted <- true
                            dispatcherref <! AllNodesExhausted
                | PrintRatio -> printfn "Node: %i ratio: %f" id (s / w)

                return! loop ()
            }

        loop ()


    if requestedAlgo = "gossip" then
        Nodelist <-
            [ for a in 0 .. givenNodeCount - 1 do
                  yield (spawn system (string a) Node) ]
    else
        Nodelist <-
            [ for a in 0 .. givenNodeCount - 1 do
                  yield (spawn system (string a) PsumNode) ]

    let Dispatcher (mailbox: Actor<_>) =
        let mutable spread = 0

        let rec loop () =
            actor {
                let! message = mailbox.Receive()
                //printfn "%A" message
                match message with
                | InitializeTopology(topology) -> topologyref <! CreateTopology(topology, Nodelist)
                | BuildTopologyCompleted ->
                    if requestedAlgo = "gossip" then
                        Nodelist.Item(randomNumber.Next() % givenNodeCount) <! Rumor
                        timer.Start()
                    else
                        let ind = randomNumber.Next() % givenNodeCount |> float
                        Nodelist.Item(randomNumber.Next() % givenNodeCount) <! Receive(ind, 1.0)
                        timer.Start()

                | AllNodesExhausted ->
                    spread <- spread + 1

                    if spread = givenNodeCount then
                        mailbox.Context.System.Terminate() |> ignore
                        // printfn "%s,%s,%i,%i" requestedAlgo requestedTopology givenNodeCount timer.ElapsedMilliseconds
                        printfn "Time elapsed - %i ms" timer.ElapsedMilliseconds
                | OutputPsumResults ->
                    for i in 0 .. givenNodeCount - 1 do
                        Nodelist.Item(i) <! PrintRatio



                return! loop ()
            }

        loop ()

    let Dispatcherref = spawn system "Dispatcher" Dispatcher

    Dispatcherref <! InitializeTopology(requestedTopology)

    system.WhenTerminated.Wait()

    ()