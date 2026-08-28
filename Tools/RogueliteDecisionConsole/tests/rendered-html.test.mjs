import assert from "node:assert/strict";
import { readFile } from "node:fs/promises";
import test from "node:test";

async function render() {
  const workerUrl = new URL("../dist/server/index.js", import.meta.url);
  workerUrl.searchParams.set("test", `${process.pid}-${Date.now()}`);
  const { default: worker } = await import(workerUrl.href);

  return worker.fetch(
    new Request("http://localhost/", { headers: { accept: "text/html" } }),
    { ASSETS: { fetch: async () => new Response("Not found", { status: 404 }) } },
    { waitUntil() {}, passThroughOnException() {} },
  );
}

test("server-renders the OCC roguelite decision console", async () => {
  const response = await render();
  assert.equal(response.status, 200);
  assert.match(response.headers.get("content-type") ?? "", /^text\/html\b/i);

  const html = await response.text();
  assert.match(html, /OCC 肉鸽模式唯一决策台账/);
  assert.match(html, /集中管理 OCC 肉鸽模式的待决定事项/);
  assert.doesNotMatch(html, /codex-preview|Your site is taking shape|Building your site/i);
});

test("supports detailed explanations and free-form alternatives", async () => {
  const [page, css] = await Promise.all([
    readFile(new URL("../app/page.tsx", import.meta.url), "utf8"),
    readFile(new URL("../app/globals.css", import.meta.url), "utf8"),
  ]);

  assert.match(page, /01 \/ 这次具体决定/);
  assert.match(page, /02 \/ 为什么现在要定/);
  assert.match(page, /03 \/ 定完会约束什么/);
  assert.match(page, /填写你自己的方案/);
  assert.match(page, /function writeCustom/);
  assert.match(page, /optionId: "OTHER"/);
  assert.match(page, /自定义回答/);
  assert.match(page, /occ-roguelite-decision-drafts/);
  assert.match(css, /\.custom-option/);
  assert.match(css, /\.decision-explainer/);
});

