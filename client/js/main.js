import { handleBongoMessage } from "./bongocats.js";

let ws = null;

function ts() {
  return new Date().toISOString().slice(11, 23);
}

function logAppend(kind, text) {
  const empty = document.getElementById("empty");
  if (empty) empty.remove();

  const log = document.getElementById("log");
  const div = document.createElement("div");
  div.className = `entry ${kind}`;
  div.innerHTML = `<span class="ts">${ts()}</span><span class="kind">${kind}</span><span class="body">${escHtml(text)}</span>`;

  const atBottom = log.scrollHeight - log.scrollTop - log.clientHeight < 40;
  log.appendChild(div);
  if (atBottom) log.scrollTop = log.scrollHeight;
}

function escHtml(str) {
  return str.replace(/&/g, "&amp;").replace(/</g, "&lt;").replace(/>/g, "&gt;");
}

function setStatus(state) {
  const el = document.getElementById("status");
  el.className = state;
}

function toggle() {
  ws ? disconnect() : connect();
}

function connect() {
  const url = document.getElementById("url-input").value.trim();
  setStatus("connecting");
  document.getElementById("toggle-btn").textContent = "Connecting…";

  ws = new WebSocket(url);

  ws.onopen = () => {
    setStatus("connected");
    document.getElementById("toggle-btn").textContent = "Disconnect";
    logAppend("open", `connected to ${url}`);
  };

  ws.onmessage = (e) => {
    logAppend("msg", e.data);
    handleBongoMessage(e.data);
  };

  ws.onclose = (e) => {
    setStatus("disconnected");
    document.getElementById("toggle-btn").textContent = "Connect";
    logAppend(
      "close",
      `closed (code ${e.code}${e.reason ? ": " + e.reason : ""})`,
    );
    ws = null;
  };

  ws.onerror = () => logAppend("error", "websocket error");
}

function disconnect() {
  ws?.close();
}

function clearLog() {
  const log = document.getElementById("log");
  log.innerHTML = '<div id="empty">no events yet</div>';
}

window.toggle = toggle;
window.clearLog = clearLog;

window.hello = () => console.log("hi");
console.log("done loading main.js");
