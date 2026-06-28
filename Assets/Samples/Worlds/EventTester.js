import console from 'console';
import { emit } from 'network';
import { crc64 } from 'hashing';
import { id } from 'behaviour';
import Buffer from 'buffer';

export let exports = {
    result: null,
    listen: "testEvent"
};

export function onClick() {
    const rng = Math.random().toString(36).substr(2, 8);
    const text = id + ":" + rng;
    exports.result.text = text;
    const utf8 = Buffer.from(text, "utf8");
    emit(exports.listen, utf8);
}

export function onEvent(key, raw, sender) {
    if (key !== crc64(exports.listen)) 
        return;
    console.log(raw);
    const message = Buffer.from(raw).toString("utf8");
    exports.result.text = message;
}

