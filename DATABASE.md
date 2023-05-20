# Feuille de note sur le parsing des sites de carthographie
## Script Commun
```
function generateSQL(NOM, DESCRIPTION, CATEGORIE, PAYS, IDENTIFIANT, TILE_URL, MIN_ZOOM, MAX_ZOOM, SITE, SITE_URL, TILE_SIZE, FORMAT) {
    let SQLTemplate = "INSERT INTO CUSTOMSLAYERS (\"NOM\", \"DESCRIPTION\", \"CATEGORIE\", \"PAYS\", \"IDENTIFIANT\", \"TILE_URL\", \"TILE_FALLBACK_URL\", \"MIN_ZOOM\", \"MAX_ZOOM\", \"FORMAT\", \"SITE\", \"SITE_URL\", \"TILE_SIZE\", \"FAVORITE\", \"TILECOMPUTATIONSCRIPT\", \"VISIBILITY\", \"SPECIALSOPTIONS\", \"RECTANGLES\", \"VERSION\", \"HAS_SCALE\") VALUES ('{NOM}', '{DESCRIPTION}', '{CATEGORIE}', '{PAYS}', '{IDENTIFIANT}','{TILE_URL}', '', {MIN_ZOOM}, {MAX_ZOOM}, '{FORMAT}', '{SITE}', '{SITE_URL}', '{TILE_SIZE}', '0', NULL, 'Visible', '', '', '0', '1');";
    let SQL = SQLTemplate;
    SQL = SQL.replaceAll("{NOM}", HTMLEntities(NOM));
    SQL = SQL.replaceAll("{DESCRIPTION}", HTMLEntities(DESCRIPTION));
    SQL = SQL.replaceAll("{CATEGORIE}", HTMLEntities(CATEGORIE));
    SQL = SQL.replaceAll("{PAYS}", HTMLEntities(PAYS));
    SQL = SQL.replaceAll("{IDENTIFIANT}", HTMLEntities(IDENTIFIANT));
    SQL = SQL.replaceAll("{TILE_URL}", HTMLEntities(TILE_URL));
    SQL = SQL.replaceAll("{MIN_ZOOM}", MIN_ZOOM);
    SQL = SQL.replaceAll("{MAX_ZOOM}", MAX_ZOOM);
    SQL = SQL.replaceAll("{SITE}", HTMLEntities(SITE));
    SQL = SQL.replaceAll("{SITE_URL}", HTMLEntities(SITE_URL));
    SQL = SQL.replaceAll("{TILE_SIZE}", TILE_SIZE);
    SQL = SQL.replaceAll("{FORMAT}", HTMLEntities(FORMAT));
    return SQL;
}

function HTMLEntities(texte, decode = false) {
    if (!texte) {
        return "";
    }

    const HTMLEntitiesEquivalent = {
        "<": "&lt;",
        ">": "&gt;",
        "&": "&amp;",
        "\"": "&quot;",
        "'": "&apos;",
        "¢": "&cent;",
        "£": "&pound;",
        "¥": "&yen;",
        "€": "&euro;",
        "©": "&copy;",
        "®": "&reg;",
        "%": "&percnt;",
        "»": "&raquo;",
        "À": "&Agrave;",
        "Ç": "&Ccedil;",
        "È": "&Egrave;",
        "É": "&Eacute;",
        "Ê": "&Ecirc;",
        "Ô": "&Ocirc;",
        "Ù": "&Ugrave;",
        "à": "&agrave;",
        "ß": "&szlig;",
        "á": "&aacute;",
        "â": "&acirc;",
        "æ": "&aelig;",
        "ç": "&ccedil;",
        "è": "&egrave;",
        "é": "&eacute;",
        "ê": "&ecirc;",
        "ë": "&euml;",
        "ô": "&ocirc;",
        "ù": "&ugrave;",
        "ü": "&uuml;",
        "∣": "&mid;"
    };
	//delete HTML tags like <br>
    let return_texte = texte.replace(/<[^>]*>/g, ' ').trim();
	//delete multiples spaces
	return_texte = return_texte.replace(/\n\s*\n/g, "\n").replace(/\s{2,}/g, " ");
    for (let entity in HTMLEntitiesEquivalent) {
        if (decode) {
            return_texte = return_texte.split(HTMLEntitiesEquivalent[entity]).join(entity);
        } else {
            return_texte = return_texte.split(entity).join(HTMLEntitiesEquivalent[entity]);
        }
    }
    return return_texte;
}

function delay(milliseconds){
    return new Promise(resolve => {
        setTimeout(resolve, milliseconds);
    });
}

function telechargerTexte(nomFichier, contenu) {
  const blob = new Blob([contenu], { type: "text/plain" });
  const a = document.createElement("a");
  a.href = URL.createObjectURL(blob);
  a.download = nomFichier;
  a.click();
  URL.revokeObjectURL(a.href);
}
```


## Layers

### > Géoportail - France

Script to parse Capabilities :
```javascript
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


### > World Imagery Wayback
Add World Imagery Wayback from https://livingatlas.arcgis.com/wayback/

Script to generate layers :
```javascript
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


### > SwissTopo :
Langue la plus parlée : 
	- Allemand : environ 60% de la population
	- Français : environ 20%
https://api3.geo.admin.ch/rest/services/api/MapServer/layersConfig?lang=en
https://wms.geo.admin.ch/?REQUEST=GetCapabilities&SERVICE=WMS&VERSION=1.0.0
https://api3.geo.admin.ch/rest/services/all/MapServer/ch.swisstopo.pixelkarte-farbe-pk1000.noscale/legend?lang=fr
https://api3.geo.admin.ch/static/images/legends/ch.bfs.generalisierte-grenzen_agglomerationen_g1_fr.png

### ArcGIS ;
Attention, in fact this script is not optimal, a large number of layers do not work correctly.
The fetch url comes from the url that is obtained when performing a search in the library (F12 tab)
```
async function fetchLayers(limit) {
    var url = "https://www.arcgis.com/sharing/rest/content/groups/47dd57c9a59d458c86d3d6b978560088/search?f=json&q=(type%3A(%22WMTS%22%20OR%20%22Map%20Service%22%20OR%20%22Vector%20Tile%20Service%22)%20typekeywords%3A(%22Hosted%22%20OR%20%22Tiled%22))%20((type%3A%22Map%20Service%22%20OR%20type%3A%22Image%20Service%22%20OR%20type%3A%22Feature%20Service%22%20OR%20type%3A%22Vector%20Tile%20Service%22%20OR%20type%3A%22OGCFeatureServer%22%20OR%20type%3A%22WMS%22%20OR%20type%3A%22WFS%22%20OR%20type%3A%22WMTS%22%20OR%20type%3A%22KML%22%20OR%20type%3A%22Table%22%20OR%20(type%3A%22Feature%20Collection%22%20AND%20typekeywords%3A%22Route%20Layer%22%5E0.1))%20%20AND%20(-type%3A%22Feature%20Collection%20Template%22%20AND%20-type%3A%22Stream%20Service%22%20AND%20-typekeywords%3A%22Elevation%203D%20Layer%22%20AND%20-typekeywords%3A%22Table%22%20AND%20-type%3A%22Feed%22%20AND%20-typekeywords%3A%22Requires%20Subscription%22%20AND%20-typekeywords%3A%22Requires%20Credits%22))&num=99&sortOrder=desc&start=1&sortField=modified";
    document.body.innerText = "";
    const CORPS_DOMAINE = new URL(url).origin;
    if (window.location.href.indexOf(CORPS_DOMAINE) == -1) {
        alert("Veuillez relancer le script depuis '" + CORPS_DOMAINE + "' pour éviter le blocage d’une requête multiorigines (Cross-Origin Request)");
        window.location.href = CORPS_DOMAINE;
    }
    let HasNextPage = false;
    let TotalLayersFetch = 0;
    let content = "";
    do {
        const response = await fetch(url);
        const data = await response.json();
        console.log(`Nombre total de résultats : ${data.total}`);
        await Promise.all(data.results.map(async (result) => {
            TotalLayersFetch++;
            await delay(1000);
            console.log(`Titre du résultat : ${result.title}`);
            const NOM = result.title;
            const DESCRIPTION = result.description;
            const CATEGORIE = "";
            const PAYS = "";
            const IDENTIFIANT = result.title;
            const TILE_URL = result.url + "/tile/{z}/{y}/{x}";
            const MIN_ZOOM = 0;
            const MAX_ZOOM = 24;
            const SITE = result.owner;
            const SITE_URL = CORPS_DOMAINE;
            let TILE_SIZE = 256;
            let FORMAT = undefined;
            const defaultFormat = "jpeg";
            const layerInfoUrl = result.url + "?f=pjson";
            try {
                const response = await fetch(layerInfoUrl);
                if (response.ok) {
                    const layerData = await response.json();
                    const tempFormat = layerData.tileInfo?.format?.toLowerCase();
                    if (tempFormat == undefined) {
                        FORMAT = "jpeg";
                    } else if (tempFormat == "jpeg") {
                        FORMAT = "jpeg";
                    } else if (tempFormat == "mixed") {
                        FORMAT = "jpeg";
                    } else if (tempFormat == "png" || tempFormat.startsWith("png")) {
                        FORMAT = "png";
                    } else {
                        console.error("Le format n'est pas supporté : " + tempFormat + "\n" + layerInfoUrl)
                        return;
                    }
                } else {
                    FORMAT = defaultFormat;
                    throw new Error('Erreur de réponse du serveur');
                }
            } catch (error) {
                if (error.name === 'TypeError' && error.message.includes('cross-origin')) {
                    console.warn("La requête a été bloquée en raison d'une violation de la politique Cross-Origin.");
                } else {
                    console.warn('Une erreur s\'est produite lors de la récupération des informations de la couche :' + layerInfoUrl, error);
                }
                FORMAT = defaultFormat;
            }
            console.warn("Ajout de " + NOM);
            content += "\n" + generateSQL(NOM, DESCRIPTION, CATEGORIE, PAYS, IDENTIFIANT, TILE_URL, MIN_ZOOM, MAX_ZOOM, SITE, SITE_URL, TILE_SIZE, FORMAT);

        }));

        HasNextPage = !(data.nextStart == -1) && (TotalLayersFetch <= limit || limit == -1);
        // Récupération de la valeur du paramètre 'start'
        const startValue = url.match(/&start=(\d+)/)[1];
         console.log("Valeur actuelle du paramètre 'start' : " + startValue);
        url = url.replace(/(&start=)\d+/, "$1" + data.nextStart);
    }while (HasNextPage);

    console.log(`TotalLayersFetch : ${TotalLayersFetch}`);

    try {
        const insertFile = await telechargerTexte("insert.txt", content);
        console.log(`Le fichier ${insertFile} a été téléchargé avec succès.`);
    } catch (error) {
        console.error(`Erreur lors de la génération du fichier : ${error}`);
    }
}

fetchLayers(-1).catch(error => {
    console.error(`Une erreur s'est produite lors de l'exécution du script : ${error}`);
});
```

Related url to improve this script :
 - https://www.arcgis.com/sharing/rest/content/items/2c9f3e737cbf4f6faf2eb956fa26cdc5/data
 - https://grand-nancy.maps.arcgis.com/sharing/rest/content/items/428a60494ab34b3f8c83b1692f79077d/data?f=json