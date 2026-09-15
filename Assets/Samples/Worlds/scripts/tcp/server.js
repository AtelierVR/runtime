const net = require('net');

const HOST = '0.0.0.0';
const PORT = 8080; // Le mapping vers 25658 se fait dans docker-compose.yml

const clients = [];
let buffer = ""; // Dernière frame envoyée, servie aux nouveaux clients

const server = net.createServer((socket) => {
    clients.push(socket);
    const clientName = `${socket.remoteAddress}:${socket.remotePort}`;
    let left = false;
    console.log(`[TCP Server] Socket JOIN ${clientName} (total: ${clients.length})`);

    // On envoie immédiatement la frame actuelle au nouveau client
    if (socket.writable) {
        socket.write(buffer);
    }

    // Premier client connecté : on (re)démarre l'animation
    if (clients.length === 1) {
        startAnimation();
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

        // Plus aucun client : on coupe l'animation pour économiser du CPU
        if (clients.length === 0) {
            stopAnimation();
        }
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
});

// --- Animation ASCII : balle 2D avec traînée, rebondissant dans une boîte ---
const WIDTH = 40;
const HEIGHT = 14;
const TRAIL_CHARS = ['●', 'o', '.']; // tête -> traînée -> disparition
let x = 2, y = 2;
let dx = 1, dy = 1;
let trail = []; // positions récentes {x, y}
let animationTimer = null;

const CLEAR_SCREEN = '\x1b[2J\x1b[H'; // efface l'écran + replace le curseur en haut

function nextFrame() {
    // Mise à jour de la traînée
    trail.unshift({ x, y });
    if (trail.length > TRAIL_CHARS.length) trail.pop();

    // Construction de la grille avec bordure
    const grid = Array.from({ length: HEIGHT }, () => new Array(WIDTH).fill(' '));
    for (let i = 0; i < WIDTH; i++) {
        grid[0][i] = '─';
        grid[HEIGHT - 1][i] = '─';
    }
    for (let j = 0; j < HEIGHT; j++) {
        grid[j][0] = '│';
        grid[j][WIDTH - 1] = '│';
    }
    grid[0][0] = '┌'; grid[0][WIDTH - 1] = '┐';
    grid[HEIGHT - 1][0] = '└'; grid[HEIGHT - 1][WIDTH - 1] = '┘';

    // Traînée dessinée de la plus vieille à la plus récente (la tête écrase le reste)
    for (let i = trail.length - 1; i >= 0; i--) {
        const p = trail[i];
        grid[p.y][p.x] = TRAIL_CHARS[i];
    }

    // Déplacement + rebond sur les bords
    x += dx;
    y += dy;
    if (x <= 1 || x >= WIDTH - 2) dx *= -1;
    if (y <= 1 || y >= HEIGHT - 2) dy *= -1;

    return CLEAR_SCREEN + grid.map((row) => row.join('')).join('\r\n') + '\r\n';
}

function startAnimation() {
    if (animationTimer) return; // déjà en cours
    animationTimer = setInterval(() => {
        buffer = nextFrame();
        broadcastMessage(buffer);
    }, 90);
}

function stopAnimation() {
    if (!animationTimer) return; // déjà arrêtée
    clearInterval(animationTimer);
    animationTimer = null;
}

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
    clients.forEach((client) => {
        client.end();
    });

    server.close(() => {
        process.exit(0);
    });
});

process.on('SIGTERM', () => {
    clients.forEach((client) => {
        client.end();
    });

    server.close(() => {
        process.exit(0);
    });
});