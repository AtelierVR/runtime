import { log } from 'console';
import { getCount, getAt } from 'players';

export let exports = {
  list: null,
  players: null,
};

let list = [];
let players = [];

export function onPlayerCollisionEnter(player) {
  log(`Player ${player.GetDisplay()} entered the zone`);
  list.push(player.GetDisplay());
  updateList();
}

export function onPlayerCollisionExit(player) {
  log(`Player ${player.GetDisplay()} exited the zone`);
  list = list.filter(p => p !== player.GetDisplay());
  updateList();
}

export function onPlayerJoined(player) {
  log(`Player ${player.GetDisplay()} joined the game`);
  players.push(player.GetDisplay());
  updatePlayerList();
}

export function onPlayerLeft(player) {
  log(`Player ${player.GetDisplay()} left the game`);
  players = players.filter(p => p !== player.GetDisplay());
  list = list.filter(p => p !== player.GetDisplay());
  updateList();
  updatePlayerList();
}

export function onAwake() {
  log(exports);
  log(exports?.list);
  for(var i = 0; i < getCount(); i++) {
    let p = getAt(i);
    players.push(p.GetDisplay());
    log(`Player ${p.GetDisplay()} is in the game`);
  }
  log(list);
  log(players);
  updatePlayerList();
  updateList();
}

function updateList() {
  if (!exports || !exports.list) return;
  exports.list.text = list.join('\n');
}

function updatePlayerList() {
  if (!exports || !exports.players) return;
  exports.players.text = players.join('\n');
}


