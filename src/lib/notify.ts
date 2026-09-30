import { isTauri } from './backend';

let granted: boolean | null = null;

/** Desktop notification (Windows toast). Silent no-op in the browser preview. */
export async function notify(title: string, body: string): Promise<void> {
  if (!isTauri) return;
  try {
    const n = await import('@tauri-apps/plugin-notification');
    if (granted === null) granted = await n.isPermissionGranted();
    if (!granted) granted = (await n.requestPermission()) === 'granted';
    if (granted) n.sendNotification({ title, body });
  } catch {
    /* notifications are optional */
  }
}
