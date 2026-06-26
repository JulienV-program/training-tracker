window.maps = window.maps || {};

// Icônes par défaut Leaflet vendorisées localement (le CSS référence "images/..." relatif à leaflet.css)
L.Icon.Default.imagePath = 'leaflet/images/';

window.renderRouteMap = function (dataJson) {
    const points = JSON.parse(dataJson);
    const container = document.getElementById('routeMap');
    if (!container) return;

    if (window.maps.route) {
        window.maps.route.remove();
        window.maps.route = null;
    }

    if (!points || points.length === 0) {
        container.innerHTML = '<div class="d-flex align-items-center justify-content-center h-100 text-muted">Pas de données GPS pour cette séance.</div>';
        return;
    }

    const latlngs = points.map(p => [p.lat, p.lng]);

    const map = L.map(container);
    window.maps.route = map;

    L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
        attribution: '&copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a> contributors',
        maxZoom: 19
    }).addTo(map);

    const polyline = L.polyline(latlngs, { color: '#fc4c02', weight: 4 }).addTo(map);
    map.fitBounds(polyline.getBounds(), { padding: [20, 20] });

    L.marker(latlngs[0]).addTo(map).bindPopup('Départ');
    L.marker(latlngs[latlngs.length - 1]).addTo(map).bindPopup('Arrivée');
};
