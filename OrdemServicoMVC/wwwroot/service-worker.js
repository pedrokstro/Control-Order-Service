const CACHE_VERSION = 'cos-pwa-v2.0.0';
const STATIC_CACHE = `static-${CACHE_VERSION}`;
const DYNAMIC_CACHE = `dynamic-${CACHE_VERSION}`;
const API_CACHE = `api-${CACHE_VERSION}`;
const OFFLINE_URL = '/offline.html';

const AUTH_BYPASS_PATHS = ['/account', '/login', '/logout', '/signin'];
const API_PATHS = ['/ordemservico', '/mensagem', '/relatorio', '/user'];

const PRECACHE_ASSETS = [
  '/',
  OFFLINE_URL,
  '/manifest.webmanifest',
  '/css/site.css',
  '/stylecss/custom.css',
  '/stylecss/ordem-info-styles.css',
  '/js/site.js',
  '/js/pwa.js',
  '/js/lazy-loader.js',
  '/lib/bootstrap/dist/css/bootstrap.min.css',
  '/lib/bootstrap/dist/js/bootstrap.bundle.min.js',
  '/lib/jquery/dist/jquery.min.js',
  '/images/quartetto-logo.png',
  '/images/pwa-icon-192.png',
  '/images/pwa-icon-512.png'
];

self.addEventListener('install', (event) => {
  event.waitUntil(
    caches.open(STATIC_CACHE)
      .then((cache) => cache.addAll(PRECACHE_ASSETS))
      .then(() => self.skipWaiting())
  );
});

self.addEventListener('activate', (event) => {
  event.waitUntil(
    caches.keys().then((keys) =>
      Promise.all(
        keys
          .filter((key) => ![STATIC_CACHE, DYNAMIC_CACHE, API_CACHE].includes(key))
          .map((key) => caches.delete(key))
      )
    ).then(() => self.clients.claim())
  );
});

self.addEventListener('message', (event) => {
  if (event?.data?.type === 'SKIP_WAITING') {
    self.skipWaiting();
  }
});

self.addEventListener('fetch', (event) => {
  const { request } = event;

  if (request.method !== 'GET') {
    return;
  }

  const url = new URL(request.url);
  const normalizedPath = url.pathname.toLowerCase();

  if (!url.origin.startsWith(self.location.origin) && request.mode !== 'navigate') {
    return;
  }

  if (AUTH_BYPASS_PATHS.some((path) => normalizedPath.startsWith(path))) {
    event.respondWith(fetch(request).catch(() => offlineFallback()));
    return;
  }

  if (request.mode === 'navigate') {
    event.respondWith(handlePageRequest(request));
    return;
  }

  if (API_PATHS.some((path) => normalizedPath.startsWith(path))) {
    event.respondWith(networkFirstApi(request));
    return;
  }

  if (isStaticAsset(normalizedPath)) {
    event.respondWith(cacheFirst(request));
    return;
  }

  if (url.origin === self.location.origin) {
    event.respondWith(staleWhileRevalidate(request));
  }
});

function handlePageRequest(request) {
  return fetch(request)
    .then((response) => cacheDynamic(request, response))
    .catch(async () => {
      const cache = await caches.open(STATIC_CACHE);
      const cachedResponse = await cache.match(request);
      return cachedResponse || cache.match(OFFLINE_URL) || offlineFallback();
    });
}

function networkFirstApi(request) {
  return fetch(request)
    .then((response) => cacheApi(request, response))
    .catch(async () => {
      const cache = await caches.open(API_CACHE);
      const cachedResponse = await cache.match(request);

      if (cachedResponse) {
        return cachedResponse;
      }

      return new Response(
        JSON.stringify({
          offline: true,
          message: 'Sem conexão com a internet. Mostrando dados em cache quando disponíveis.'
        }),
        {
          headers: { 'Content-Type': 'application/json' },
          status: 503
        }
      );
    });
}

function cacheFirst(request) {
  return caches.match(request).then((cachedResponse) => {
    if (cachedResponse) {
      return cachedResponse;
    }

    return fetch(request).then((response) => cacheDynamic(request, response));
  });
}

function staleWhileRevalidate(request) {
  return caches.match(request).then((cachedResponse) => {
    const fetchPromise = fetch(request).then((response) => cacheDynamic(request, response));
    return cachedResponse || fetchPromise;
  });
}

function cacheDynamic(request, response) {
  if (!response || response.status !== 200 || response.type !== 'basic') {
    return response;
  }

  const copy = response.clone();
  caches.open(DYNAMIC_CACHE).then((cache) => cache.put(request, copy));
  return response;
}

function cacheApi(request, response) {
  if (!response || response.status !== 200) {
    return response;
  }

  const copy = response.clone();
  caches.open(API_CACHE).then((cache) => cache.put(request, copy));
  return response;
}

function isStaticAsset(pathname) {
  return (
    pathname.endsWith('.css') ||
    pathname.endsWith('.js') ||
    pathname.endsWith('.png') ||
    pathname.endsWith('.jpg') ||
    pathname.endsWith('.jpeg') ||
    pathname.endsWith('.svg') ||
    pathname.endsWith('.woff2') ||
    pathname.endsWith('.woff') ||
    pathname.endsWith('.ttf')
  );
}

function offlineFallback() {
  return new Response(
    '<html><body><h1>Você está offline</h1><p>Tente novamente quando estiver conectado.</p></body></html>',
    { headers: { 'Content-Type': 'text/html' } }
  );
}
