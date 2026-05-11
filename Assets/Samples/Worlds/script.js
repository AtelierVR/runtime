import { transform, rigidbody } from 'behaviour';
import { Vector3 } from 'unity';

let d = Date.now();
export let speed = 0.1;

export let exports = {
    target: null,
    distance: 10
};

export function onAwake() {
    reset();
}

function reset() {
    if (!exports?.target) return;
    transform.position = exports.target.position;
    if (!rigidbody) return;
    rigidbody.linearVelocity = Vector3.from(0, 0, 0);
    rigidbody.angularVelocity = Vector3.from(0, 0, 0);
}

export function onUpdate() {
    let delta = Date.now() - d;
    d = Date.now();
    transform.rotate(delta * -speed, delta * speed, delta * -speed);

    if (!exports?.target) return;
    const dis = Vector3.distance(transform.position, exports.target.position);
    if (dis > exports.distance) reset();
}