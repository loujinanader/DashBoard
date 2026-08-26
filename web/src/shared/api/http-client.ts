/** No auth on this API by design — the dashboard is open to anyone on the network. */
export async function apiRequest<T>(path: string, options: { method?: string } = {}): Promise<T> {
  const res = await fetch(path, { method: options.method ?? 'GET' });
  if (!res.ok) {
    const body = await res.json().catch(() => null);
    const message = (body && typeof body.message === 'string' ? body.message : null) ?? `Request failed (${res.status})`;
    throw new Error(message);
  }
  if (res.status === 204) return undefined as T;
  return res.json();
}
