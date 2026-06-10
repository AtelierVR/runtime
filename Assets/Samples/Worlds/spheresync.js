import { transform, rigidbody } from 'behaviour';
import { local } from 'players';
import { emit } from 'network';
import { crc64 } from 'hashing';
import { from, toString } from 'buffer';
import { Vector3, Quaternion } from 'unity';
import console from 'console';
import Gizmo, { isEnabled, wireSphere, wireDisc, line } from 'gizmo';

// Event ID for sphere sync packets

export let exports = {
    zoneRadius: 5,
    target: null
};

// Zone center: parent transform position, or spawn position as fallback
let zoneCenter = null;

export function onAwake() {
    zoneCenter = exports?.target?.position ?? transform.position;
    console.log(`SyncedSphere: zone center (${zoneCenter.x}, ${zoneCenter.y}, ${zoneCenter.z}), radius ${exports.zoneRadius}`);
}

const SYNC_EVENT = crc64('sphere.sync');

// Master broadcasts physics state every tick
export function onTick() {
    if (!local?.isMaster) return;

    const pos = transform.position;
    const rot = transform.rotation;
    const vel = rigidbody ? rigidbody.linearVelocity : Vector3.zero;
    const ang = rigidbody ? rigidbody.angularVelocity : Vector3.zero;

    const json = JSON.stringify({
        px: pos.x, py: pos.y, pz: pos.z,
        rx: rot.x, ry: rot.y, rz: rot.z, rw: rot.w,
        vx: vel.x, vy: vel.y, vz: vel.z,
        ax: ang.x, ay: ang.y, az: ang.z
    });

    emit(SYNC_EVENT, from(json, 'utf8'));
}

// Non-master clients receive and apply the broadcasted state
export function onEvent(key, raw, sender) {
    if (key !== SYNC_EVENT) return;
    if (local?.isMaster) return; // master already owns the simulation

    const json = toString(raw, 'utf8');
    if (!json) return;

    const d = JSON.parse(json);

    transform.position = Vector3.from(d.px, d.py, d.pz);
    transform.rotation = Quaternion.from(d.rx, d.ry, d.rz, d.rw);
    if (rigidbody) {
        rigidbody.linearVelocity  = Vector3.from(d.vx, d.vy, d.vz);
        rigidbody.angularVelocity = Vector3.from(d.ax, d.ay, d.az);
    }
}

// All clients enforce zone boundary locally
export function onFixedUpdate() {
    if (!rigidbody) return;

    const center = exports?.target?.position ?? zoneCenter;
    if (!center) return;

    const pos    = transform.position;
    const radius = exports?.zoneRadius ?? 5;
    const dx     = pos.x - center.x;
    const dy     = pos.y - center.y;
    const dz     = pos.z - center.z;
    const dist   = Math.sqrt(dx * dx + dy * dy + dz * dz);

    if (dist <= radius) return;

    // Clamp position to zone surface
    const scale = radius / dist;
    transform.position = Vector3.from(
        center.x + dx * scale,
        center.y + dy * scale,
        center.z + dz * scale
    );

    // Reflect the outward component of velocity (bounce)
    const vel = rigidbody.linearVelocity;
    const nx  = dx / dist;
    const ny  = dy / dist;
    const nz  = dz / dist;
    const dot = vel.x * nx + vel.y * ny + vel.z * nz;
    if (dot > 0) 
        rigidbody.linearVelocity = Vector3.from(
            vel.x - 2 * dot * nx,
            vel.y - 2 * dot * ny,
            vel.z - 2 * dot * nz
        );
}

export function onGizmo() {
    if (!isEnabled) return;
    const center = exports?.target?.position ?? zoneCenter;
    if (!center) return;
    const radius = exports?.zoneRadius ?? 5;

    // Zone sphere boundary (green)
    Gizmo.color = [0, 1, 0, 0.8];
    wireSphere(center, radius);

    // Ground circle at bottom of zone (white)
    Gizmo.color = [1, 1, 1, 1];
    wireDisc(Vector3.from(center.x, center.y, center.z), Vector3.from(0, 1, 0), radius);

    // Line from zone center to sphere (yellow)
    const pos = transform.position;
    Gizmo.color = [1, 1, 0, 1];
    line(center, pos);
}
