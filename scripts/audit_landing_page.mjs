import fs from 'fs';
import path from 'path';

const html = fs.readFileSync('docs/index.html', 'utf8');

console.log('--- 1. AUDIT ASSETS LOCAUX (src & href) ---');
const srcRegex = /(?:src|href)=["']([^"'#?]+(?:\?[^"']*)?)["']/g;
let match;
let missingCount = 0;
while ((match = srcRegex.exec(html)) !== null) {
  const fullRef = match[1];
  const cleanPath = fullRef.split('?')[0];
  if (
    !cleanPath.startsWith('http') &&
    !cleanPath.startsWith('mailto:') &&
    !cleanPath.startsWith('#') &&
    !cleanPath.startsWith('javascript:') &&
    !cleanPath.startsWith('data:')
  ) {
    const targetPath = path.join('docs', cleanPath);
    if (!fs.existsSync(targetPath)) {
      console.log('❌ FICHIER MANQUANT :', fullRef, '-> cherché à :', targetPath);
      missingCount++;
    }
  }
}
if (missingCount === 0) {
  console.log('✅ Tous les assets locaux référencés existent sur le disque !');
}

console.log('\n--- 2. AUDIT DES ANCRES INTERNES (#...) ---');
const anchorRegex = /href=["']#([a-zA-Z0-9_\-]+)["']/g;
const idRegex = /id=["']([a-zA-Z0-9_\-]+)["']/g;
const ids = new Set();
while ((match = idRegex.exec(html)) !== null) {
  ids.add(match[1]);
}

let brokenAnchors = 0;
while ((match = anchorRegex.exec(html)) !== null) {
  const targetId = match[1];
  if (!ids.has(targetId)) {
    console.log('❌ ANCRE CASSÉE : #' + targetId, '(aucun id="' + targetId + '" dans le DOM)');
    brokenAnchors++;
  }
}
if (brokenAnchors === 0) {
  console.log('✅ Tous les liens d\'ancres (#...) pointent vers des éléments réels du DOM !');
}

console.log('\n--- 3. AUDIT DES CATÉGORIES D\'OUTILS (index.html vs app.js) ---');
const appJs = fs.readFileSync('docs/app.js', 'utf8');
const buttonCatRegex = /data-cat=["']([a-zA-Z0-9_\-]+)["']/g;
const htmlCats = [];
while ((match = buttonCatRegex.exec(html)) !== null) {
  htmlCats.push(match[1]);
}

const matchTools = appJs.match(/const toolsData = (\[[\s\S]*?\n\]);/);
if (!matchTools) {
  throw new Error('Impossible de localiser toolsData dans app.js');
}
const toolsData = eval(matchTools[1]);
const appCats = new Set(toolsData.map(t => t.cat));

console.log('Catégories de filtres dans index.html :', htmlCats);
console.log('Catégories utilisées par les outils dans app.js :', [...appCats]);

for (const c of appCats) {
  if (c !== 'all' && !htmlCats.includes(c)) {
    console.log('⚠️ Catégorie présente dans app.js mais pas en bouton dans index.html :', c);
  }
}

console.log('\n--- 4. COMPTEURS ET TEXTES DE VERSION ---');
const count268Html = (html.match(/268/g) || []).length;
const count264Html = (html.match(/264/g) || []).length;
const countV310Html = (html.match(/v3\.1\.0|3\.1\.0/g) || []).length;
const countV300Html = (html.match(/v3\.0\.0|3\.0\.0/g) || []).length;
console.log('Occurrences de 268 dans index.html :', count268Html);
console.log('Occurrences résiduelles de 264 dans index.html :', count264Html);
console.log('Occurrences de 3.1.0 dans index.html :', countV310Html);
console.log('Occurrences résiduelles de 3.0.0 dans index.html :', countV300Html);

console.log('\n--- 5. DÉCOMPTE EXACT PAR CATÉGORIE DANS APP.JS ---');
const counts = {};
for (const t of toolsData) {
  counts[t.cat] = (counts[t.cat] || 0) + 1;
}
console.log(counts);
console.log('Total des outils dans app.js :', toolsData.length);

console.log('\n--- 6. COMPARAISON TOOLS_MANIFEST VS APP.JS ---');
const manifest = JSON.parse(fs.readFileSync('revit-mcp-server/tools_manifest.json', 'utf8'));
const manifestNames = new Set(manifest.tools.map(t => t.name));
manifestNames.add('revit_dispatch'); // The 268th tool injected in Rust

const appToolNames = new Set(toolsData.map(t => t.name));

console.log('Nombre dans manifest (+ dispatch) :', manifestNames.size);
console.log('Nombre dans app.js :', appToolNames.size);

let manifestDiffs = 0;
for (const name of manifestNames) {
  if (!appToolNames.has(name)) {
    console.log('⚠️ Outil dans manifest mais absent de app.js :', name);
    manifestDiffs++;
  }
}
for (const name of appToolNames) {
  if (!manifestNames.has(name)) {
    console.log('⚠️ Outil dans app.js mais absent de manifest :', name);
    manifestDiffs++;
  }
}
if (manifestDiffs === 0) {
  console.log('✅ Parité parfaite (100%) entre tools_manifest.json (+ revit_dispatch) et toolsData dans app.js !');
}
