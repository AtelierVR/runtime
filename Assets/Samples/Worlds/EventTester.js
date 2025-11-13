import { log } from 'console';
import { emitEvent } from 'network';
import { id } from 'behaviour';

export let exports = {
    result: null,
};

export function onClick() {
    var rng = Math.random().toString(36).substr(2, 8);
    log(`Generated random string: ${rng}`);
    let text = id + ":" + rng;
    log(`Emitting event with message: ${text}`);
    if (exports?.result)
        exports.result.text = text;
    
    // Convertir la chaîne en Uint8Array (UTF-8)
    var utf8 = [];
    for (var i = 0; i < text.length; i++) {
        var charcode = text.charCodeAt(i);
        if (charcode < 0x80) utf8.push(charcode);
        else if (charcode < 0x800) {
            utf8.push(0xc0 | (charcode >> 6), 0x80 | (charcode & 0x3f));
        }
        else if (charcode < 0xd800 || charcode >= 0xe000) {
            utf8.push(0xe0 | (charcode >> 12), 0x80 | ((charcode >> 6) & 0x3f), 0x80 | (charcode & 0x3f));
        }
        else {
            i++;
            charcode = 0x10000 + (((charcode & 0x3ff) << 10) | (text.charCodeAt(i) & 0x3ff));
            utf8.push(0xf0 | (charcode >> 18), 0x80 | ((charcode >> 12) & 0x3f), 0x80 | ((charcode >> 6) & 0x3f), 0x80 | (charcode & 0x3f));
        }
    }
    
    emitEvent("testEvent", utf8);
}

export function onEvent(name, raw, sender) {
    if (name !== "testEvent") return;
    log(raw);
    
    // Convertir Uint8Array en chaîne (UTF-8)
    var message = "";
    var i = 0;
    while (i < raw.length) {
        var byte1 = raw[i++];
        if (byte1 < 0x80) {
            message += String.fromCharCode(byte1);
        }
        else if ((byte1 & 0xe0) === 0xc0) {
            var byte2 = raw[i++];
            message += String.fromCharCode(((byte1 & 0x1f) << 6) | (byte2 & 0x3f));
        }
        else if ((byte1 & 0xf0) === 0xe0) {
            var byte2 = raw[i++];
            var byte3 = raw[i++];
            message += String.fromCharCode(((byte1 & 0x0f) << 12) | ((byte2 & 0x3f) << 6) | (byte3 & 0x3f));
        }
        else if ((byte1 & 0xf8) === 0xf0) {
            var byte2 = raw[i++];
            var byte3 = raw[i++];
            var byte4 = raw[i++];
            var codepoint = ((byte1 & 0x07) << 18) | ((byte2 & 0x3f) << 12) | ((byte3 & 0x3f) << 6) | (byte4 & 0x3f);
            codepoint -= 0x10000;
            message += String.fromCharCode((codepoint >> 10) + 0xd800, (codepoint & 0x3ff) + 0xdc00);
        }
    }
    
    log(`Received event from ${sender}: ${message}`);
    if (exports?.result)
        exports.result.text = message;
}

