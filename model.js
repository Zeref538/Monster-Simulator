(function (root) {
  'use strict';
  class Monster {
    constructor(happiness = 75, stamina = 100) {
      if (!Number.isFinite(happiness) || !Number.isFinite(stamina)) throw new TypeError('State must be finite numbers.');
      this.hundredths = Math.round(Math.max(0, Math.min(100, happiness)) * 100);
      this.stamina = Math.max(0, Math.min(100, stamina));
      this.ticks = 0;
    }
    get happiness() { return this.hundredths / 100; }
    get mood() { return this.hundredths === 10000 ? 'happy' : this.hundredths <= 5000 ? 'sad' : 'normal'; }
    get canWork() { return this.stamina >= 20; }
    get canSleep() { return this.stamina < 20; }
    tick() { this.hundredths = Math.max(0, this.hundredths - 1); this.ticks++; }
    act(action) {
      if (!['play', 'study', 'eat', 'sleep'].includes(action)) return false;
      if ((action === 'play' || action === 'study') && !this.canWork) return false;
      if (action === 'sleep' && !this.canSleep) return false;
      if (action === 'play') { this.hundredths += 50; this.stamina -= 10; }
      if (action === 'study') { this.hundredths -= 50; this.stamina -= 10; }
      if (action === 'eat') { this.hundredths += 500; this.stamina += 20; }
      if (action === 'sleep') this.stamina = 100;
      this.hundredths = Math.max(0, Math.min(10000, this.hundredths));
      this.stamina = Math.max(0, Math.min(100, this.stamina));
      return true;
    }
  }
  root.Monster = Monster;
  if (typeof module !== 'undefined') module.exports = Monster;
})(typeof globalThis !== 'undefined' ? globalThis : window);
