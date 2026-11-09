/// Koder tekst fra brukere før den settes inn som HTML, for eksempel i kart-popuper.
function escapeHtml(verdi) {
    return String(verdi ?? '')
        .replace(/&/g, '&amp;')
        .replace(/</g, '&lt;')
        .replace(/>/g, '&gt;')
        .replace(/"/g, '&quot;')
        .replace(/'/g, '&#39;');
}

/// <summary>
/// Formatering av areal og lengde fra kartet. Brukes av flere views.
/// </summary>
function formaterAreal(m2) {
    if (m2 === null || m2 === undefined) { return ''; }
    if (m2 < 10000) {
        return Math.round(m2).toLocaleString('nb-NO') + ' m²';
    }
    if (m2 < 1000000) {
        return (m2 / 10000).toFixed(1).replace('.', ',') + ' ha';
    }
    return (m2 / 1000000).toFixed(2).replace('.', ',') + ' km²';
}

function formaterLengde(m) {
    if (m === null || m === undefined) { return ''; }
    if (m < 1000) {
        return Math.round(m).toLocaleString('nb-NO') + ' m';
    }
    return (m / 1000).toFixed(1).replace('.', ',') + ' km';
}

function tomOmrade() {
    document.getElementById('AreaGeoJson').value = '';
    document.getElementById('AreaSquareMeters').value = '';
    document.getElementById('PerimeterMeters').value = '';
    document.getElementById('omradeInfo').classList.add('d-none');
}

/// <summary>
/// Lagrer et tegnet område i de skjulte feltene, som GeoJSON med areal og omkrets.
/// </summary>
function lagreOmrade(layer) {

    var punkter = layer.getLatLngs()[0];

    var areal = L.GeometryUtil.geodesicArea(punkter);

    var omkrets = 0;
    for (var i = 0; i < punkter.length; i++) {
        var neste = punkter[(i + 1) % punkter.length];
        omkrets += punkter[i].distanceTo(neste);
    }
    document.getElementById('AreaGeoJson').value = JSON.stringify(layer.toGeoJSON());
    document.getElementById('AreaSquareMeters').value = areal.toFixed(2);
    document.getElementById('PerimeterMeters').value = omkrets.toFixed(2);

    document.getElementById('omradeAreal').textContent = formaterAreal(areal);
    document.getElementById('omradeOmkrets').textContent = formaterLengde(omkrets);
    document.getElementById('omradeInfo').classList.remove('d-none');
}

/// Satt av stedssøk. null = ingen geografisk begrensning
var stedsBoks = null;
var radiusSenter = null;
var radiusMeter = 5000;
var radiusSirkel = null;
var velgerPunkt = false;
/// <summary>
/// Filtrerer tabellradene og kartet samtidig, og returnerer filteret så stedssøk og radiussøk kan kjøre det.
/// lagRegistre: oppslag fra rad-id til kartlag. lagGrupper: hvor lagene ligger (ellers selve kartet).
/// </summary>
function settOppFilter(kart, lagRegistre, lagGrupper = []) {
    var sokeFelt = document.getElementById('sokeFelt');
    var typeFilter = document.getElementById('typeFilter');
    if (!sokeFelt || !typeFilter) return function () { };

    function filtrer() {
        var tekst = sokeFelt.value.toLowerCase().trim();
        var type = typeFilter.value;

        document.querySelectorAll('.table-row-clickable').forEach(function (rad) {
            var lat = parseFloat(rad.dataset.lat);
            var lng = parseFloat(rad.dataset.lng);

            var passerTekst = tekst === '' || rad.dataset.sok.includes(tekst);
            var passerType = type === '' || rad.dataset.type === type;
            var passerSted = !stedsBoks ||
                (lat >= stedsBoks.minLat && lat <= stedsBoks.maxLat &&
                    lng >= stedsBoks.minLng && lng <= stedsBoks.maxLng);
            var passerRadius = !radiusSenter || radiusSenter.distanceTo(L.latLng(lat, lng)) <= radiusMeter;
            var vis = passerTekst && passerType && passerSted &&passerRadius;

            rad.classList.toggle('d-none', !vis);

            lagRegistre.forEach(function (register, index) {
                var lag = register[rad.dataset.id];
                if (!lag) return;
                var eier = lagGrupper[index] || kart;
                if (vis) { eier.addLayer(lag); } else { eier.removeLayer(lag); }
            });
        });
    }

    sokeFelt.addEventListener('input', filtrer);
    typeFilter.addEventListener('change', filtrer);

    return filtrer;
}

function settOppStedssok(kart, kjorFilter) {
    var feltet = document.getElementById('stedsFelt');
    if (!feltet) return;

    feltet.addEventListener('keydown', function (e) {
        if (e.key !== 'Enter') return;
        e.preventDefault();
        sokSted(feltet.value.trim(), kart, kjorFilter);
    });
}

function sokSted(sporring, kart, kjorFilter)
{
    if (!sporring)
    {
        stedsBoks = null;
        kjorFilter();
        return;
    }
    var url = 'https://nominatim.openstreetmap.org/search?format=jsonv2&limit=1&countrycodes=no&q='
        + encodeURIComponent(sporring);

    fetch(url, { headers: { 'Accept-Language': 'nb' } })
        .then(function (svar) { return svar.json(); })
        .then(function (treff) {
            if (!treff.length) {
                alert('Fant ikke stedet "' + sporring + '".');
                return;
            }

            /// boundingbox: [minLat, maxLat, minLng, maxLng] som tekst
            var b = treff[0].boundingbox;
            stedsBoks = {
                minLat: parseFloat(b[0]), maxLat: parseFloat(b[1]),
                minLng: parseFloat(b[2]), maxLng: parseFloat(b[3])
            };

            kart.fitBounds([
                [stedsBoks.minLat, stedsBoks.minLng],
                [stedsBoks.maxLat, stedsBoks.maxLng]
            ]);

            kjorFilter();
        });
}

/// <summary>
/// Etter «Velg punkt» blir neste klikk på kartet midten av radiussøket.
/// </summary>
function settOppRadiusSok(kart, kjorFilter) {
    var knapp = document.getElementById('velgPunktBtn');
    var radiusFelt = document.getElementById('radiusFelt');
    if (!knapp || !radiusFelt) return;

    knapp.addEventListener('click', function () {
        if (radiusSenter) { nullstillRadius(kart, kjorFilter); return; }

        velgerPunkt = !velgerPunkt;
        knapp.textContent = velgerPunkt ? 'Avbryt' : 'Velg punkt';
        kart.getContainer().style.cursor = velgerPunkt ? 'crosshair' : '';
    });

    kart.on('click', function (e) {
        if (!velgerPunkt) return;

        radiusSenter = e.latlng;
        radiusMeter = (parseFloat(radiusFelt.value) || 5) * 1000;
        velgerPunkt = false;
        knapp.textContent = 'fjern punkt';
        kart.getContainer().style.cursor = '';

        tegnRadius(kart);
        kjorFilter();
    });

    radiusFelt.addEventListener('input', function () {
        if (!radiusSenter) return;
        radiusMeter = (parseFloat(radiusFelt.value) || 5) * 1000;
        tegnRadius(kart);
        kjorFilter();
    });
}

function tegnRadius(kart) {
    if (radiusSirkel) { kart.removeLayer(radiusSirkel); }
    radiusSirkel = L.circle(radiusSenter, {
        radius: radiusMeter,
        color: '#0d6efd',
        weight: 2,
        fillOpacity: 0.08
    }).addTo(kart);
}

function nullstillRadius(kart, kjorFilter) {
    radiusSenter = null;
    velgerPunkt = false;
    if (radiusSirkel) { kart.removeLayer(radiusSirkel); radiusSirkel = null; }
    document.getElementById('velgPunktBtn').textContent = 'velg punkt';
    kart.getContainer().style.cursor = '';
    kjorFilter();
}