import { log, warn } from 'console';
import { emitEvent, eventToHash } from 'network';
import { id } from 'behaviour';
import { from as bufferFrom, toString as bufferToString } from 'buffer';

export let exports = {
    result: null,
    listen: "testEvent"
};

export function onClick() {
    var rng = Math.random().toString(36).substr(2, 8);
    let text = id + ":" + rng;
    log(`Emitting event with message: ${text}`);
    exports.result.text = text;
    
    // Utiliser Buffer pour convertir la chaîne en bytes (UTF-8)
    var utf8 = bufferFrom(text, "utf8");
    
    emitEvent(exports.listen, utf8);
}

export function onEvent(name, raw, sender) {
    if (name !== eventToHash(exports.listen)) {
        warn(`Received unknown event: ${name} (expected ${exports.listen}/${eventToHash(exports.listen)})`);
        return;
    }
    log(raw);
    
    // Utiliser Buffer pour convertir les bytes en string (UTF-8)
    var message = bufferToString(raw, "utf8");
    
    log(`Received event from ${sender}: ${message}`);
    exports.result.text = message;
}

