## Qu'est ce que MapsInMyFolder

MapsInMyFolder est un logiciel conçu pour simplifier la tâche de téléchargement de cartes et d'images à partir des divers services de cartographie en ligne. Le logiciel peut vous aider à récupérer des données cartographiques de haute qualité à partir de sources en ligne telles que Google Maps, Bing Maps, OpenStreetMap, Yandex Maps, et bien d'autres encore et des les assembler en une grande image pour vos projets.

### Principales caractéristiques
* **Support de multiples sources de cartes :** 
MapsInMyFolder prend en charge une variété de sources de cartes en ligne populaires, y compris Google Maps, Bing Maps, OpenStreetMap, Yandex Maps et plus encore (MapsInMyFolder prend également en charge de nombreux d'autres sources que vous pouvez ajouter selon votre localisation et besoins).
* **Personnalisation avancée :** 
Ce logiciel vous permet de personnaliser les paramètres de téléchargement pour répondre à vos besoins spécifiques. Vous pouvez définir la zone de téléchargement, régler le niveau de zoom, choisir le format de sortie (tel que JPEG, TIFF, ou PNG).

* **Téléchargement rapide et efficace :** 
MapsInMyFolder est conçu pour télécharger des cartes de manière rapide et efficace. Il peut également télécharger des cartes en très haute résolution, ce qui en fait un outil essentiel pour les professionnels et les amateurs de cartographie.

* **Transparence intégrée :** 
MapsInMyFolder prend en charge la gestion de la transparence des calques.

## Quels problèmes MapsInMyFolder résout ?

Lors de nombreux projets, l'accès à des fonds d'images cartographiques, qu'il s'agisse de vues satellites, de cartes cadastrales, ou d'autres types de données géospatiales, est essentiel. Cependant, obtenir ces données peut souvent être fastidieux, chronophage et potentiellement source d'erreurs. Vous pourriez par exemple envisager d'aller sur des services en ligne tels que Google Maps, et de capturer manuellement de nombreuses captures d'écran tout en essayant de maintenir le même niveau de zoom, tout en évitant de manquer des parties importantes, et en vous assurant que les images se chevauchent correctement.
Une fois cette tâche fastidieuse accomplie, vous devriez ensuite assembler toutes ces captures d'écran dans un logiciel de montage, en espérant que rien ne manque et que tout s'aligne correctement. 

Gérer cette procédure pour plusieurs type de calques peut devenir un véritable casse-tête, sans oublier que vous ne pourriez pas gérer la transparence des calques.

C'est là que MapsInMyFolder entre en jeu pour simplifier radicalement ce processus fastidieux. Ce logiciel intuitif élimine les tracas associés au téléchargement et à la gestion de données cartographiques. Voici comment il peut vous aider à gagner du temps et à améliorer votre productivité :
* **Téléchargement simplifié :** 
Avec MapsInMyFolder, vous lancez simplement le logiciel, sélectionnez la zone souhaitée sur la carte, spécifiez le niveau de zoom souhaité, et cliquez sur "Télécharger". La carte est ensuite téléchargée et prête à être utilisée dans vos applications.

* **Évitez les captures manuelles :** 
Oubliez les captures d'écran fastidieuses. MapsInMyFolder automatise le processus de téléchargement, éliminant ainsi les erreurs humaines potentielles, et garantissant un résultat de haute qualité.

* **Évolutif et flexible :** 
Si vous devez élargir votre vue ultérieurement ou travailler sur d'autres projets cartographiques, MapsInMyFolder s'adapte en un clin d'œil, vous faisant gagner un temps précieux.

En somme, MapsInMyFolder est votre allié pour simplifier et accélérer le processus de téléchargement de cartes, en vous évitant les tracas du travail manuel fastidieux. Libérez votre créativité cartographique et gagnez en productivité grâce à ce logiciel efficace et convivial.

## Comment ça marche ?

Le principe fondamental d'un système tuilé de fournisseurs de cartes est de découper une carte géographique en petites tuiles carrés standardisées. Ces tuiles sont précalculées et stockées sur des serveurs pour une utilisation ultérieure. Lorsque vous affichez une carte sur une application ou un site web, le système demande les tuiles nécessaires pour la région actuellement visible à l'écran. Voici une explication plus détaillée :

* **Découpage en tuiles :** 
La carte géographique est divisée en petites tuiles rectangulaires de taille fixe, généralement 256 à 512 pixels. Chaque tuile représente une zone spécifique de la carte à une certaine échelle (zoom).

* **Affichage à l'écran :** 
Lorsque vous consultez une carte sur une application ou un site web, seule une portion de la carte est visible à l'écran à un moment donné. Le système détermine quelles tuiles sont nécessaires pour afficher cette portion de la carte.

* **Requêtes de tuiles :** 
L'application ou le site envoie des requêtes aux serveurs du fournisseur de tuiles (par exemple, Google Maps ou OpenStreetMap) pour demander les tuiles nécessaires à l'affichage de la portion de carte actuellement visible et au zoom souhaité.

* **Récupération des tuiles :** 
Le serveur du fournisseur de tuiles reçoit la demande, génère les tuiles correspondantes à partir de sa base de données, puis les renvoie à l'application. Les tuiles sont généralement des images prérendues.

* **Assemblage des tuiles :** 
Une fois les tuiles récupérées, l'application les assemble pour former la carte complète séléctionnée ou sont agencées de manière à créer une vue continue de la carte.






## Obtenir MapsInMyFolder
Pour obtenir MapsInMyFolder, suivez ces étapes simples :

1. Rendez-vous sur la page des releases de MapsInMyFolder à cette adresse : https://github.com/SioGabx/MapsInMyFolder/releases/latest/.

2. Sur la page des releases, vous trouverez la dernière version de MapsInMyFolder. Téléchargez le fichier MSI en cliquant sur le lien de téléchargement correspondant.

3. Une fois le fichier MSI téléchargé, localisez-le dans votre dossier de téléchargement ou le dossier que vous avez spécifié.

4. Double-cliquez sur le fichier MSI pour lancer l'assistant d'installation.

## Avertissements de Windows Defender et Windows UAC
### Windows Defender
Lorsque vous téléchargez et exécutez le fichier d'installation de MapsInMyFolder, il est possible que Windows Defender, l'antivirus intégré de Windows, affiche un avertissement. Cela est courant avec des fichiers exécutables non signés numériquement. Les fichiers MapsInMyFolder ne sont pas signés numériquement en raison des coûts et des formalités liés à la signature de code. Si vous ne faites pas confiance à l'exécutable disponible sur GitHub, vous avez également la possibilité de compiler le code à partir des sources.

Pour continuer l'installation en toute sécurité, suivez ces étapes :

1. Si Windows Defender affiche un avertissement, cliquez sur "Informations complémentaires" pour afficher davantage de détails.

2. Vous verrez une option intitulée "Exécuter quand même". Cliquez sur cette option pour autoriser l'installation de MapsInMyFolder.

3. L'installation se poursuivra normalement.

### Windows UAC (Contrôle de compte d'utilisateur)
Lorsque vous lancez l'assistant d'installation de MapsInMyFolder, Windows peut également afficher un avertissement pour demander une confirmation en raison des modifications apportées à votre appareil. Suivez ces étapes pour continuer en toute sécurité :

1. Vous verrez une boîte de dialogue Windows UAC demandant "Voulez-vous autoriser cette application à apporter des modifications à votre appareil ?" Cliquez sur "Oui" pour continuer.

2. L'installation se poursuivra normalement.

Ces avertissements sont des mesures de sécurité standard pour garantir que vous êtes conscient des modifications apportées à votre système. MapsInMyFolder est un logiciel sûr, mais il est important de toujours être prudent lors de l'installation de tout logiciel.

Félicitations ! Vous avez maintenant installé MapsInMyFolder. Si vous rencontrez des problèmes d'installation ou avez des questions, vous pouvez créer une issue sur GitHub en cliquant ici : https://github.com/SioGabx/MapsInMyFolder/issues/new 


## Utilisation

### Première utilisation
Au premier démarage de MapsInMyFolder, le logiciel cherchera une base de donnée. En cas d'absence, MapsInMyFolder vous proposera plusieurs choix : de crée une base de donnée vide (sans aucun calques) ou de telecharger une base de donnée complete téléchargeable sur ce dépot Github

### 











