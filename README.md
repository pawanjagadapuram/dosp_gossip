# COP5615: DOSP, Fall 2023 Programming Assignment 3

## Gossip & Push-sum algorithm simulation using Akka-framework and F#

| Team Member                | UFID     |
| -------------------------- | -------- |
| Nitish Chandra Mahesh      | 36139637 |
| Anuj Papriwal              | 37008807 |
| Pawan Kumar Jagadapuram    | 73643747 |
| Gopi Amarnath Reddy Bekkem | 72188579 |

## Overview

In this project we implemented Gossip and Push-Sum algorithms for information propagation and sum computation, respectively, within a distributed system simulated using F# and the Akka framework. Here, the Asynchronous Gossip algorithm is employed, where actors asynchronously share rumors with random neighbors until each actor has heard the rumor 10 times. In the Push-Sum algorithm, actors maintain state variables s and w, and messages exchanged contain pairs (s, w). Actors iteratively update their values based on received messages, selecting random neighbors for communication. The termination criterion is when an actor's s/w ratio remains unchanged more than 10^-10 for three consecutive rounds. This experiment involves exploring various network topologies, such as a full, 2D, line, and imperfect 3D grid, to observe their impact on the dissemination speed of Gossip protocols.

## How to Run

Run the Project using the below command:

> dotnet run numNodes topology algorithm

Where numNodes is the number of actors, topology is one of full, 2D, line, imp3D, the algorithm is one of gossip, push-sum.

Usage:

> dotnet run 10 line gossip

## What is working

- Convergence of Gossip algorithm for all topologies - Full, Line, 2D Grid, Imperfect 3D Grid
- Convergence of Push Sum algorithm for all topologies - Full, Line, 2D Grid, Improper 3D Grid

## Largest Network:

Largest Number of Nodes for Gossip Algorithm:

- Full: 10000
- Line: 5000
- 2D: 10000
- Imperfect 3D Grid: 10000

Largest Number of Nodes for Push-sum Algorithm:

- Full: 80
- Line: 80
- 2D: 80
- Imperfect 3D Grid: 80

# Results Graph

Convergence time vs Number of nodes

![image info](<output/Gossip_Algorithm.png>)
![image info](<output/GossipAlgorithm(LogarithmicScaledgraph).png>)
![image info](<output/Push-SumAlgorithm.png>)
![image info](<output/Push-SumAlgorithm(LogarithmicScaledgraph).png>)
