import { connect } from 'tcp';
import console from 'console';
import { from } from 'buffer';
import { setTimeout } from 'scheduler';

export let exports = {
    result: null,
    host: "localhost",
    port: 8080
};

let socket = null;
let destroyed = false;

// Retire les codes d'échappement ANSI (clear screen, couleurs, etc.) illisibles dans l'UI
const ANSI_ESCAPE_REGEX = /\u001b\[[0-9;]*[A-Za-z]/g;

export async function onAwake() {
    destroyed = false;
    loop();
}

async function loop() {
    if (destroyed) return;

    console.log(`[TCP Client] Connecting to ${exports.host}:${exports.port}...`);

    // Indique que la tentative de connexion est en cours
    if (exports.result)
        exports.result.text = "connecting...";

    await tryConnect();

    if (!destroyed) {
        // Indique la déconnexion après l'échec ou la fermeture du socket
        if (exports.result)
            exports.result.text = "disconnected";

        console.log(`[TCP Client] Reconnection in 15sec...`);
        setTimeout(loop, 15000);
    }
}

async function tryConnect() {
    try {
        socket = await connect(exports.host, exports.port);
        if (destroyed || !socket?.connected) return;

        console.log(`[TCP Client] Connected to ${exports.host}:${exports.port}`);

        // Indique que la connexion est établie
        if (exports.result)
            exports.result.text = "connected";

        socket.on("data", (data) => {
            if (destroyed || !exports.result)
                return;

            const text = from(data).toString("utf8");

            exports.result.text = text
                .replace(ANSI_ESCAPE_REGEX, "")
                .replace(/\r\n/g, "\n");
        });

        socket.on("error", (message) => {
            if (destroyed) 
                return;
            console.error(`[TCP Client] ${message}`);
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
    } catch (error) {
        console.error("[TCP Client]", error);
    } finally {
        if (socket) {
            await socket.close();
            socket = null;
        }
    }
}

export async function onDestroy() {
    destroyed = true;

    if (exports.result)
        exports.result.text = "disconnected";

    if (socket) {
        await socket.close();
        socket = null;
    }
}