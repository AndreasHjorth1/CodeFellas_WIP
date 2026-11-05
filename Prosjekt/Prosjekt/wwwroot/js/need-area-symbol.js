/// <summary>
/// Viser behovets symbol inne i et tegnet område, og flytter det når området endres.
/// </summary>
window.createNeedAreaSymbol = function (map) {
    let marker;
    /// <summary>
    /// Finner et punkt inne i området. Midtpunktet kan havne utenfor når området er buet,
    /// og da brukes midten av den bredeste strekningen blant 12 breddegrader.
    /// </summary>
    function centerInside(polygon) {
        const center = polygon.getCenter();
        const ring = polygon.getLatLngs()[0];
        /// <summary>
        /// Hvor ytterkanten krysser breddegraden. To og to kryssinger avgrenser en strekning inne i området.
        /// </summary>
        function intervals(lat) {
            const xs = [];
            for (let i = 0, j = ring.length - 1; i < ring.length; j = i++) {
                const a = ring[j], b = ring[i];
                if ((a.lat > lat) !== (b.lat > lat))
                    xs.push(a.lng + (lat - a.lat) * (b.lng - a.lng) / (b.lat - a.lat));
            }
            return xs.sort((a,b) => a-b);
        }
        const xs = intervals(center.lat);
        for (let i = 0; i + 1 < xs.length; i += 2)
            if (center.lng > xs[i] && center.lng < xs[i+1]) return center;
        const bounds = polygon.getBounds();
        let best = null, width = 0;
        for (let row = 0; row < 12; row++) {
            const lat = bounds.getSouth() + (row + .5) * (bounds.getNorth() - bounds.getSouth()) / 12;
            const edges = intervals(lat);
            for (let i = 0; i + 1 < edges.length; i += 2) {
                if (edges[i+1] - edges[i] <= width) continue;
                width = edges[i+1] - edges[i];
                best = L.latLng(lat, (edges[i] + edges[i+1]) / 2);
            }
        }
        return best || center;
    }
    return {
        update(polygon, icon) {
            if (!polygon) {
                if (marker) marker.remove();
                marker = null;
                return null;
            }
            const center = centerInside(polygon);
            if (marker) marker.setLatLng(center).setIcon(icon);
            else marker = L.marker(center, {icon, interactive:false, keyboard:false}).addTo(map);
            return center;
        }
    };
};
