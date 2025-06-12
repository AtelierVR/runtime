import { log } from "console";

let d = Date.now();

export function onFixedUpdate() {
    let delta = Date.now() - d;
    d = Date.now();
    gameObject.transform.Rotate(delta * -0.1, delta * 0.1, delta * -0.1);
}

export function onAwake() {
    log("test", "Hello World");
}

export function onPrepare() {
    log("onPrepare called");
}

export function onValidate() {
    log("onValidate called");
    log("onValidate called", gameObject.name);
    log("74");
}