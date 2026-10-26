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