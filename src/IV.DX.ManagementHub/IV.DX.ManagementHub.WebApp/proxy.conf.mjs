// Dev-server proxy. Runs in Node inside `ng serve` and never ships to production.
//
// Besides forwarding /api to the ASP.NET host, it attaches a DX service token to
// every proxied request. The real user login is not ported yet, and keeping the
// service key here — rather than in the Angular app — means no credential ever
// reaches the browser and there is nothing to rip out of the client later.
import { request } from 'node:https';

const target = process.env['MH_API_URL'] ?? 'https://localhost:7097';
const serviceKey = process.env['MH_SERVICE_KEY'] ?? 'mh-local-service-key';

// The ASP.NET dev certificate is not in the OpenSSL trust store on Linux.
const insecure = { rejectUnauthorized: false };

let accessToken = null;
let refreshTimer = null;

function fetchServiceToken() {
  return new Promise((resolve, reject) => {
    const body = JSON.stringify({ ServiceKey: serviceKey });
    const url = new URL('/api/service-auth/token', target);

    const req = request(
      url,
      {
        method: 'POST',
        headers: { 'content-type': 'application/json', 'content-length': Buffer.byteLength(body) },
        ...insecure,
      },
      (res) => {
        let raw = '';
        res.on('data', (chunk) => (raw += chunk));
        res.on('end', () => {
          if (res.statusCode !== 200) {
            reject(new Error(`service-auth/token returned HTTP ${res.statusCode}`));
            return;
          }
          try {
            resolve(JSON.parse(raw).accessToken);
          } catch {
            reject(new Error('service-auth/token returned a malformed body'));
          }
        });
      },
    );

    req.on('error', reject);
    req.end(body);
  });
}

async function refreshToken() {
  accessToken = await fetchServiceToken();

  // Tokens live 30 minutes (AccessTokenLifetimeMinutes); renew well before that.
  clearTimeout(refreshTimer);
  refreshTimer = setTimeout(() => void refreshToken().catch(warn), 20 * 60 * 1000);
  refreshTimer.unref();
}

function warn(error) {
  console.warn(`[proxy] could not obtain a DX service token from ${target}: ${error.message}`);
  console.warn('[proxy] /api requests will be forwarded without authorization (expect HTTP 401).');
}

// Best effort at startup: `ng serve` must not fail just because the backend is down.
await refreshToken().catch(warn);

export default {
  '/api': {
    target,
    secure: false,
    changeOrigin: true,
    configure: (proxy) => {
      proxy.on('proxyReq', (proxyReq) => {
        if (accessToken) {
          proxyReq.setHeader('authorization', `Bearer ${accessToken}`);
        }
      });

      // A rejected token usually means it expired early; renew for the next request.
      proxy.on('proxyRes', (proxyRes) => {
        if (proxyRes.statusCode === 401) {
          void refreshToken().catch(warn);
        }
      });
    },
  },
};
