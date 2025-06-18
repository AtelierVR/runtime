import { log } from "console";
import { transform, gameObject } from "behaviour";

let d = Date.now();

export let speed = 0.1;

export function onUpdate() {
    let delta = Date.now() - d;
    d = Date.now();
    transform.Rotate(delta * -speed, delta * speed, delta * -speed);
}

export function onAwake() {
    log("test", "Hello World");
}

export function onPrepare() {
    log("onPrepare called");
}

export function onValidate() {
    log("onValidate called", gameObject.name);
}