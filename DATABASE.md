# Feuille de note sur le parsing des sites de carthographie

----
## Géoportail - France

Script to parse Capabilities :
```
console.log("loading...");
const url = "https://wxs.ign.fr/an7nvfzojv5wa96dsga5nk8w/geoportail/wmts?SERVICE=WMTS&REQUEST=GetCapabilities";
fetch(url)
    .then(response => response.text())
    .then(data => {
        parse(data);
    });

function parse(xmlString) {
    const parser = new DOMParser();
    const xmlDoc = parser.parseFromString(xmlString, "application/xml");
    const layers = xmlDoc.getElementsByTagName("Layer");

    for (let i = 0; i < layers.length; i++) {
        const layer = layers[i];

        const title = layer.getElementsByTagName("ows:Title")[0].textContent;
        const description = layer.getElementsByTagName("ows:Abstract")[0].textContent;
        const keywords = layer.getElementsByTagName("ows:Keywords")[0].textContent;
        const bbox = layer.getElementsByTagName("ows:WGS84BoundingBox")[0];
        const identifier = layer.getElementsByTagName("ows:Identifier")[0].textContent;
        let legendUrl = null;
        try {
            legendElements = layer.getElementsByTagName("LegendURL");
            for (let l = 0; l < legendElements.length; l++) {
                let lurl = legendElements[l].getAttribute("xlink:href");
                if (lurl != "https://wxs.ign.fr/static/legends/LEGEND.jpg") {
                    legendUrl = lurl;
                }
            }
        } catch (error) {}
        const format = layer.getElementsByTagName("Format")[0].textContent;
        const tileMatrixSet = layer.getElementsByTagName("TileMatrixSet")[0].textContent;

        let minTileMatrixSetLimit = null;
        let maxTileMatrixSetLimit = null;
        const tileMatrixSetLimits = layer.getElementsByTagName("TileMatrix");
        for (let j = 0; j < tileMatrixSetLimits.length; j++) {
            const tileMatrixCurrent = parseInt(tileMatrixSetLimits[j].textContent);
            if (minTileMatrixSetLimit === null || minTileMatrixSetLimit > tileMatrixCurrent) {
                minTileMatrixSetLimit = tileMatrixCurrent;
            }

            if (maxTileMatrixSetLimit === null || maxTileMatrixSetLimit < tileMatrixCurrent) {
                maxTileMatrixSetLimit = tileMatrixCurrent;
            }
        }

        console.log("Titre: ", title);
        console.log("Description: ", description);
        console.log("Mots-clés: ", keywords);
        console.log("Bounding box: ", bbox.getAttribute("minx"), bbox.getAttribute("miny"), bbox.getAttribute("maxx"), bbox.getAttribute("maxy"));
        console.log("Identifiant: ", identifier);
        console.log("URL de légende: ", legendUrl);
        console.log("Format: ", format);
        console.log("TileMatrixSet: ", tileMatrixSet);
        console.log("Min Zoom: ", minTileMatrixSetLimit);
        console.log("Max Zoom: ", maxTileMatrixSetLimit);
    }
}
```


----
## World Imagery Wayback
Add World Imagery Wayback from https://livingatlas.arcgis.com/wayback/

Script to generate layers :
```
let SQLTemplate = "INSERT INTO CUSTOMSLAYERS (\"NOM\", \"DESCRIPTION\", \"CATEGORIE\", \"PAYS\", \"IDENTIFIANT\", \"TILE_URL\", \"TILE_FALLBACK_URL\", \"MIN_ZOOM\", \"MAX_ZOOM\", \"FORMAT\", \"SITE\", \"SITE_URL\", \"TILE_SIZE\", \"FAVORITE\", \"TILECOMPUTATIONSCRIPT\", \"VISIBILITY\", \"SPECIALSOPTIONS\", \"RECTANGLES\", \"VERSION\", \"HAS_SCALE\") VALUES ('{NOM}', '{DESCRIPTION}', '{CATEGORIE}', '{PAYS}', '{IDENTIFIANT}','{TILE_URL}', '', {MIN_ZOOM}, {MAX_ZOOM}, '{FORMAT}', '{SITE}', '{SITE_URL}', '{TILE_SIZE}', '0', NULL, 'Visible', '', '', '0', '1');";
let result = "";
let nbrLayers = 0;
fetch('https://s3-us-west-2.amazonaws.com/config.maptiles.arcgis.com/waybackconfig.json')
  .then(response => response.json())
  .then(data => {
    for (let key in data) {
        nbrLayers++;
      const obj = data[key];
let SQL = SQLTemplate;
        let DESCRIPTION = "Wayback imagery is a digital archive of the World Imagery basemap, enabling users to access different versions of World Imagery captured over the years. Each record in the archive represents World Imagery as it existed on the date new imagery was published. Wayback currently supports all updated versions of World Imagery dating back to February 20, 2014.\n - Key :" + key + "\n - itemID : " + obj.itemID + "\n - metadataLayerUrl : " + obj.metadataLayerUrl;
        SQL = SQL.replaceAll("{NOM}",obj.itemTitle);
        SQL = SQL.replaceAll("{DESCRIPTION}",DESCRIPTION);
        SQL = SQL.replaceAll("{CATEGORIE}","Satellite historique");
        SQL = SQL.replaceAll("{PAYS}","*");
        SQL = SQL.replaceAll("{IDENTIFIANT}",obj.metadataLayerItemID);
        SQL = SQL.replaceAll("{TILE_URL}",obj.itemURL.replaceAll("{level}/{row}/{col}", "{z}/{y}/{x}"));
        SQL = SQL.replaceAll("{MIN_ZOOM}",0);
        SQL = SQL.replaceAll("{MAX_ZOOM}",19);
        SQL = SQL.replaceAll("{SITE}","ArcGIS");
        SQL = SQL.replaceAll("{SITE_URL}","https://livingatlas.arcgis.com/wayback/");
         SQL = SQL.replaceAll("{TILE_SIZE}",256);
        SQL = SQL.replaceAll("{FORMAT}","jpeg");
        result += "\n" + SQL;
    }
      console.log(result);
      console.warn("Nombre d'enregistrement à crée : " + nbrLayers)
  })
  .catch(error => console.error(error));
```
----
## SwissTopo :
Langue la plus parlée : 
	- Allemand : environ 60% de la population
	- Français : environ 20%
https://api3.geo.admin.ch/rest/services/api/MapServer/layersConfig?lang=en
https://wms.geo.admin.ch/?REQUEST=GetCapabilities&SERVICE=WMS&VERSION=1.0.0
https://api3.geo.admin.ch/rest/services/all/MapServer/ch.swisstopo.pixelkarte-farbe-pk1000.noscale/legend?lang=fr
https://api3.geo.admin.ch/static/images/legends/ch.bfs.generalisierte-grenzen_agglomerationen_g1_fr.png

