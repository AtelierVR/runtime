const net = require('net');

const HOST = '0.0.0.0';
const PORT = 8080;

const clients = [];
let buffer = ""; // Buffer global conservé par le serveur

const server = net.createServer((socket) => {
    clients.push(socket);
    const clientName = `${socket.remoteAddress}:${socket.remotePort}`;
    let left = false;
    console.log(`[TCP Server] Socket JOIN ${clientName} (total: ${clients.length})`);

    // On envoie immédiatement le buffer actuel au nouveau client
    if (socket.writable) {
        socket.write(buffer);
    }

    socket.on('data', (data) => {
        const message = data.toString('utf8').trim();
        socket.write(`Echo: ${message}\n`);
    });

    socket.on('close', () => {
        if (left) return;
        left = true;
        const index = clients.indexOf(socket);
        if (index > -1)
            clients.splice(index, 1);
        console.log(`[TCP Server] Socket LEAVE ${clientName} (total: ${clients.length})`);
    });

    socket.on('error', (err) => {
        console.error(`[TCP Server] Socket error ${clientName}: ${err.message}`);
    });
});

server.on('error', (err) => {
    console.error(`[TCP Server error] ${err.message}`);
});

server.listen(PORT, HOST, () => {
    console.log(`[TCP Server] Listening on ${HOST}:${PORT}`);

    process.stdin.setRawMode(true);
    process.stdin.resume();
    process.stdin.setEncoding('utf8');

    console.log('[TCP Server] Type to broadcast immediately (Ctrl+C to exit)');

    process.stdin.on('data', (key) => {
        if (key === '\u0003') // Ctrl+C
            process.exit(0);

        if (key === '\u0008' || key === '\x7f') {
            // Traitement de la touche Effacer / Backspace
            buffer = buffer.slice(0, -1);
        } else {
            // Ajout du caractère (lettre, \n, \r, etc.)
            buffer += key;
        }

        // Limite optionnelle si besoin, ex: conserver les 64 derniers caractères
        buffer = buffer.slice(-64);

        // Envoi du buffer complet à tous les clients
        broadcastMessage(buffer);
    });
});

function broadcastMessage(message) {
    if (clients.length === 0) return;

    clients.forEach((client) => {
        if (client.writable) {
            try {
                client.write(message.length === 0 ? " " : message);
            } catch {
                const clientIndex = clients.indexOf(client);
                if (clientIndex > -1)
                    clients.splice(clientIndex, 1);
            }
        } else {
            const clientIndex = clients.indexOf(client);
            if (clientIndex > -1)
                clients.splice(clientIndex, 1);
        }
    });
}

process.on('SIGINT', () => {
    process.stdin.setRawMode(false);
    process.stdin.pause();

    clients.forEach((client) => {
        client.end();
    });

    server.close(() => {
        process.exit(0);
    });
});