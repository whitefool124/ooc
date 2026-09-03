"use client";

import { useEffect, useMemo, useState } from "react";
import { conflicts, decisions, frozenRules, type Decision } from "./decision-data";

type View = "open" | "frozen" | "conflicts";
type Draft = { optionId?: string; customText?: string };
type Drafts = Record<string, Draft>;

const domainOrder = ["全部领域", "战斗结算", "装备系统", "法术与构筑", "奖励与内容池", "资源与时间", "学院第一阶段", "阶段转入"];

export default function Home() {
  const [view, setView] = useState<View>("open");
  const [domain, setDomain] = useState("全部领域");
  const [query, setQuery] = useState("");
  const [selected, setSelected] = useState<Decision | null>(null);
  const [drafts, setDrafts] = useState<Drafts>({});
  const [copied, setCopied] = useState(false);

  useEffect(() => {
    let restored: Drafts = {};
    try {
      const saved = JSON.parse(localStorage.getItem("occ-roguelite-decision-drafts") ?? "{}") as Record<string, string | Draft>;
      restored = Object.fromEntries(Object.entries(saved).map(([id, draft]) => [id, typeof draft === "string" ? { optionId: draft } : draft]));
    } catch { /* 损坏草稿按空状态恢复。 */ }
    const frame = window.requestAnimationFrame(() => setDrafts(restored));
    return () => window.cancelAnimationFrame(frame);
  }, []);

  const visible = useMemo(() => {
    const needle = query.trim().toLowerCase();
    return decisions
      .filter((item) => domain === "全部领域" || item.domain === domain)
      .filter((item) => !needle || `${item.id}${item.domain}${item.title}${item.question}${item.why}`.toLowerCase().includes(needle))
      .sort((a, b) => a.order - b.order);
  }, [domain, query]);

  const domainCounts = useMemo(() => Object.fromEntries(domainOrder.map((item) => [item, item === "全部领域" ? decisions.length : decisions.filter((decision) => decision.domain === item).length])), []);
  const currentDecision = decisions.find((item) => item.status === "当前决策");
  const draftedCount = Object.values(drafts).filter((draft) => draft.optionId && (draft.optionId !== "OTHER" || draft.customText?.trim())).length;

  function choose(decisionId: string, optionId: string) {
    const next = { ...drafts, [decisionId]: { ...drafts[decisionId], optionId } };
    setDrafts(next);
    localStorage.setItem("occ-roguelite-decision-drafts", JSON.stringify(next));
  }

  function writeCustom(decisionId: string, customText: string) {
    const nextDraft = customText.trim()
      ? { ...drafts[decisionId], optionId: "OTHER", customText }
      : { ...drafts[decisionId], optionId: undefined, customText: "" };
    const next = { ...drafts, [decisionId]: nextDraft };
    setDrafts(next);
    localStorage.setItem("occ-roguelite-decision-drafts", JSON.stringify(next));
  }

  function exportMarkdown() {
    const rows = decisions
      .filter((item) => drafts[item.id]?.optionId && (drafts[item.id].optionId !== "OTHER" || drafts[item.id].customText?.trim()))
      .map((item) => {
        const draft = drafts[item.id];
        if (draft.optionId === "OTHER" && draft.customText?.trim()) {
          return `## ${item.id} · ${item.title}\n\n- 草案选择：其他方案\n- 自定义回答：${draft.customText.trim()}\n- 冻结条件：${item.freezeWhen}`;
        }
        const option = item.options.find((candidate) => candidate.id === draft.optionId);
        return `## ${item.id} · ${item.title}\n\n- 草案选择：${option?.id}）${option?.label}\n- 说明：${option?.description}\n- 冻结条件：${item.freezeWhen}`;
      });
    const text = `# OCC 肉鸽模式决策草案\n\n> 本文件仅是页面导出的草案，不等于策划冻结。\n\n${rows.join("\n\n") || "尚未选择任何草案。"}`;
    const blob = new Blob([text], { type: "text/markdown;charset=utf-8" });
    const link = document.createElement("a");
    link.href = URL.createObjectURL(blob);
    link.download = "OCC_肉鸽模式决策草案.md";
    link.click();
    URL.revokeObjectURL(link.href);
  }

  async function copyId(id: string) {
    await navigator.clipboard?.writeText(id);
    setCopied(true);
    window.setTimeout(() => setCopied(false), 1200);
  }

  return (
    <main className="shell">
      <header className="masthead">
        <div className="brand-mark" aria-hidden="true">OCC</div>
        <div className="brand-copy"><span>ROGUELITE DESIGN AUTHORITY</span><strong>肉鸽模式 · 唯一决策台账</strong></div>
        <nav className="top-tabs" aria-label="台账视图">
          <button className={view === "open" ? "active" : ""} onClick={() => setView("open")}>待决定 <b>{decisions.length}</b></button>
          <button className={view === "frozen" ? "active" : ""} onClick={() => setView("frozen")}>已冻结 <b>{frozenRules.length}</b></button>
          <button className={view === "conflicts" ? "active" : ""} onClick={() => setView("conflicts")}>待同步 <b>{conflicts.length}</b></button>
        </nav>
        <div className="baseline-pill"><i /> 策划基线 v0.1</div>
      </header>

      <section className="hero">
        <p className="eyebrow">DECISION CONTROL / ROGUELITE ONLY / 2026.08.15</p>
        <h1>每个问题，只有一个<br /><em>权威答案。</em></h1>
        <div className="hero-side">
          <p>集中登记尚未明确的肉鸽模式产品问题。页面选择只保存为本机草案；只有同步权威策划、标记被取代条目并更新待办，才算正式冻结。</p>
          <button className="export-button" onClick={exportMarkdown}>导出已选草案 <span>↓</span></button>
        </div>
        <div className="summary-grid">
          <article><b>{String(decisions.length).padStart(2, "0")}</b><span>需要产品决定</span></article>
          <article><b>{currentDecision ? "01" : "00"}</b><span>{currentDecision ? "当前只处理一项" : "当前无待回答"}</span></article>
          <article><b>{String(draftedCount).padStart(2, "0")}</b><span>本机草案选择</span></article>
          <article className="warning-stat"><b>{String(conflicts.length).padStart(2, "0")}</b><span>已有决定待同步</span></article>
        </div>
      </section>

      {view === "open" && (
        <section className="workspace">
          <aside className="rail">
            <p>决策域</p>
            {domainOrder.map((item) => <button key={item} className={domain === item ? "active" : ""} onClick={() => setDomain(item)}><span>{item}</span><b>{String(domainCounts[item]).padStart(2, "0")}</b></button>)}
            <div className="rule-note"><span>唯一性规则</span><p>一个稳定 ID 对应一个问题。新决定冻结后，必须登记权威文件、被取代条目、依赖项和验证条件。</p></div>
          </aside>

          <div className="decision-panel">
            <div className="panel-head">
              <div><p>OPEN DECISIONS</p><h2>{domain === "全部领域" ? "全部待决定事项" : domain}</h2></div>
              <label className="search"><span>⌕</span><input value={query} onChange={(event) => setQuery(event.target.value)} placeholder="搜索编号、问题或领域" aria-label="搜索决策" /></label>
            </div>
            <div className="current-banner"><span>{currentDecision ? "当前只处理" : "当前状态"}</span><strong>{currentDecision ? `${currentDecision.id} · ${currentDecision.title}` : "没有待产品回答的问题"}</strong><i>{currentDecision ? "其余问题保持排队，不跨门禁" : "子系统细节按推荐方案推进；新上位分歧出现时再登记"}</i></div>
            <div className="decision-list">
              {visible.map((item, index) => (
                <button className={`decision-card ${item.status === "当前决策" ? "priority" : ""}`} key={item.id} onClick={() => setSelected(item)}>
                  <span className="sequence">{String(index + 1).padStart(2, "0")}</span>
                  <div className="card-main">
                    <div className="card-meta"><code>{item.id}</code><span>{item.domain}</span><span className={`status status-${item.status}`}>{item.status}</span>{drafts[item.id]?.optionId && <span className="draft-tag">{drafts[item.id].optionId === "OTHER" ? "已填写其他方案" : `已选草案 ${drafts[item.id].optionId}`}</span>}</div>
                    <h3>{item.title}</h3><p><strong>需要决定：</strong>{item.question}</p><p className="card-context"><strong>为什么重要：</strong>{item.why}</p>
                    <div className="card-foot"><small className={`impact impact-${item.impact}`}>{item.impact}影响</small><small>{item.dependsOn.length ? `依赖 ${item.dependsOn.join("、")}` : "无前置依赖"}</small></div>
                  </div>
                  <span className="arrow">↗</span>
                </button>
              ))}
              {!visible.length && <div className="empty-state">没有匹配的待决定事项。</div>}
            </div>
          </div>
        </section>
      )}

      {view === "frozen" && (
        <section className="registry-page">
          <div className="registry-head"><p>FROZEN BASELINE</p><h2>已经唯一化的肉鸽规则</h2><span>这些规则不应再次作为开放问题出现；需要改动时必须新建变更决策并明确取代关系。</span></div>
          <div className="frozen-grid">{frozenRules.map((rule, index) => <article key={rule}><code>FIXED-{String(index + 1).padStart(3, "0")}</code><b>已冻结</b><p>{rule}</p></article>)}</div>
        </section>
      )}

      {view === "conflicts" && (
        <section className="registry-page">
          <div className="registry-head"><p>SYNC DEBT</p><h2>已有答案，但下游尚未统一</h2><span>这些项目通常不需要再次做产品决定；应按已冻结权威来源完成文档或实现迁移。</span></div>
          <div className="conflict-list">{conflicts.map((item) => <article key={item.id}><div><code>{item.id}</code><b>{item.severity}</b></div><h3>{item.title}</h3><p>{item.detail}</p><span>动作：同步，不重新决策</span></article>)}</div>
        </section>
      )}

      {selected && view === "open" && (
        <div className="drawer-backdrop" role="presentation" onMouseDown={(event) => { if (event.target === event.currentTarget) setSelected(null); }}>
          <aside className="drawer" role="dialog" aria-modal="true" aria-labelledby="decision-title">
            <div className="drawer-top"><div><code>{selected.id}</code><span>{selected.domain} · {selected.impact}影响</span></div><button onClick={() => setSelected(null)} aria-label="关闭详情">×</button></div>
            <div className="drawer-scroll">
              <p className="drawer-kicker">{selected.status}</p><h2 id="decision-title">{selected.title}</h2>
              <section className="decision-explainer">
                <div><span>01 / 这次具体决定</span><p className="lead">{selected.question}</p></div>
                <div><span>02 / 为什么现在要定</span><p>{selected.why}</p></div>
                <div><span>03 / 定完会约束什么</span><p>{selected.downstream.join("、")}。冻结后，下游内容必须按同一个答案同步，不能再各自采用不同解释。</p></div>
              </section>
              <section className="detail-block"><h3>可选方案</h3><div className="option-list">{selected.options.map((option) => (
                <button key={option.id} className={drafts[selected.id]?.optionId === option.id ? "selected" : ""} onClick={() => choose(selected.id, option.id)}>
                  <b>{option.id}</b><div><strong>{option.label}{selected.options[0].id === option.id && <em>建议</em>}</strong><p>{option.description}</p></div><i>{drafts[selected.id]?.optionId === option.id ? "●" : "○"}</i>
                </button>
              ))}</div>
                <label className={`custom-option ${drafts[selected.id]?.optionId === "OTHER" ? "selected" : ""}`}>
                  <span className="custom-id">其他</span>
                  <span className="custom-copy"><strong>填写你自己的方案</strong><small>现有选项都不准确时，直接写完整规则、例外或你想要的体验。输入后会自动采用此方案。</small></span>
                  <textarea value={drafts[selected.id]?.customText ?? ""} onChange={(event) => writeCustom(selected.id, event.target.value)} placeholder="请写出你希望采用的完整规则；如果有适用条件、例外或数值边界，也请一起说明。" rows={4} aria-label={`${selected.title}的其他方案`} />
                </label>
                <small>预设选项和其他方案都只保存为本机草案，不会自动修改策划文件。</small>
              </section>
              <section className="recommendation"><span>推荐方向与理由</span><p>{selected.recommendation}</p></section>
              <section className="detail-grid"><div><h3>前置依赖</h3><p>{selected.dependsOn.length ? selected.dependsOn.join("、") : "无，可立即决定"}</p></div><div><h3>影响下游</h3><p>{selected.downstream.join("、")}</p></div></section>
              <section className="detail-block"><h3>权威证据</h3><ul>{selected.sources.map((source) => <li key={source}>{source}</li>)}</ul></section>
              <section className="freeze-box"><h3>冻结完成条件</h3><p>{selected.freezeWhen}</p></section>
            </div>
            <div className="drawer-footer"><button onClick={() => copyId(selected.id)}>{copied ? "已复制" : "复制决策 ID"}</button><button className="next-button" onClick={() => { const next = decisions.find((item) => item.order > selected.order); setSelected(next ?? null); }}>查看下一项 →</button></div>
          </aside>
        </div>
      )}
    </main>
  );
}
