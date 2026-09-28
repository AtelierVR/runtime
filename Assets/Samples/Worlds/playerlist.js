import console from 'console';
import { all } from 'players';

export let exports = {
  players: null,
};

/** Last rendered text, so the label is only re-assigned when it actually changes. */
let last = null;

export function onPlayerJoined(player) {
  console.log(`Player ${player.display} joined the game`);
  updateList();
}

export function onPlayerLeft(player) {
  console.log(`Player ${player.display} left the game`);
  updateList();
}

export function onAwake() {
  all.forEach(p => console.log(`Player ${p.display} is in the game`));
  updateList();
}

export function onUpdate() {
  // Teams are picked after joining: re-render so the colours always follow the current teams.
  updateList();
}

function updateList() {
  if (!exports || !exports.players) return;

  const text = all.map(format).join('\n');
  if (text === last) return;

  last = text;
  exports.players.text = text;
}

/** `Display` wrapped in a RichText colour tag when the player belongs to a team. */
function format(player) {
  const display = player.display ?? '';
  const color = teamColor(player);
  return color ? `<color=${color}>${display}</color>` : display;
}

/** `<color=#RRGGBB>` value of the team of a player, or null when it has none. */
function teamColor(player) {
  const team = player.team;
  const color = team ? team.color : null;
  return color ? toHex(color) : null;
}

/** `#RRGGBB` of a `UnityEngine.Color`. */
function toHex(color) {
  return '#' + channel(color.r) + channel(color.g) + channel(color.b);
}

function channel(value) {
  const byte = Math.max(0, Math.min(255, Math.round((value ?? 0) * 255)));
  return (byte < 16 ? '0' : '') + byte.toString(16);
}


