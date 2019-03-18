var logger = document.getElementById("logger");
if (debug && logger) {
  logger.classList.add("visible");
  console.log = (function(old_function, div_log) {
    return function(text) {
      old_function(text);
      div_log.innerHTML += "<div> > " + text + " </div>";
      div_log.scrollTop = div_log.scrollHeight;
    };
  })(console.log.bind(console), logger);
}
