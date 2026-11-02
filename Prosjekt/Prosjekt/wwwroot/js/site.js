// Formatering av areal og lengde fra kartet. Brukes av flere views.
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