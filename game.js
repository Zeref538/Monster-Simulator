'use strict';
let monster = new Monster();
let nextTick = performance.now() + 1000;
let actionUntil = 0;
let actionAnimation = 'idle';
const byId = id => document.getElementById(id);
function render() {
  byId('happiness').textContent = monster.happiness.toFixed(2) + ' / 100';
  byId('stamina').textContent = monster.stamina + ' / 100';
  byId('happy-bar').value = monster.happiness;
  byId('stamina-bar').value = monster.stamina;
  byId('ticks').textContent = monster.ticks;
  byId('mood').textContent = monster.mood.toUpperCase();
  document.body.dataset.mood = monster.mood;
  byId('play').disabled = byId('study').disabled = !monster.canWork;
  byId('sleep').disabled = !monster.canSleep;
  byId('restriction').textContent = monster.canSleep ? 'Stamina below 20: Play and Study locked. Sleep unlocked.' : 'Sleep locked: stamina must be below 20.';
}
document.querySelectorAll('[data-action]').forEach(button => button.addEventListener('click', () => {
  const action = button.dataset.action;
  if (!monster.act(action)) return;
  actionAnimation = {play:'playing', study:'studying', eat:'eating', sleep:'sleeping'}[action];
  actionUntil = performance.now() + 1800;
  byId('bubble').textContent = {play:'Let’s play!', study:'Time to learn.', eat:'Yum! That gives me energy.', sleep:'Rested! My stamina is full.'}[action];
  render();
}));
document.querySelectorAll('[data-scenario]').forEach(button => button.addEventListener('click', () => {
  const values = {normal:[75,100],happy:[99.5,100],sad:[50.5,100],tired:[75,20]}[button.dataset.scenario];
  monster = new Monster(...values); nextTick = performance.now() + 1000; actionUntil = 0;
  byId('bubble').textContent = 'Demo starting point loaded. Real rules remain active.';
  render();
}));
let lastImage = '';
function frame(now) {
  while (now >= nextTick) { monster.tick(); nextTick += 1000; render(); }
  const state = monster.mood === 'happy' ? 'happy' : monster.mood === 'sad' ? 'sad' : now < actionUntil ? actionAnimation : 'idle';
  const count = state === 'sad' ? 6 : 12;
  const number = Math.floor(now / 167) % count + 1;
  const src = 'assets/' + state + '_' + String(number).padStart(2,'0') + '.png';
  if (src !== lastImage) { byId('pet').src = src; lastImage = src; }
  requestAnimationFrame(frame);
}
render(); requestAnimationFrame(frame);
