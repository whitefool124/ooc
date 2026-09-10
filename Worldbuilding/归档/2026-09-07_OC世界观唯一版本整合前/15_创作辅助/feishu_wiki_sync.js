const fs = require('fs');
const path = require('path');
const os = require('os');

function stripJsoncComments(text) {
  return text.replace(/\/\/.*$/gm, '').replace(/\/\*[\s\S]*?\*\//g, '');
}

function interpolateEnvVars(text) {
  return text.replace(/\$\{(\w+)\}/g, (_match, varName) => process.env[varName] || '');
}

function loadLocalBridgeConfig() {
  const candidates = [
    path.resolve('opencode-lark.jsonc'),
    path.resolve('opencode-lark.json'),
    path.resolve('opencode-im-bridge.jsonc'),
    path.resolve('opencode-im-bridge.json'),
    path.resolve('opencode-feishu.jsonc'),
    path.resolve('opencode-feishu.json'),
  ];

  for (const file of candidates) {
    if (!fs.existsSync(file)) continue;
    const raw = fs.readFileSync(file, 'utf8');
    const parsed = JSON.parse(stripJsoncComments(interpolateEnvVars(raw)));
    if (parsed?.feishu?.appId && parsed?.feishu?.appSecret) {
      return {
        appId: parsed.feishu.appId,
        appSecret: parsed.feishu.appSecret,
        source: file,
      };
    }
  }

  return null;
}

function loadBridgeEnvConfig() {
  const configDir = path.join(os.homedir(), '.config', 'opencode-lark');
  if (!fs.existsSync(configDir)) return null;

  const envFiles = fs.readdirSync(configDir)
    .filter((name) => name.startsWith('.env.') && name.length > 5)
    .map((name) => path.join(configDir, name));

  for (const file of envFiles) {
    const raw = fs.readFileSync(file, 'utf8');
    const lines = raw.split(/\r?\n/);
    const values = {};
    for (const line of lines) {
      const trimmed = line.trim();
      if (!trimmed || trimmed.startsWith('#')) continue;
      const idx = trimmed.indexOf('=');
      if (idx === -1) continue;
      const key = trimmed.slice(0, idx).trim();
      let value = trimmed.slice(idx + 1).trim();
      if ((value.startsWith('"') && value.endsWith('"')) || (value.startsWith("'") && value.endsWith("'"))) {
        value = value.slice(1, -1);
      }
      values[key] = value;
    }

    const appId = values.FEISHU_APP_ID || values.LARK_APP_ID;
    const appSecret = values.FEISHU_APP_SECRET || values.LARK_APP_SECRET;
    if (appId && appSecret) {
      return { appId, appSecret, source: file };
    }
  }

  return null;
}

const LOCAL_CONFIG = loadLocalBridgeConfig();
const BRIDGE_ENV_CONFIG = loadBridgeEnvConfig();
const FEISHU_APP_ID = process.env.FEISHU_APP_ID || process.env.LARK_APP_ID || LOCAL_CONFIG?.appId || BRIDGE_ENV_CONFIG?.appId;
const FEISHU_APP_SECRET = process.env.FEISHU_APP_SECRET || process.env.LARK_APP_SECRET || LOCAL_CONFIG?.appSecret || BRIDGE_ENV_CONFIG?.appSecret;

function assertEnv() {
  if (!FEISHU_APP_ID || !FEISHU_APP_SECRET) {
    throw new Error('Missing Feishu credentials. Provide FEISHU_APP_ID/FEISHU_APP_SECRET, LARK_APP_ID/LARK_APP_SECRET, or a local opencode-lark/opencode-im-bridge config file.');
  }
}

function parseArgs() {
  const args = process.argv.slice(2);
  const out = {};
  for (let i = 0; i < args.length; i += 1) {
    const arg = args[i];
    if (arg.startsWith('--')) {
      out[arg.slice(2)] = args[i + 1];
      i += 1;
    }
  }
  return out;
}

function splitMarkdownByHeadings(markdown) {
  const lines = markdown.split(/\r?\n/);
  const chunks = [];
  let current = [];

  for (const line of lines) {
    if (/^#{1,2}\s+/.test(line) && current.length) {
      chunks.push(current.join('\n').trim());
      current = [line];
    } else {
      current.push(line);
    }
  }

  if (current.length) {
    chunks.push(current.join('\n').trim());
  }

  return chunks.filter(Boolean);
}

async function api(path, options = {}) {
  const resp = await fetch(`https://open.feishu.cn/open-apis${path}`, options);
  const data = await resp.json();
  if (!resp.ok || (typeof data.code !== 'undefined' && data.code !== 0)) {
    throw new Error(`API ${path} failed: ${resp.status} ${JSON.stringify(data)}`);
  }
  return data;
}

async function getTenantToken() {
  const data = await api('/auth/v3/tenant_access_token/internal', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json; charset=utf-8' },
    body: JSON.stringify({
      app_id: FEISHU_APP_ID,
      app_secret: FEISHU_APP_SECRET,
    }),
  });
  return data.tenant_access_token;
}

function authHeaders(token) {
  return {
    Authorization: `Bearer ${token}`,
    'Content-Type': 'application/json; charset=utf-8',
  };
}

function extractWikiToken(input) {
  if (/^https?:\/\//.test(input)) {
    const match = input.match(/\/wiki\/([A-Za-z0-9]+)/);
    if (!match) throw new Error('Cannot extract wiki token from URL.');
    return match[1];
  }
  return input;
}

async function resolveWikiToDocx(token, wikiToken) {
  const data = await api(`/wiki/v2/spaces/get_node?token=${encodeURIComponent(wikiToken)}`, {
    method: 'GET',
    headers: authHeaders(token),
  });

  const node = data.data?.node;
  if (!node) throw new Error('Wiki node not found in response.');
  if (node.obj_type !== 'docx') {
    throw new Error(`Target wiki node is not docx, got: ${node.obj_type}`);
  }
  return {
    wikiToken,
    nodeToken: node.node_token,
    docToken: node.obj_token,
    title: node.title,
  };
}

async function listBlocks(token, docToken) {
  let pageToken = '';
  const items = [];
  do {
    const path = `/docx/v1/documents/${docToken}/blocks?page_size=500${pageToken ? `&page_token=${encodeURIComponent(pageToken)}` : ''}`;
    const data = await api(path, { method: 'GET', headers: authHeaders(token) });
    items.push(...(data.data?.items || []));
    pageToken = data.data?.page_token || '';
  } while (pageToken);
  return items;
}

async function deleteNonRootBlocks(token, docToken) {
  const blocks = await listBlocks(token, docToken);
  const deletable = blocks
    .filter((b) => b.block_id && b.block_id !== docToken)
    .sort((a, b) => (b.parent_id === a.block_id ? -1 : 0));

  for (const block of deletable) {
    try {
      await api(`/docx/v1/documents/${docToken}/blocks/${block.block_id}`, {
        method: 'DELETE',
        headers: authHeaders(token),
      });
      await new Promise((r) => setTimeout(r, 350));
    } catch (err) {
      // Ignore repeated child deletions after parent removal.
    }
  }
}

function stripReadOnlyFields(obj) {
  if (Array.isArray(obj)) return obj.map(stripReadOnlyFields);
  if (obj && typeof obj === 'object') {
    const next = {};
    for (const [k, v] of Object.entries(obj)) {
      if (k === 'merge_info') continue;
      next[k] = stripReadOnlyFields(v);
    }
    return next;
  }
  return obj;
}

async function convertMarkdown(token, markdown) {
  const data = await api('/docx/v1/documents/blocks/convert', {
    method: 'POST',
    headers: authHeaders(token),
    body: JSON.stringify({
      content_type: 'markdown',
      content: markdown,
    }),
  });
  const converted = data.data || {};
  return {
    children_id: converted.first_level_block_ids || [],
    descendants: stripReadOnlyFields(converted.blocks || []),
  };
}

async function insertChunk(token, docToken, converted) {
  if (!converted.children_id.length) return;
  await api(`/docx/v1/documents/${docToken}/blocks/${docToken}/descendant`, {
    method: 'POST',
    headers: authHeaders(token),
    body: JSON.stringify({
      index: -1,
      children_id: converted.children_id,
      descendants: converted.descendants,
    }),
  });
}

function splitLargeChunk(markdown) {
  const lines = markdown.split(/\r?\n/);
  const chunks = [];
  let current = [];
  let currentLen = 0;
  const maxChars = 4500;

  for (const line of lines) {
    const lineLen = line.length + 1;
    const isBoundary = /^#{1,6}\s+/.test(line) || /^[-*+]\s+/.test(line) || /^\d+\.\s+/.test(line) || line.trim() === '';

    if (current.length && currentLen + lineLen > maxChars && isBoundary) {
      chunks.push(current.join('\n').trim());
      current = [line];
      currentLen = lineLen;
    } else {
      current.push(line);
      currentLen += lineLen;
    }
  }

  if (current.length) chunks.push(current.join('\n').trim());
  return chunks.filter(Boolean);
}

async function syncToWiki({ wiki, source }) {
  assertEnv();
  const markdown = fs.readFileSync(source, 'utf8');
  const token = await getTenantToken();
  const resolved = await resolveWikiToDocx(token, extractWikiToken(wiki));
  const topLevelChunks = splitMarkdownByHeadings(markdown);
  const chunks = topLevelChunks.flatMap((chunk) => splitLargeChunk(chunk));

  await deleteNonRootBlocks(token, resolved.docToken);

  for (const chunk of chunks) {
    const converted = await convertMarkdown(token, chunk);
    await insertChunk(token, resolved.docToken, converted);
    await new Promise((r) => setTimeout(r, 400));
  }

  return resolved;
}

async function main() {
  const args = parseArgs();
  if (!args.wiki || !args.source) {
    throw new Error('Usage: node feishu_wiki_sync.js --wiki <wiki_url_or_token> --source <markdown_path>');
  }
  const result = await syncToWiki(args);
  console.log(JSON.stringify({ ok: true, credentialSource: LOCAL_CONFIG?.source || BRIDGE_ENV_CONFIG?.source || 'env', ...result }, null, 2));
}

main().catch((err) => {
  console.error(err.message || err);
  process.exit(1);
});
