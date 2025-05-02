# JoyBridge

## Solution Setup
1. Contracts
2. EventBridge
3. Gateway
4. PollingAgent
5. Shared
6. Simulator
7. ThinClient
8. docker-compose

## Project Descriptions

### Contracts
This project encapsulated the our protobuf file as well as shared classes or records to be uised between the different projects
For demo purposes, we will assume that the data we can poll from the running process can be mapped to the PatientEvent.

### EventBridge
EventBridge binds to RabbitMQ queue with the provided exchange and routing key. When a message is recieved, it saves it
to the database

### Gateway
- The gateway acts as the web socket server and listens on the same web socket connection used by the polling agent.
- The gateway will then publish a message on the relevant RabbitMQ queue to which EventBridge can react, process the data and save it to the database
- The gateway will forward RPCs from the ThinClient to the polling agent which can then either pause or resume processing of the TCP stream, simulating the pulling of information
from a running process

### Polling Agent
- The polling agent is what is responsible for extracting information from running processes
- The idea is that the agent should listen on a TCP socket and react to information received, then forward it via web socket.
- In hind sight, the polling agent should live within the thin client I think, then we would have one executeable less to run

### Shared
- The idea behind shared is that it's a place where we can consolidate shared services, including as a proof of concept, centralized logging

### Simulator
- The simulator acts as a "mock" running process which will accept the TCP connection initiated by the polling agent.
- The program will send 1000 dummy messages simulating the information received

### ThinClient
- This is a WPF application intended to show a live stream of the data being sent via the polling agent via websocket
- It provides a button to either send a pause or resume RPC command
- It thus also opens a ws connection on the relevant port and sends the RPC calls across a different port to be intercepted by the 
gateway, the gateway will then forward it to the polling agent which will then pause or resume processing of the underlying TPC stream

### Docker Compose
- Declares 2 services, RabbitMQ and PostgreSQL

### Missing WEB UI Front End
- The task included a optional Web UI to allow the end user to edit data received from the clinical site
- I have decided to NOT implement it as I won't have made it on time
- A basic workflow could be:
1. Create a Angular SPA
2. Inject HttpClient and open a WS connection
3. Intercept data and send back via the same WS connection
4. The gateway will send corresponding message on the relevant queue
5. EventBridge can then perform CRUD operations as needed on the data
6. The gateway will forward the edited data via the same WS
7. The polling agent is then required to WRITE to the relevant TCP stream which can conclude the process

### Port Allocation considerations
- Port 5130 is used for real time data transmissions between via web socket between the gateway and the thin client
- Port 5131 is used separately for RPCs
- Port 6001 is used for internal gRPC communication between the gateway and the polling agent

### Further Considerations
- When pausing a stream, currently the simulator still sends data which is then buffered, this can't be used on production as it would
cause spikes. 
- Implementing state machines would have made my life easier to I can gracefully handle communication events
- Reiterating, the polling agent should live in the thin client, which makes more sense to me in hindsight as I'm currently allocating extra resources

### Running the solution:
- Clone the solution
- Ensure you have the docker engine installed as a minimum
- Click on the batch script to run it. 
- A basic window will appear showing a live stream of data received via the web socket connection
- I made a mistake by not updating the connection status label, so hit pause and resume to see it update
- Fingers crossed everything runs as expected!!