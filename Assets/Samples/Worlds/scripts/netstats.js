import time from 'time';
import players from 'players';
import network from 'network';
import console from 'console';

/**
 * Network stats — live session connection state, rendered into a text component.
 *
 * Backend-agnostic: it only uses the `network` / `players` scripting modules, so it works with
 * any session exposing network information (relay, a future networked backend, …) and simply
 * reports "Disconnected" for sessions without a transport (offline).
 *
 * Bind `exports.result` to any component exposing a `text` property (UnityEngine.UI.Text,
 * TextMeshPro, …) and, optionally, override `exports.pattern` / `exports.interval` in the
 * inspector.
 *
 * Live values come from the `network` module (read through the namespace: a named import would
 * freeze the value at load time):
 *   network.connected        transport connected
 *   network.reconnecting     automatic reconnection loop running
 *   network.reconnectAttempt current attempt (0 when not reconnecting)
 *   network.ping             round-trip latency in ms, sub-millisecond precision
 *                            (-1 when never measured)
 *   network.tickrate         server ticks per second
 *   network.payloadSize      maximum payload of a custom event
 *   network.time             server time
 *
 * Available placeholders in `pattern`:
 *   {state} {connected} {reconnecting} {attempt} {ping} {tickrate} {payload} {players} {time}
 */

const DEFAULT_PATTERN =
    'Network\n' +
    'State: {state}\n' +
    'Connected: {connected}\n' +
    'Reconnecting: {reconnecting} (attempt {attempt})\n' +
    'Ping: {ping} ms\n' +
    'Tick rate: {tickrate} t/s\n' +
    'Payload limit: {payload} B\n' +
    'Players: {players}\n' +
    'Server time: {time}';

export let exports = {
    /** Text component receiving the rendered stats (bound in the inspector). */
    result: null,
    /** Template rendered into `result.text`. */
    pattern: DEFAULT_PATTERN,
    /** Refresh period in seconds. */
    interval: 0.25,
};

// ── State ────────────────────────────────────────────────────────────────

let elapsed = 0;
let lastRendered = null;

// ── Lifecycle ────────────────────────────────────────────────────────────

export function onAwake() {
    refresh();
}

export function onUpdate() {
    if (!exports.result)
        return;

    elapsed += time.deltaTime;
    const interval = exports.interval > 0 ? exports.interval : 0.25;
    if (elapsed < interval)
        return;

    elapsed = 0;
    refresh();
}

export function onConnected() {
    console.log('Network connected');
    refresh(true);
}

export function onDisconnected(reason) {
    console.log(`Network disconnected: ${reason || 'unknown reason'}`);
    refresh(true);
}

export function onReconnecting(attempt) {
    console.log(`Network connection lost — reconnecting (attempt ${attempt})`);
    refresh(true);
}

export function onReconnected() {
    console.log('Network connection restored');
    refresh(true);
}

export function onPlayerJoined(player) {
    refresh(true);
}

export function onPlayerLeft(player) {
    refresh(true);
}

// ── Rendering ────────────────────────────────────────────────────────────

/** Force a render on the next call even if the value did not change (`force`). */
export function refresh(force) {
    if (!exports.result)
        return;

    const text = render();
    if (!force && text === lastRendered)
        return;

    lastRendered = text;
    exports.result.text = text;
}

function render() {
    const connected = !!network.connected;
    const reconnecting = !!network.reconnecting;
    const attempt = network.reconnectAttempt | 0;

    let state;
    if (reconnecting)
        state = `Reconnecting (attempt ${attempt})`;
    else if (connected)
        state = 'Connected';
    else
        state = 'Disconnected';

    const ping = typeof network.ping === 'number' && network.ping > 0
        ? network.ping.toFixed(2)
        : 'N/A';
    const tickrate = network.tickrate | 0;
    const payload = network.payloadSize | 0;
    const count = players.count | 0;

    const pattern = typeof exports.pattern === 'string' && exports.pattern.length > 0
        ? exports.pattern
        : DEFAULT_PATTERN;

    return pattern
        .replace(/\{state\}/g, state)
        .replace(/\{connected\}/g, connected ? 'yes' : 'no')
        .replace(/\{reconnecting\}/g, reconnecting ? 'yes' : 'no')
        .replace(/\{attempt\}/g, String(attempt))
        .replace(/\{ping\}/g, String(ping))
        .replace(/\{tickrate\}/g, String(tickrate))
        .replace(/\{payload\}/g, String(payload))
        .replace(/\{players\}/g, String(count))
        .replace(/\{time\}/g, formatTime(network.time));
}

function formatTime(value) {
    if (value === null || value === undefined)
        return 'N/A';

    // A DateTime converted by the engine is either a JS Date or a CLR object exposing ToString().
    try {
        if (typeof value.toLocaleTimeString === 'function')
            return value.toLocaleTimeString();
    } catch {
        // fall through to String()
    }

    const text = String(value);
    if (text === '0001-01-01 00:00:00' || text.startsWith('0001-01-01'))
        return 'N/A';

    // Trim the date part of an ISO-like timestamp (0001-01-01T00:00:00 → 00:00:00).
    const match = /(\d{2}:\d{2}:\d{2})/.exec(text);
    return match ? match[1] : text;
}
