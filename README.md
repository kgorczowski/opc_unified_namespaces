# OPCGateway
 Intermediary service between external clients and OPC UA servers.



## Features

- Manage connections with OPC UA servers
- Read/Write data to OPC UA servers
- Monitor OPC UA server nodes

## Prerequisites

- .NET 8 SDK
- Docker

## Setup Instructions

### Running with Docker

Both the main project (OPCGateway) and the OPCServerMock are dockerized. You can run them using Docker.  Note that the PostgreSQL database is not included in the project's Docker setup and needs to be started separately.

1. Clone the repository:

	`git clone https://github.com/vmpl/opc-gateway.git cd OPCGateway`

2. Start the PostgreSQL container:

	`docker run --name opc-postgres -e POSTGRES_USER=postgres -e POSTGRES_PASSWORD=admin123 -e POSTGRES_DB=opc_gateway -p 5432:5432 -d postgres`

2. Build and run the Docker container:
 
	`docker build -t opc-gateway . && docker run -d -p 5000:80 --name opc-gateway opc-gateway`


   This command will build and start the OPCGateway container. The database will be created automatically on project startup if it doesn't exist.

3. Access the Swagger UI:

   Open your browser and navigate to `http://localhost:5000/swagger` to access the Swagger UI.

### Running Locally

If you prefer to run the project locally without Docker, follow these steps:

1. Clone the repository:

	`git clone https://github.com/vmpl/opc-gateway.git cd OPCGateway`

2. Start the PostgreSQL container:

	`docker run --name opc-postgres -e POSTGRES_USER=postgres -e POSTGRES_PASSWORD=admin123 -e POSTGRES_DB=opc_gateway -p 5432:5432 -d postgres`

2. Run the application:

   `dotnet run`

3. Access the Swagger UI:

   Open your browser and navigate to `https://localhost:5001/swagger` to access the Swagger UI.


## API Documentation

The API endpoints are documented using Swagger. You can view the documentation by pasting the provided Swagger JSON or YAML file into the [Swagger Editor](https://editor-next.swagger.io/).

1. Open the [Swagger Editor](https://editor-next.swagger.io/).
2. Copy the contents of the `OPCGateway/swagger.json` file from this repository.
3. Paste the contents into the editor.


## Usage Summary

### Connect to an OPC UA Server

Users can connect to an OPC UA server by providing the endpoint URL, username, and password.

### Read and Write Data

Users can read and write data to and from the OPC UA server by specifying the node ID and the value to be read or written.

### Monitor Nodes

Users can monitor nodes on the OPC UA server. Monitoring requires the use of WebSockets to receive real-time updates.


## Testing with OPCServerMock

To test the OPCGateway project, you can use the included OPCGateway.OPCServerMock. Follow these steps to set up and test the project:

1. Ensure Docker is installed and running on your machine.

2. Clone the repository:

	`git clone https://github.com/yourusername/OPCGateway.git cd OPCGateway.OPCServerMock`

3. Build and run the Docker containers:

	`docker build -t opc-server-mock . && docker run -d -p 4841:4841 -p 4842:4842 --name opc-server-mock opc-server-mock`


   This command will build and start the OPCServerMock container.


	**Basic tests with OPCServerMock:**

	Body of a message in _/api/Connection/connect/anonymous_ endpoint for establishing connection with opc mock server:

	```
	{
	  "endpointUrl": "opc.tcp://localhost:4841"
	}
   ```
	Note that there are also similar endpoint to establish connection with Username/Password authentication and with Certificate authentication. In all three authentication modes, additionally, SecurityMode and SecurityPolicy can be chosen. If they are not present (or set to "Auto"), they are adapting to the OPC server security settings.
 
	All parameters in opc mock reside in opc namespace = 2. They are all of "float" type.
 
	Parameters that change randomly on opc mock server and can be read e.g. in _/api/Data/read/_ endpoint:
	- SomeDynamicNodeId
	- SecondDynamicNodeId
 
	Above parameters can be also written by client but they will continue generating random values at high frequency, so we'll not be able to read the written value.
 
	Parameters that can we can write to (they maintain their value, so then we also read them), e.g. in _/api/Data/write_ endpoint:
	- SomeWriteNodeId

## Testing Monitoring with OPCGateway.Tests.MonitoringFrontend

To test the monitoring functionality, you can use the `OPCGateway.Tests.MonitoringFrontend` directory. Follow these steps to set up and test the monitoring:

1. Ensure you have `http-server` installed. If not, install it using npm:

    `npm install -g http-server`

2. Navigate to the `OPCGateway.Tests.MonitoringFrontend` directory:

    `cd OPCGateway.Tests.MonitoringFrontend`

3. Start the `http-server` on port 8000:

    `http-server -p 8000`

4. Open your browser and navigate to `http://localhost:8000` to access the monitoring frontend.

5. Use the monitoring frontend to connect to the OPC UA server and monitor nodes. Ensure that the OPCGateway project is running and the WebSocket endpoint is accessible.

## Testing Monitoring with tools like Postman

Apart from testing monitoring with above-mentioned OPCGateway.Tests.MonitoringFrontend, you can also use tools like Postman to test the monitoring functionality. Follow these steps to set up and test the monitoring:

1. Connect to websocket _ws://localhost:5034/api/monitoring/monitor_.
2. Send a message to the WebSocket connection to start monitoring a node. The JSON message should include the connection ID, node ID, and publishing interval. For example:

```
{
	"Action": "StartMonitoring",
	"ConnectionId": "<your connection id>",
	"OpcNamespace": 2,
	"NodeIds": ["SomeDynamicNodeId"],
	"PublishingInterval": 500
}
```
3. Apart from "StartMonitoring" action, there are also "StopMonitoring" and "GetMonitoredItems" actions. The message structure is the same, just change the action name.

## Additional Information

- The OPCGateway project uses Serilog for logging.
- The API endpoints are secured using API key authentication. For now it's just a simple API key check for all endpoints in connection controller. The api is defined in the appsettings.json file.
  
  Api key (must be placed in messages header in "X-API-Key" field or pasted in Swagger's Authorize section):
e0c2747d-40e9-4b5d-980a-6b06a4c4d24d
- Integration tests start the OPCServerMock process in the background to simulate OPC client-server real communication. So during tests execution, a separate process of OPCServerMock (e.g. run in Docker) will interfere with tests and will cause tests failure. Therefore, it's advised to close any instance of OPCServerMock before running integration tests.
