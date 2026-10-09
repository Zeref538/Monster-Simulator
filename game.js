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
  document.querySelector('.happiness-art .meter-fill').style.clipPath = 'inset(0 ' + (100-monster.happiness) + '% 0 0)';
  document.querySelector('.stamina-art .meter-fill').style.clipPath = 'inset(0 ' + (100-monster.stamina) + '% 0 0)';
  byId('ticks').textContent = monster.ticks;
  byId('mood').textContent = monster.mood.toUpperCase();
  document.body.dataset.mood = monster.mood;
  byId('play').disabled = byId('study').disabled = !monster.canWork;
  byId('sleep').disabled = !monster.canSleep;
  byId('restriction').textContent = monster.canSleep ? 'Too tired to play or study. Ready for a nap.' : 'A little play, a little care.';
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
  byId('bubble').textContent = {normal:'Hi! Want to play?', happy:'One more game?', sad:'Keep me company?', tired:'I could use a little rest.'}[button.dataset.scenario];
  render();
}));
let lastImage = '';
function centerBody() {
  const image = byId('pet');
  if (!image.naturalWidth) return;
  const filename = image.src.split('/').pop();
  const center = BODY_CENTERS[filename] ?? 0.5;
  const renderedWidth = Math.min(image.clientWidth, image.clientHeight * image.naturalWidth / image.naturalHeight);
  image.style.marginLeft = ((0.5-center)*renderedWidth) + 'px';
}
byId('pet').addEventListener('load', centerBody);
window.addEventListener('resize', centerBody);
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
