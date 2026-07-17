// Minimal xterm.js host for AgentHelm. The terminal is a renderer only:
// input goes through the Blazor input box (there is no PTY server-side, so
// per-keystroke echo would be misleading). convertEol maps \n to \r\n.
window.helmTerm = {
  term: null,
  init(elementId) {
    if (this.term) { this.term.dispose(); this.term = null; }
    const host = document.getElementById(elementId);
    if (!host || typeof Terminal === "undefined") return;
    this.term = new Terminal({
      convertEol: true,
      fontSize: 13,
      fontFamily: '"Cascadia Code", "JetBrains Mono", Consolas, monospace',
      theme: { background: "#0d1220", foreground: "#e7ecf3", cursor: "#e0a458" }
    });
    this.term.open(host);
  },
  write(data) { if (this.term) this.term.write(data); },
  echoInput(line) { if (this.term) this.term.write("\x1b[38;2;224;164;88m$ " + line + "\x1b[0m\r\n"); },
  clear() { if (this.term) this.term.clear(); }
};
