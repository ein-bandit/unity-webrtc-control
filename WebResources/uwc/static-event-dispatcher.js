/*! static-event-dispatcher.js
 *
 * author: ein-bandit <github.com/ein-bandit>
 * Licensed under GPL 3.0 */

import EventDispatcher from "./../lib/event-dispatcher.js";

var _instance = null;

class StaticEventDispatcher {
  constructor() {
    if (_instance) {
      return _instance;
    }
    this.baseEventDispatcher = new EventDispatcher();
    _instance = this;
  }

  on(eventName, handler) {
    this.baseEventDispatcher.on(eventName, handler);
  }
  off(eventName, handler) {
    this.baseEventDispatcher.off(eventName, handler);
  }
  emit(eventName, parameters) {
    this.baseEventDispatcher.emit(eventName, parameters);
  }
}

export default StaticEventDispatcher;
