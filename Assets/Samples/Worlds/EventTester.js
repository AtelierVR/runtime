import { log, warn } from 'console';
import { emitEvent, eventToHash } from 'network';
import { id } from 'behaviour';
import { from as bufferFrom, toString as bufferToString } from 'buffer';

export let exports = {
    result: null,
    listen: "testEvent"
};

export function onClick() {
    const rng = Math.random().toString(36).substr(2, 8);
    const text = id + ":" + rng;
    log(`Emitting event with message: ${text}`);
    exports.result.text = text;
    
    const utf8 = bufferFrom(text, "utf8");
    
    emitEvent(exports.listen, utf8);
}

export function onEvent(key, raw, sender) {
    if (key !== eventToHash(exports.listen)) {
        warn(`Received unknown event: ${key} (expected ${exports.listen}/${eventToHash(exports.listen)})`);
        return;
    }
    log(raw);
    
    const message = bufferToString(raw, "utf8");
    
    log(`Received event from ${sender}: ${message}`);
    exports.result.text = message;
}

