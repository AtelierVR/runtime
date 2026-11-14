import { connect } from 'tcp';
import console from 'console';
import { from } from 'buffer';
import { setTimeout } from 'scheduler';

export let exports = {
    result: null,
    host: "localhost",
    port: 8080,
    /** Nombre maximum de lignes conservées dans le tampon du terminal. */
    maxRows: 200
};

let socket = null;
let destroyed = false;

// Texte en attente d'affichage (appliqué dans onUpdate)
let pendingText = null;
// Indique qu'un texte reçu doit être affiché au prochain Update
let needPrint = false;

// Demande l'affichage d'un texte : stocké puis appliqué au prochain Update
function print(text) {
    pendingText = text;
    needPrint = true;
}

// ── Terminal virtuel ─────────────────────────────────────────────────────
// Le flux distant contient des séquences ANSI (positionnement du curseur,
// effacement de l'écran ou de la ligne, couleurs…). Elles sont interprétées
// dans une grille de caractères afin de ne jamais apparaître dans le texte.

/** Lignes du terminal (tableaux de caractères pour permettre l'écrasement). */
let screen = [];
/** Position du curseur : ligne / colonne. */
let cursorRow = 0;
let cursorCol = 0;
/** Position mémorisée par ESC 7 / CSI s, restaurée par ESC 8 / CSI u. */
let savedRow = 0;
let savedCol = 0;
/** Début d'une séquence d'échappement coupée entre deux paquets TCP. */
let escapeBuffer = "";

/** Réinitialise complètement le terminal virtuel. */
function resetScreen() {
    screen = [];
    cursorRow = 0;
    cursorCol = 0;
    savedRow = 0;
    savedCol = 0;
    escapeBuffer = "";
}

/** Garantit l'existence de la ligne demandée. */
function ensureRow(row) {
    while (screen.length <= row)
        screen.push([]);
}

/** Écrit un caractère à la position du curseur, puis avance d'une colonne. */
function putChar(ch) {
    ensureRow(cursorRow);
    const line = screen[cursorRow];
    while (line.length < cursorCol)
        line.push(" ");
    line[cursorCol] = ch;
    cursorCol++;
}

/** Saut de ligne : retour en colonne 0 et ligne suivante (avec historique limité). */
function newLine() {
    cursorCol = 0;
    cursorRow++;
    ensureRow(cursorRow);

    const max = Math.max(1, exports.maxRows || 200);
    while (screen.length > max) {
        screen.shift();
        cursorRow = Math.max(0, cursorRow - 1);
        savedRow = Math.max(0, savedRow - 1);
    }
}

function saveCursor() {
    savedRow = cursorRow;
    savedCol = cursorCol;
}

function restoreCursor() {
    cursorRow = savedRow;
    cursorCol = savedCol;
    ensureRow(cursorRow);
}

/** Effacement de ligne (CSI n K) : 0 = curseur→fin, 1 = début→curseur, 2 = ligne entière. */
function eraseLine(mode) {
    ensureRow(cursorRow);
    const line = screen[cursorRow];

    if (mode === 1)
        for (let col = 0; col <= cursorCol && col < line.length; col++)
            line[col] = " ";
    else if (mode === 2)
        line.length = 0;
    else
        line.length = Math.min(line.length, cursorCol);
}

/** Effacement d'écran (CSI n J) : 0 = curseur→fin, 1 = début→curseur, 2/3 = tout. */
function eraseDisplay(mode) {
    if (mode === 1) {
        for (let row = 0; row < cursorRow; row++)
            screen[row] = [];
        eraseLine(1);
    } else if (mode === 2 || mode === 3) {
        screen = [];
    } else {
        eraseLine(0);
        screen.length = cursorRow + 1;
    }

    ensureRow(cursorRow);
}

/**
 * Applique une séquence CSI (ESC [ params final).
 * Seuls le curseur et les effacements sont traités ; le reste (couleurs, modes,
 * visibilité du curseur…) est ignoré.
 */
function applyCsi(paramString, finalChar) {
    const params = paramString
        .replace(/^[?<>!=]/, "") // marqueurs privés (?25, >0…) ignorés
        .split(";")
        .map((value) => {
            const parsed = parseInt(value, 10);
            return Number.isNaN(parsed) ? 0 : parsed;
        });

    const first = params[0] || 0;
    const second = params[1] || 0;

    switch (finalChar) {
        case "A": // curseur vers le haut
            cursorRow = Math.max(0, cursorRow - (first || 1));
            break;
        case "B": // curseur vers le bas
            cursorRow += first || 1;
            break;
        case "C": // curseur vers la droite
            cursorCol += first || 1;
            break;
        case "D": // curseur vers la gauche
            cursorCol = Math.max(0, cursorCol - (first || 1));
            break;
        case "E": // début de la ligne suivante
            cursorCol = 0;
            cursorRow += first || 1;
            break;
        case "F": // début de la ligne précédente
            cursorCol = 0;
            cursorRow = Math.max(0, cursorRow - (first || 1));
            break;
        case "G": // colonne absolue
            cursorCol = Math.max(0, (first || 1) - 1);
            break;
        case "H": // position absolue (ligne ; colonne)
        case "f":
            cursorRow = Math.max(0, (first || 1) - 1);
            cursorCol = Math.max(0, (second || 1) - 1);
            break;
        case "J": // effacement écran
            eraseDisplay(first);
            break;
        case "K": // effacement ligne
            eraseLine(first);
            break;
        case "s": // mémorise la position du curseur
            saveCursor();
            break;
        case "u": // restaure la position du curseur
            restoreCursor();
            break;
        default: // couleurs (m), modes (h/l), statut du terminal (n)… ignorés
            break;
    }

    ensureRow(cursorRow);
}

/**
 * Consomme une séquence d'échappement à partir de `start`.
 * Retourne le nombre de caractères consommés, ou 0 si la séquence est
 * incomplète (elle sera alors rejouée au prochain chunk).
 */
function consumeEscape(data, start) {
    const next = data[start + 1];
    if (next === undefined)
        return 0; // ESC seul en fin de chunk

    // CSI : ESC [ paramètres finale
    if (next === "[") {
        let i = start + 2;
        while (i < data.length && data[i] >= " " && data[i] <= "?")
            i++;
        if (i >= data.length)
            return 0; // paramètres incomplets
        if (data[i] < "@" || data[i] > "~")
            return 0; // caractère final invalide
        applyCsi(data.slice(start + 2, i), data[i]);
        return i - start + 1;
    }

    // OSC : ESC ] … (BEL ou ESC \) — titre de fenêtre, hyperliens…
    if (next === "]") {
        let i = start + 2;
        while (i < data.length) {
            if (data[i] === "\u0007")
                return i - start + 1;
            if (data[i] === "\u001b" && data[i + 1] === "\\")
                return i - start + 2;
            i++;
        }
        return 0; // fin de séquence non encore reçue
    }

    // Séquences courtes
    if (next === "7") { saveCursor(); return 2; }
    if (next === "8") { restoreCursor(); return 2; }
    if (next === "D") { newLine(); return 2; }                 // index
    if (next === "E") { cursorCol = 0; newLine(); return 2; }  // next line
    if (next === "M") {                                       // reverse index
        cursorRow = Math.max(0, cursorRow - 1);
        return 2;
    }
    if (next === "c") { resetScreen(); return 2; }             // reset matériel (clear total)
    if ("()#%".includes(next))                                 // jeu de caractères : ESC ( B
        return data.length > start + 2 ? 3 : 0;

    return 2; // tout autre ESC x : ignoré
}

/**
 * Traite un morceau (chunk) reçu du serveur : interprète les séquences ANSI
 * (positionnement du curseur, effacement écran/ligne, clear…) et met à jour
 * l'écran virtuel. Les séquences coupées entre deux paquets sont conservées.
 * Le rendu est transmis à print() pour être appliqué dans onUpdate().
 */
function onChunk(chunk) {
    const data = escapeBuffer + chunk;
    escapeBuffer = "";

    let i = 0;
    while (i < data.length) {
        const ch = data[i];

        if (ch === "\u001b") {
            const consumed = consumeEscape(data, i);
            if (consumed === 0) {
                escapeBuffer = data.slice(i); // séquence incomplète : on la garde
                break;
            }
            i += consumed;
            continue;
        }

        switch (ch) {
            case "\r": cursorCol = 0; break;                     // retour chariot
            case "\n": newLine(); break;                         // saut de ligne
            case "\b": if (cursorCol > 0) cursorCol--; break;    // retour arrière
            case "\t": cursorCol += 8 - (cursorCol % 8); break;  // tabulation
            case "\u0007": break;                                // cloche (ignorée)
            default:
                if (ch >= " " && ch !== "\u007f")
                    putChar(ch);
                break;
        }

        i++;
    }

    print(renderScreen());
}

/** Convertit l'écran virtuel en texte affichable (sans espaces de fin de ligne). */
function renderScreen() {
    const lines = screen.map((line) => line.join(""));

    while (lines.length > 0 && lines[lines.length - 1] === "")
        lines.pop();

    return lines.map((line) => line.replace(/\s+$/, "")).join("\n");
}

export async function onAwake() {
    destroyed = false;
    pendingText = null;
    needPrint = false;
    resetScreen();
    loop();
}

export function onUpdate() {
    if (!needPrint)
        return;

    needPrint = false;

    if (exports.result)
        exports.result.text = pendingText;
}

async function loop() {
    if (destroyed) return;

    console.log(`[TCP Client] Connecting to ${exports.host}:${exports.port}...`);

    // Nouvelle connexion : on repart d'un écran propre
    resetScreen();

    // Indique que la tentative de connexion est en cours
    print("connecting...");

    await tryConnect();

    if (!destroyed) {
        // Indique la déconnexion après l'échec ou la fermeture du socket
        print("disconnected");

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
        print("connected");

        socket.on("data", (data) => {
            if (destroyed || !exports.result)
                return;

            // Chaque chunk est interprété par le terminal virtuel (curseur, clear…)
            onChunk(from(data).toString("utf8"));
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

    pendingText = null;
    needPrint = false;
    resetScreen();

    if (exports.result)
        exports.result.text = "disconnected";

    if (socket) {
        await socket.close();
        socket = null;
    }
}