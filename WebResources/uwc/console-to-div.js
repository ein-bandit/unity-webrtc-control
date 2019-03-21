/*! console-to-div.js
 *
 * author: ein-bandit <github.com/ein-bandit>
 * Licensed under GPL 3.0 */

export function logToDiv(id) {
  var logger = document.getElementById(id || "logger");
  console.log(logger);
  if (logger) {
    logger.classList.remove("hidden");
    console.log = (function(old_function, div_log) {
      return function(text, ...data) {
        old_function(text, ...data);
        div_log.innerHTML += `<div> > ${text}, ${data} </div>`;
        div_log.scrollTop = div_log.scrollHeight;
      };
    })(console.log.bind(console), logger);
  } else {
    console.log(`No div with id ${id || "logger"} for debug logging found.`);
  }
}
