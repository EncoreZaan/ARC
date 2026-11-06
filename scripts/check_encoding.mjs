import fs from 'fs';
import path from 'path';

const mojibakePatterns = [
  'Ã©', 'Ã¨', 'Ã ', 'Ã¢', 'Ã§', 'Ã®', 'Ã´', 'Ã»', 'Ã¹', 'Ãª',
  'â€”', 'â€¢', 'â†’', 'âœ“', 'â„¢', 'âš ', 'âš¡'
];

function checkFile(filePath) {
  const content = fs.readFileSync(filePath, 'utf8');
  let fileCount = 0;
  for (const p of mojibakePatterns) {
    const matches = content.split(p).length - 1;
    if (matches > 0) {
      fileCount += matches;
    }
  }
  if (fileCount > 0) {
    console.log(`⚠️ ${filePath}: ${fileCount} mojibake occurrences`);
  } else {
    console.log(`✅ ${filePath}: Clean UTF-8 (0 mojibake)`);
  }
}

for (const file of fs.readdirSync('docs')) {
  const full = path.join('docs', file);
  if (fs.statSync(full).isFile() && !file.endsWith('.bak')) {
    checkFile(full);
  }
}
