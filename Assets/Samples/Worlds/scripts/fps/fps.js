import { deltaTime } from 'time';

export let exports = {
    pattern: "{fps} FPS",
    result: null
};

let frameCount = 0;
let elapsedTime = 0;

export function onUpdate() {
    if (!exports.result || !exports.pattern)
        return;

    frameCount++;
    elapsedTime += deltaTime;

    if (elapsedTime >= 1.0) {
        const fps = Math.round(frameCount / elapsedTime);
        const text = exports.pattern.replace("{fps}", fps);
        exports.result.text = text;
        frameCount = 0;
        elapsedTime = 0;
    }
}