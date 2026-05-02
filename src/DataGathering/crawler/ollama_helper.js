import fs from 'fs';
import path from 'path';
import { fileURLToPath } from 'url';

const __dirname = path.dirname(fileURLToPath(import.meta.url));

const OLLAMA_URL = 'http://192.168.0.151:11434/api/generate';
const MODEL = 'llama3.2:1b';
const CACHE_FILE = path.join(__dirname, 'output', 'ollama_cache.json');

let cache = new Map();
if (fs.existsSync(CACHE_FILE)) {
  try {
    const data = JSON.parse(fs.readFileSync(CACHE_FILE, 'utf8'));
    cache = new Map(Object.entries(data));
  } catch (e) {}
}

const saveCache = () => {
  fs.writeFileSync(CACHE_FILE, JSON.stringify(Object.fromEntries(cache)));
};

const queryOllama = async (prompt) => {
  try {
    const response = await fetch(OLLAMA_URL, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({
        model: MODEL,
        prompt: prompt,
        stream: false,
        options: { temperature: 0.3 }
      })
    });

    if (!response.ok) throw new Error(`HTTP ${response.status}`);

    const data = await response.json();
    return data.response?.trim() || '';
  } catch (e) {
    console.log(`  Ollama error: ${e.message}`);
    return null;
  }
};

export const selectSuggestions = async (query, suggestions) => {
  if (suggestions.length === 0) return [];

  const cacheKey = `${query}:${suggestions.sort().join(',')}`;
  if (cache.has(cacheKey)) {
    return cache.get(cacheKey);
  }

  const prompt = `Given the search query "${query}", which of these eBay search suggestions are most relevant for finding printable products (t-shirts, mugs, posters, stickers, etc)?

Suggestions: ${suggestions.join(', ')}

Return ONLY a comma-separated list of the most relevant suggestions. If none are relevant, return "none".`;

  const response = await queryOllama(prompt);
  if (!response || response.toLowerCase() === 'none') return [];

  const selected = response
    .split(',')
    .map(s => s.trim().toLowerCase())
    .filter(s => suggestions.map(s2 => s2.toLowerCase()).includes(s));

  cache.set(cacheKey, selected);
  saveCache();

  return selected;
};
