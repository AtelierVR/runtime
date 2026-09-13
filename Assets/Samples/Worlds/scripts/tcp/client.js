import { connect } from 'tcp';
import console from 'console';
import { from as bufferFrom } from 'buffer';

export let exports = {
    result: null,
    host: "localhost",
    port: 8080
};

let socket = null;
let destroyed = false;

export async function onAwake() {
   start();
}

async function start() {
     destroyed = false;

    while (!destroyed) {
        try {
            socket = await connect(exports.host, exports.port);

            if (destroyed)
                break;

            if (!socket?.connected) {
                console.log(`[TCP Client] Failed to connect to ${exports.host}:${exports.port}`);
                await new Promise(resolve => setTimeout(resolve, 15000));
                continue;
            }

            console.log(`[TCP Client] Connected to ${exports.host}:${exports.port}`);

            socket.on("data", (data) => {
                if (destroyed || !exports.result)
                    return;
                let text = data.length !== 0 
                    ? bufferFrom(data).toString("utf8")
                    : "";
                exports.result.text = text;
            });

            socket.on("error", (message) => {
                if (destroyed) 
                    return;
                console.error(`[TCP Client] Error: ${message}`);
            });

            await new Promise(resolve => {
                if (destroyed || !socket?.connected) {
                    resolve();
                    return;
                }

                socket.on("closed", () => {
                    if (!destroyed)
                        console.log("[TCP Client] Connection closed");

                    resolve();
                });
            });

            if (destroyed)
                break;

            console.log("[TCP Client] Disconnected, reconnecting in 15s...");
        } catch (error) {
            if (!destroyed)
                console.error("[TCP Client] Error:", error);
        } finally {
            if (socket) {
                await socket.close();
                socket = null;
            }
        }

        if (!destroyed)
            await new Promise(resolve => setTimeout(resolve, 15000));
    }
}

export async function onDestroy() {
    destroyed = true;

    if (socket) {
        await socket.close();
        socket = null;
    }
}