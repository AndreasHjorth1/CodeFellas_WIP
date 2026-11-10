(function () {
    'use strict';
    const config = document.currentScript.dataset;
    const resourceTypes = ['Provision','Clothing','Tool','Vehicle','Materials','Other'];
    const needTypes = ['Food','Medicine','Transport','PowerGenerator','SnowRemoval','SandGravel','Machinery','Evacuation','Communication','Personnel','Other'];
    const resourceFiles = {Provision:'provision',Clothing:'clothing',Tool:'tool',Vehicle:'vehicle',Materials:'materials', Competence: 'other', Other:'other'};
    const needFiles = {Food:'food',Medicine:'medicine',Transport:'transport',PowerGenerator:'power',SnowRemoval:'snow',SandGravel:'gravel',Machinery:'machinery',Evacuation:'evacuation',Communication:'communication',Personnel:'personnel',Other:'other'};
    /// <summary>
    /// Typen kan komme som tall (enum-verdi) eller som navn, avhengig av hvor dataene kommer fra.
    /// </summary>
    function typeName(value, types) {
        if (typeof value === 'number' || (typeof value === 'string' && value.trim() !== '' && Number.isInteger(Number(value)))) return types[Number(value)] || 'Other';
        return types.find(t => t.toLowerCase() === String(value).toLowerCase()) || 'Other';
    }
    function isAvailable(value) { return value === true || value === 1 || value === 'true' || value === 'True'; }
    function isAcute(value) { return value === 0 || value === '0' || value === 'Acute'; }
    function resourceUrl(type, available) {
        return config.resourcesUrl + resourceFiles[typeName(type,resourceTypes)] + (isAvailable(available) ? '' : '-unavailable') + '.svg';
    }
    function needUrl(type) { return config.needsUrl + needFiles[typeName(type,needTypes)] + '.svg'; }
    function icon(url, className) {
        return L.icon({iconUrl:url,iconSize:[40,40],iconAnchor:[20,20],popupAnchor:[0,-24],className:'map-symbol-marker '+(className || '')});
    }
    function needClass(importance) { return 'need-map-icon '+(isAcute(importance) ? 'need-map-acute' : 'need-map-planned'); }
    function escape(value) { return String(value ?? '').replace(/[&<>"']/g, c => ({'&':'&amp;','<':'&lt;','>':'&gt;','"':'&quot;',"'":'&#39;'}[c])); }
    function image(url, className='') { return '<img src="'+escape(url)+'" class="map-symbol-small '+className+'" width="28" height="28" alt="">'; }
    /// <summary>
    /// Samler symboler som ligger tett. Et klikk sprer dem, i stedet for å zoome inn.
    /// </summary>
    function createCluster(map) {
        const group=L.markerClusterGroup({maxClusterRadius:45,zoomToBoundsOnClick:false,spiderfyOnMaxZoom:false,showCoverageOnHover:false,spiderfyDistanceMultiplier:1.6,spiderLegPolylineOptions:{weight:1.5,color:'#164e63',opacity:.7}});
        map.addLayer(group);
        group.on('clusterclick', e => e.layer.spiderfy());
        return group;
    }
    function focus(group,marker) {
        if(group && marker && group.hasLayer(marker)) group.zoomToShowLayer(marker,() => marker.openPopup());
    }
    window.MapSymbols={resourceUrl,needUrl,needClass,isAvailable,isAcute,escape,image,icon,createCluster,focus,
        resourceIcon:(type,available) => icon(resourceUrl(type,available)),
        needIcon:(type,importance) => icon(needUrl(type),needClass(importance))};
})();
