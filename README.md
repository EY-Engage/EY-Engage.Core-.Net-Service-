# Diagramme de Classes EY Engage

## Description
Ce diagramme représente l'architecture des entités principales du système EY Engage.

## Utilisation
1. Copiez le contenu du fichier `class-diagram.puml`
2. Collez-le dans un éditeur PlantUML en ligne : https://www.plantuml.com/plantuml/uml/
3. Le diagramme sera généré automatiquement

## Entités principales
- **User** : Utilisateurs du système avec rôles et départements
- **Event** : Événements organisés avec système d'approbation
- **JobOffer/JobApplication** : Module de recrutement et candidatures
- **Comment/CommentReply** : Système de commentaires avec réactions
- **EventParticipation/EventInterest** : Gestion des participations aux événements

## Relations clés
- Un utilisateur peut organiser plusieurs événements
- Un événement peut avoir plusieurs participants et intéressés
- Les commentaires peuvent avoir des réponses et des réactions
- Le système de recrutement permet les recommandations entre utilisateurs