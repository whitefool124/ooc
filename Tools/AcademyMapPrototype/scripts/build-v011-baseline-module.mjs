import { readFile, writeFile } from "node:fs/promises";

const sourceUrl = new URL("../data/academy-campus-v0.11.geojson", import.meta.url);
const targetUrl = new URL("../src/v011-baseline.generated.js", import.meta.url);
const baseline = JSON.parse(await readFile(sourceUrl, "utf8"));
await writeFile(targetUrl, `// Generated mechanically from data/academy-campus-v0.11.geojson. Do not hand-edit.\nexport default ${JSON.stringify(baseline)};\n`);
console.log(`Generated ${targetUrl.pathname} from the preserved v0.11 exchange file.`);
