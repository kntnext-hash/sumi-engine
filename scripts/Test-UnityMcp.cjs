// Protocol smoke test only; an MCP handshake does not prove Editor connectivity.
const { spawn } = require('node:child_process');
const path = require('node:path');
const executable = process.argv[2] || path.join(process.env.LOCALAPPDATA, 'Microsoft/WindowsApps/unity.exe');
const project = path.resolve(__dirname, '../UnitySandbox');
const child = spawn(executable, ['mcp', '--project-path', project], { windowsHide: true });
let buffer = '', initialized = false, finished = false;
const timer = setTimeout(() => finish(false, 'MCP response timeout (20 seconds)'), 20000);
function finish(ok, message) {
  if (finished) return;
  finished = true;
  clearTimeout(timer);
  console.log(JSON.stringify({ success: ok, scope: process.argv.includes('--editor-probe') ? 'MCP Editor read probe' : 'MCP protocol only', message }));
  child.kill();
  process.exitCode = ok ? 0 : 1;
}
function send(value) { child.stdin.write(JSON.stringify(value) + '\n'); }
child.on('error', e => finish(false, e.message));
child.on('exit', code => { if (!finished) finish(false, `MCP exited before tools/list: ${code}`); });
child.stdin.on('error', e => finish(false, e.message));
// Do not print arbitrary diagnostics, which may contain machine/account details.
child.stderr.resume();
child.stdout.on('data', data => {
  buffer += data.toString();
  if (buffer.length > 1024 * 1024) return finish(false, 'MCP output exceeded 1 MiB');
  let end;
  while ((end = buffer.indexOf('\n')) >= 0) {
    const line = buffer.slice(0, end); buffer = buffer.slice(end + 1);
    let response;
    try { response = JSON.parse(line); } catch { continue; }
    if (response.error) return finish(false, `JSON-RPC error ${response.error.code}`);
    if (response.id === 1) {
      if (!response.result?.protocolVersion || !response.result?.serverInfo) return finish(false, 'Invalid initialize response');
      initialized = true;
      console.log(JSON.stringify({ protocolVersion: response.result.protocolVersion, serverInfo: response.result.serverInfo }));
      send({ jsonrpc: '2.0', method: 'notifications/initialized' });
      send({ jsonrpc: '2.0', id: 2, method: 'tools/list', params: {} });
    }
    if (response.id === 2 && initialized) {
      if (!Array.isArray(response.result?.tools)) return finish(false, 'Invalid tools/list response');
      console.log(JSON.stringify({ toolCount: response.result.tools.length }));
      if (process.argv.includes('--editor-probe')) {
        if (!response.result.tools.some(tool => tool.name === 'eval')) return finish(false, 'Editor eval unavailable');
        send({ jsonrpc: '2.0', id: 3, method: 'tools/call', params: { name: 'eval', arguments: {
          code: 'return new { version = UnityEngine.Application.unityVersion, scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().path };'
        } } });
      } else finish(true, 'initialize and tools/list completed; Editor operations must be verified separately');
    }
    if (response.id === 3) {
      console.log(JSON.stringify({ editorProbe: response.result }));
      let payload;
      try { payload = JSON.parse(response.result.content.find(item => item.type === 'text').text); } catch {}
      const ok = !response.result?.isError && payload?.success === true && typeof payload?.result?.version === 'string';
      finish(ok, ok ? 'Editor version and scene read through MCP' : 'Editor read probe failed');
    }
  }
});
send({ jsonrpc: '2.0', id: 1, method: 'initialize', params: {
  protocolVersion: '2024-11-05', capabilities: {}, clientInfo: { name: 'sumi-mcp-smoke', version: '1.0.0' }
} });
