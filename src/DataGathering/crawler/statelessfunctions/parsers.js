export function extractProductId(url) {
  const match = url.match(/\/itm\/(?:.*?\/)?(\d{12,})/);
  return match ? match[1] : null;
}
