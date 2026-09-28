import console from 'console';
import { local } from 'players';
import { all as registered, create } from 'teams';
import { emit } from 'network';
import { crc64 } from 'hashing';
import { from as bufferFrom, toString as bufferToString } from 'buffer';
import { Color } from 'unity';

/**
 * Team chooser — four fixed teams (Rouge, Vert, Bleu, Jaune).
 *
 * A player picks one with `choose(...)` and the choice is:
 *   1. applied to the entity itself (`player.team`), so the local game state is up to date;
 *   2. broadcast with the `team.choose` network event.
 * Every client applies the received team to the sender player (`sender.team`), so the same
 * choice is visible everywhere. Events are live only: a player joining triggers a
 * re-announcement of the choices already made.
 *
 * Nothing is persisted — call `choose` again after a reload to restore a team.
 */

// ── Teams ────────────────────────────────────────────────────────────────

/**
 * The four teams, in the order they are registered in the session (index 0-3).
 * The colours are `UnityEngine.Color` values (an object literal `{ r, g, b, a }` would not be
 * converted — the `teams.create` argument goes through the `Color` converter).
 */
const TEAMS = [
    { index: 0, name: 'Rouge', color: Color.red,    id: 0 },
    { index: 1, name: 'Vert',  color: Color.green,  id: 0 },
    { index: 2, name: 'Bleu',  color: Color.blue,   id: 0 },
    { index: 3, name: 'Jaune', color: Color.yellow, id: 0 },
];

/** Network event of a team choice (payload: the team index as an utf8 string). */
const CHOOSE_EVENT = crc64('team.choose');

// ── Exports (bound in the inspector) ─────────────────────────────────────

/** Text of the team button before the player picks a team. */
const DEFAULT_LABEL = 'Choose Team';

export let exports = {
    /** Text of the team button: the team of the local player. */
    button: null,
};

// ── State ────────────────────────────────────────────────────────────────

/** Index (0-3) of the local player's team, -1 for none. */
let myTeam = -1;
/** Set once the first announcement went out (retried in onUpdate while the session is not ready). */
let announced = false;

/** Team indexes the other players announced, keyed by player identifier. */
const chosen = {};

// ── Lifecycle ────────────────────────────────────────────────────────────

export function onAwake() {
    ensure();
    if (local) {
        announce();
        announced = true;
    }
    refresh();
}

export function onUpdate() {
    // The session (and so the local player) may not be ready on awake.
    if (announced || !local) return;

    ensure();
    announce();
    announced = true;
    refresh();
}

export function onPlayerJoined(player) {
    // Announce again so the newcomer learns the choices already made, and the other clients learn
    // ours (the newcomer announces its own on its own join).
    announce();
    refresh();
}

export function onPlayerLeft(player) {
    if (player) delete chosen[player.identifier];
    refresh();
}

export function onEvent(key, raw, sender) {
    if (key !== CHOOSE_EVENT || !sender) return;

    const index = parseInt(bufferToString(raw, 'utf8'), 10);
    if (isNaN(index) || index < -1 || index >= TEAMS.length) return;

    ensure();
    apply(sender, index);
    chosen[sender.identifier] = index;

    console.log(`${sender.display} joined team ${teamName(index)}`);
    refresh();
}

/** Cycles to the next team — handy to test the sync by clicking the object. */
export function onClick() {
    choose((myTeam + 1) % TEAMS.length);
}

// ── Choice ───────────────────────────────────────────────────────────────

/**
 * Picks a team among the four: `choose(0)` to `choose(3)` by index, `choose('Rouge')` by name, or
 * `choose(team)` with an `ITeam` from the `teams` module. Returns the chosen index, -1 for none
 * (`choose(null)`).
 */
export function choose(target) {
    const index = resolveIndex(target);

    ensure();
    myTeam = index;
    apply(local, index);

    announce();
    console.log(`Local team: ${teamName(index)}`);
    refresh();

    return myTeam;
}

/** Team id of a player (0 = no team). */
export function teamOf(player) {
    if (!player) return 0;

    const team = player.team;
    if (team) return team.id;

    const index = chosen[player.identifier];
    return index === undefined ? 0 : teamId(index);
}

// ── Teams registry ───────────────────────────────────────────────────────

/**
 * Registers the four teams in the session registry — once per client, reusing a team already
 * registered under the same name so every client ends up with the same ids.
 */
function ensure() {
    const existing = registered ?? [];

    for (const team of TEAMS) {
        if (team.id === 0)
            team.id = findId(existing, team.name) ?? (create(team.name, team.color)?.id ?? 0);
    }
}

function findId(list, name) {
    for (const team of list)
        if (String(team.name).toLowerCase() === name.toLowerCase())
            return team.id;
    return null;
}

/** Index of the team of `target`: a number (index), a name, or an `ITeam` from `teams`. */
function resolveIndex(target) {
    if (target === null || target === undefined)
        return -1;

    if (typeof target === 'number')
        return target >= 0 && target < TEAMS.length ? target : -1;

    if (typeof target === 'string') {
        const name = target.toLowerCase();
        for (const team of TEAMS)
            if (team.name.toLowerCase() === name)
                return team.index;
        return -1;
    }

    const id = target.id;
    for (const team of TEAMS)
        if (team.id === id)
            return team.index;
    return -1;
}

/** Team id of an index (0 = none). */
const teamId = index => index >= 0 && index < TEAMS.length ? TEAMS[index].id : 0;

/** Display name of an index. */
const teamName = index => index >= 0 && index < TEAMS.length ? TEAMS[index].name : 'none';

// ── Replication ──────────────────────────────────────────────────────────

/** Broadcasts the local choice (its index, identical on every client) to the other clients. */
function announce() {
    if (!local) return;
    emit(CHOOSE_EVENT, bufferFrom(`${myTeam}`, 'utf8'));
}

/** Applies a team (by index) to a player, locally. */
function apply(player, index) {
    if (!player) return;
    player.team = teamId(index);
}

// ── UI ───────────────────────────────────────────────────────────────────

function refresh() {
    if (exports?.button)
        exports.button.text = myTeam >= 0 ? TEAMS[myTeam].name : DEFAULT_LABEL;
}
