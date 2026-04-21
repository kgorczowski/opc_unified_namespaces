document.addEventListener("DOMContentLoaded", function () {
    const webSocket = new WebSocket("ws://localhost:5034/api/monitoring/monitor");

    webSocket.onopen = function (event) {
        console.log("WebSocket is open now.");
    };

    webSocket.onmessage = function (event) {
        const message = JSON.parse(event.data);
        console.log("Received data:", message);
        displayMessage(message);
    };

    webSocket.onclose = function (event) {
        console.log("WebSocket is closed now.");
    };

    webSocket.onerror = function (error) {
        console.log("WebSocket error:", error);
    };

    document.getElementById("startMonitoring").addEventListener("click", function () {
        const connectionId = document.getElementById("connectionId").value;
        const opcNamespace = parseInt(document.getElementById("opcNamespace").value);
        const nodeIds = document.getElementById("nodeIds").value.split(",");
        const publishingInterval = parseInt(document.getElementById("publishingInterval").value);

        const startMonitoringMessage = JSON.stringify({
            Action: "StartMonitoring",
            ConnectionId: connectionId,
            OpcNamespace: opcNamespace,
            NodeIds: nodeIds,
            PublishingInterval: publishingInterval
        });
        webSocket.send(startMonitoringMessage);
    });

    document.getElementById("stopMonitoring").addEventListener("click", function () {
        const connectionId = document.getElementById("connectionId").value;
        const opcNamespace = parseInt(document.getElementById("opcNamespace").value);
        const nodeIds = document.getElementById("nodeIds").value.split(",");

        const stopMonitoringMessage = JSON.stringify({
            Action: "StopMonitoring",
            ConnectionId: connectionId,
            OpcNamespace: opcNamespace,
            NodeIds: nodeIds
        });
        webSocket.send(stopMonitoringMessage);
    });

    function displayMessage(message) {
        const messagesDiv = document.getElementById("messages");
        const messageDiv = document.createElement("div");
        messageDiv.className = "message";
        messageDiv.textContent = JSON.stringify(message);
        messagesDiv.appendChild(messageDiv);
        messagesDiv.scrollTop = messagesDiv.scrollHeight;
    }
});
