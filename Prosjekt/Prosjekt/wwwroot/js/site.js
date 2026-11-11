/// <summary>
/// Koder tekst fra brukere før den settes inn som HTML, for eksempel i kart-popuper.
/// </summary>
function escapeHtml(value) {
    return String(value ?? '')
        .replace(/&/g, '&amp;')
        .replace(/</g, '&lt;')
        .replace(/>/g, '&gt;')
        .replace(/"/g, '&quot;')
        .replace(/'/g, '&#39;');
}

/// <summary>
/// Gjør GeoJSON fra GetInformation om til en vanlig liste. Er geometrien et punkt, er det posisjonen.
/// Ellers er det et tegnet område, og posisjonen ligger i latitude/longitude.
/// </summary>
function fromGeoJson(geoJson) {
    if (!geoJson || !Array.isArray(geoJson.features)) return [];

    return geoJson.features.map(function (feature) {
        var item = Object.assign({}, feature.properties);
        var geometry = feature.geometry;

        if (geometry && geometry.type === 'Point') {
            /// <summary>
            /// GeoJSON har lengdegrad først, motsatt av Leaflet.
            /// </summary>
            item.longitude = geometry.coordinates[0];
            item.latitude = geometry.coordinates[1];
        } else if (geometry) {
            item.areaGeoJson = geometry;
        }

        return item;
    });
}

/// <summary>
/// Formatering av areal og lengde fra kartet. Brukes av flere views.
/// </summary>
function formatArea(squareMeters) {
    if (squareMeters === null || squareMeters === undefined) { return ''; }
    if (squareMeters < 10000) {
        return Math.round(squareMeters).toLocaleString('nb-NO') + ' m²';
    }
    if (squareMeters < 1000000) {
        return (squareMeters / 10000).toFixed(1).replace('.', ',') + ' ha';
    }
    return (squareMeters / 1000000).toFixed(2).replace('.', ',') + ' km²';
}

/// <summary>
/// Viser en lengde i meter som m eller km.
/// </summary>
function formatLength(meters) {
    if (meters === null || meters === undefined) { return ''; }
    if (meters < 1000) {
        return Math.round(meters).toLocaleString('nb-NO') + ' m';
    }
    return (meters / 1000).toFixed(1).replace('.', ',') + ' km';
}

/// <summary>
/// Tømmer de skjulte feltene for tegnet område og skjuler arealinfoen.
/// </summary>
function clearArea() {
    document.getElementById('AreaGeoJson').value = '';
    document.getElementById('AreaSquareMeters').value = '';
    document.getElementById('PerimeterMeters').value = '';
    document.getElementById('omradeInfo').classList.add('d-none');
}

/// <summary>
/// Lagrer et tegnet område i de skjulte feltene, som GeoJSON med areal og omkrets.
/// </summary>
function saveArea(layer) {

    var points = layer.getLatLngs()[0];

    var area = L.GeometryUtil.geodesicArea(points);

    var perimeter = 0;
    for (var i = 0; i < points.length; i++) {
        var next = points[(i + 1) % points.length];
        perimeter += points[i].distanceTo(next);
    }
    document.getElementById('AreaGeoJson').value = JSON.stringify(layer.toGeoJSON());
    document.getElementById('AreaSquareMeters').value = area.toFixed(2);
    document.getElementById('PerimeterMeters').value = perimeter.toFixed(2);

    document.getElementById('omradeAreal').textContent = formatArea(area);
    document.getElementById('omradeOmkrets').textContent = formatLength(perimeter);
    document.getElementById('omradeInfo').classList.remove('d-none');
}

/// <summary>
/// Satt av stedssøk. null = ingen geografisk begrensning.
/// </summary>
var placeBounds = null;
var radiusCenter = null;
var radiusMeters = 5000;
var radiusCircle = null;
var isPickingPoint = false;

/// <summary>
/// Filtrerer tabellradene og kartet samtidig, og returnerer filteret så stedssøk og radiussøk kan kjøre det.
/// layerRegistries: oppslag fra rad-id til kartlag. layerGroups: hvor lagene ligger (ellers selve kartet).
/// </summary>
function setupFilter(map, layerRegistries, layerGroups = []) {
    var searchField = document.getElementById('sokeFelt');
    var typeFilter = document.getElementById('typeFilter');
    if (!searchField || !typeFilter) return function () { };

    function applyFilter() {
        var text = searchField.value.toLowerCase().trim();
        var type = typeFilter.value;

        document.querySelectorAll('.table-row-clickable').forEach(function (row) {
            var lat = parseFloat(row.dataset.lat);
            var lng = parseFloat(row.dataset.lng);

            var matchesText = text === '' || row.dataset.sok.includes(text);
            var matchesType = type === '' || row.dataset.type === type;
            var matchesPlace = !placeBounds ||
                (lat >= placeBounds.minLat && lat <= placeBounds.maxLat &&
                    lng >= placeBounds.minLng && lng <= placeBounds.maxLng);
            var matchesRadius = !radiusCenter || radiusCenter.distanceTo(L.latLng(lat, lng)) <= radiusMeters;
            var visible = matchesText && matchesType && matchesPlace && matchesRadius;

            row.classList.toggle('d-none', !visible);

            layerRegistries.forEach(function (registry, index) {
                var layer = registry[row.dataset.id];
                if (!layer) return;
                var owner = layerGroups[index] || map;
                if (visible) { owner.addLayer(layer); } else { owner.removeLayer(layer); }
            });
        });
    }

    searchField.addEventListener('input', applyFilter);
    typeFilter.addEventListener('change', applyFilter);

    return applyFilter;
}

/// <summary>
/// Søker etter stedet i stedsfeltet når brukeren trykker Enter.
/// </summary>
function setupPlaceSearch(map, runFilter) {
    var field = document.getElementById('stedsFelt');
    if (!field) return;

    field.addEventListener('keydown', function (e) {
        if (e.key !== 'Enter') return;
        e.preventDefault();
        searchPlace(field.value.trim(), map, runFilter);
    });
}

/// <summary>
/// Slår opp stedet hos Nominatim, zoomer kartet dit og filtrerer på området. Tomt søk fjerner begrensningen.
/// </summary>
function searchPlace(query, map, runFilter)
{
    if (!query)
    {
        placeBounds = null;
        runFilter();
        return;
    }
    var url = 'https://nominatim.openstreetmap.org/search?format=jsonv2&limit=1&countrycodes=no&q='
        + encodeURIComponent(query);

    fetch(url, { headers: { 'Accept-Language': 'nb' } })
        .then(function (response) { return response.json(); })
        .then(function (results) {
            if (!results.length) {
                alert('Fant ikke stedet "' + query + '".');
                return;
            }

            /// <summary>
            /// boundingbox: [minLat, maxLat, minLng, maxLng] som tekst.
            /// </summary>
            var b = results[0].boundingbox;
            placeBounds = {
                minLat: parseFloat(b[0]), maxLat: parseFloat(b[1]),
                minLng: parseFloat(b[2]), maxLng: parseFloat(b[3])
            };

            map.fitBounds([
                [placeBounds.minLat, placeBounds.minLng],
                [placeBounds.maxLat, placeBounds.maxLng]
            ]);

            runFilter();
        });
}

/// <summary>
/// Etter «Velg punkt» blir neste klikk på kartet midten av radiussøket.
/// </summary>
function setupRadiusSearch(map, runFilter) {
    var button = document.getElementById('velgPunktBtn');
    var radiusField = document.getElementById('radiusFelt');
    if (!button || !radiusField) return;

    button.addEventListener('click', function () {
        if (radiusCenter) { resetRadius(map, runFilter); return; }

        isPickingPoint = !isPickingPoint;
        button.textContent = isPickingPoint ? 'Avbryt' : 'Velg punkt';
        map.getContainer().style.cursor = isPickingPoint ? 'crosshair' : '';
    });

    map.on('click', function (e) {
        if (!isPickingPoint) return;

        radiusCenter = e.latlng;
        radiusMeters = (parseFloat(radiusField.value) || 5) * 1000;
        isPickingPoint = false;
        button.textContent = 'fjern punkt';
        map.getContainer().style.cursor = '';

        drawRadius(map);
        runFilter();
    });

    radiusField.addEventListener('input', function () {
        if (!radiusCenter) return;
        radiusMeters = (parseFloat(radiusField.value) || 5) * 1000;
        drawRadius(map);
        runFilter();
    });
}

/// <summary>
/// Tegner sirkelen for radiussøket på nytt.
/// </summary>
function drawRadius(map) {
    if (radiusCircle) { map.removeLayer(radiusCircle); }
    radiusCircle = L.circle(radiusCenter, {
        radius: radiusMeters,
        color: '#0d6efd',
        weight: 2,
        fillOpacity: 0.08
    }).addTo(map);
}

/// <summary>
/// Fjerner radiussøket og filtrerer på nytt.
/// </summary>
function resetRadius(map, runFilter) {
    radiusCenter = null;
    isPickingPoint = false;
    if (radiusCircle) { map.removeLayer(radiusCircle); radiusCircle = null; }
    document.getElementById('velgPunktBtn').textContent = 'velg punkt';
    map.getContainer().style.cursor = '';
    runFilter();
}

/// <summary>
/// Erstatter onclick, onchange og onsubmit i HTML, som CSP-en blokkerer.
/// data-on-click="funksjon" og data-on-change="funksjon" kaller en global funksjon i siden, med data-arg som argument.
/// Returnerer funksjonen false, avbrytes klikket, slik som med return i onclick.
/// data-confirm="tekst" på et skjema spør før innsending. data-stop-click stopper klikket fra å nå raden rundt.
/// </summary>
(function () {
    function invoke(element, key, event) {
        var fn = window[element.dataset[key]];
        if (typeof fn !== 'function') return;

        var arg = element.dataset.arg;
        var result = arg === undefined ? fn() : fn(isNaN(arg) ? arg : Number(arg));
        if (result === false) event.preventDefault();
    }

    document.addEventListener('click', function (e) {
        var element = e.target.closest('[data-on-click]');
        if (element) invoke(element, 'onClick', e);
    });

    document.addEventListener('change', function (e) {
        var element = e.target.closest('[data-on-change]');
        if (element) invoke(element, 'onChange', e);
    });

    /// Viser popupen #confirmModal fra _Layout i stedet for nettleserens confirm().
    /// data-confirm-title og data-confirm-button kan endre tittel og knappetekst.
    /// Teksten settes med textContent, så navn i teksten kan ikke kjøres som HTML (XSS).
    var pendingForm = null;

    document.addEventListener('submit', function (e) {
        var form = e.target.closest('form[data-confirm]');
        if (!form) return;

        var modalElement = document.getElementById('confirmModal');
        if (!modalElement || typeof bootstrap === 'undefined') {
            if (!confirm(form.dataset.confirm)) e.preventDefault();
            return;
        }

        e.preventDefault();
        pendingForm = form;
        document.getElementById('confirmModalTitle').textContent = form.dataset.confirmTitle || 'Bekreft';
        document.getElementById('confirmModalText').textContent = form.dataset.confirm;
        document.getElementById('confirmModalButton').textContent = form.dataset.confirmButton || 'Slett';
        bootstrap.Modal.getOrCreateInstance(modalElement).show();
    });

    /// form.submit() utløser ikke submit-hendelsen, så popupen vises ikke på nytt
    document.addEventListener('click', function (e) {
        if (!e.target.closest('#confirmModalButton') || !pendingForm) return;
        var form = pendingForm;
        pendingForm = null;
        bootstrap.Modal.getOrCreateInstance(document.getElementById('confirmModal')).hide();
        form.submit();
    });

    /// <summary>
    /// Må ligge på selve elementet, ellers har klikket alt nådd raden.
    /// </summary>
    document.addEventListener('DOMContentLoaded', function () {
        document.querySelectorAll('[data-stop-click]').forEach(function (element) {
            element.addEventListener('click', function (e) { e.stopPropagation(); });
        });
    });
})();
