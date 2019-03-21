"use strict";

const assertValidEventName = function(eventName) {
  if (!eventName || typeof eventName !== "string") {
    throw new Error("Event name should be a valid non-empty string!");
  }
};

const assertValidHandler = function(handler) {
  if (typeof handler !== "function") {
    throw new Error("Handler should be a function!");
  }
};

const assertAllowedEventName = function(allowedEvents, eventName) {
  if (allowedEvents && allowedEvents.indexOf(eventName) < 0) {
    throw new Error(`Event "${eventName}" is not allowed!`);
  }
};

const p = Object.freeze({
  allowedEvents: Symbol("allowedEvents"),
  listeners: Symbol("listeners")
});

export default class EventDispatcher {
  constructor(allowedEvents) {
    if (typeof allowedEvents !== "undefined" && !Array.isArray(allowedEvents)) {
      throw new Error("Allowed events should be a valid array of strings!");
    }

    this[p.listeners] = new Map();
    this[p.allowedEvents] = allowedEvents;
  }

  /**
   * Registers listener function to be executed once event occurs.
   *
   * @param {string} eventName Name of the event to listen for.
   * @param {function} handler Handler to be executed once event occurs.
   */
  on(eventName, handler) {
    assertValidEventName(eventName);
    assertAllowedEventName(this[p.allowedEvents], eventName);
    assertValidHandler(handler);

    let handlers = this[p.listeners].get(eventName);
    if (!handlers) {
      handlers = new Set();
      this[p.listeners].set(eventName, handlers);
    }

    // Set.add ignores handler if it has been already registered.
    handlers.add(handler);
  }

  /**
   * Registers listener function to be executed only first time when event
   * occurs.
   *
   * @param {string} eventName Name of the event to listen for.
   * @param {function} handler Handler to be executed once event occurs.
   */
  once(eventName, handler) {
    assertValidHandler(handler);

    const once = parameters => {
      this.off(eventName, once);

      handler.call(this, parameters);
    };

    this.on(eventName, once);
  }

  /**
   * Removes registered listener for the specified event.
   *
   * @param {string} eventName Name of the event to remove listener for.
   * @param {function} handler Handler to remove, so it won't be executed
   * next time event occurs.
   */
  off(eventName, handler) {
    assertValidEventName(eventName);
    assertAllowedEventName(this[p.allowedEvents], eventName);
    assertValidHandler(handler);

    const handlers = this[p.listeners].get(eventName);
    if (!handlers) {
      return;
    }

    handlers.delete(handler);

    if (!handlers.size) {
      this[p.listeners].delete(eventName);
    }
  }

  /**
   * Removes all registered listeners for the specified event.
   *
   * @param {string=} eventName Name of the event to remove all listeners for.
   */
  offAll(eventName) {
    if (typeof eventName === "undefined") {
      this[p.listeners].clear();
      return;
    }

    assertValidEventName(eventName);
    assertAllowedEventName(this[p.allowedEvents], eventName);

    const handlers = this[p.listeners].get(eventName);
    if (!handlers) {
      return;
    }

    handlers.clear();

    this[p.listeners].delete(eventName);
  }

  /**
   * Emits specified event so that all registered handlers will be called
   * with the specified parameters.
   *
   * @param {string} eventName Name of the event to call handlers for.
   * @param {Object=} parameters Optional parameters that will be passed to
   * every registered handler.
   */
  emit(eventName, parameters) {
    assertValidEventName(eventName);
    assertAllowedEventName(this[p.allowedEvents], eventName);

    const handlers = this[p.listeners].get(eventName);
    if (!handlers) {
      return;
    }

    handlers.forEach(handler => {
      try {
        handler.call(this, parameters);
      } catch (error) {
        console.error(error);
      }
    });
  }

  /**
   * Checks if there are any listeners that listen for the specified event.
   *
   * @param {string} eventName Name of the event to check listeners for.
   * @returns {boolean}
   */
  hasListeners(eventName) {
    assertValidEventName(eventName);
    assertAllowedEventName(this[p.allowedEvents], eventName);

    return this[p.listeners].has(eventName);
  }

  /**
   * Mixes dispatcher methods into target object.
   * @param {Object} target Object to mix dispatcher methods into.
   * @param {Array.<string>?} allowedEvents Optional list of the allowed event
   * names that can be emitted and listened for.
   * @returns {Object} Target object with added dispatcher methods.
   */
  static mixin(target, allowedEvents) {
    if (!target || typeof target !== "object") {
      throw new Error("Object to mix into should be valid object!");
    }

    if (typeof allowedEvents !== "undefined" && !Array.isArray(allowedEvents)) {
      throw new Error("Allowed events should be a valid array of strings!");
    }

    const dispatcher = new EventDispatcher(allowedEvents);
    ["on", "once", "off", "offAll", "emit", "hasListeners"].forEach(method => {
      if (typeof target[method] !== "undefined") {
        throw new Error(
          `Object to mix into already has "${method}" property defined!`
        );
      }

      if (method !== "constructor") {
        target[method] = dispatcher[method].bind(dispatcher);
      }
    }, dispatcher);

    return target;
  }
}
