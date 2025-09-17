import { log } from 'console';

export let exports = {
  list: null
};

let list = [];

export function onPlayerCollisionEnter(player) {
  log(`Player ${player.GetDisplayName()} entered the zone`);
  list.push(player.GetDisplayName());
  updateList();
}

export function onPlayerCollisionExit(player) {
  log(`Player ${player.GetDisplayName()} exited the zone`);
  list = list.filter(p => p !== player.GetDisplayName());
  updateList();
}

export function onAwake() {
  log(exports);
  log(exports?.list);
  updateList();
}

function updateList() {
  if(!exports || !exports.list) return;
  exports.list.text = list.join('\n');
}
