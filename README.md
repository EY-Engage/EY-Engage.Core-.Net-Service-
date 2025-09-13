# EY-Engage.Core-.Net-Service-

## Diagramme de Classes

Le diagramme de classes PlantUML est disponible dans le fichier `class-diagram.puml`.

### Structure du Système

Le système EY Engage est organisé autour de plusieurs entités principales :

#### Entités Principales
- **User** : Utilisateurs du système avec authentification et rôles
- **Event** : Événements organisés avec système d'approbation
- **JobOffer** : Offres d'emploi internes
- **Comment** : Système de commentaires avec réactions et réponses

#### Relations Clés
- Les utilisateurs peuvent organiser des événements
- Les événements nécessitent une approbation
- Système de participation avec demandes et approbations
- Commentaires avec réactions et réponses imbriquées
- Offres d'emploi avec candidatures et recommandations

### Visualisation
Pour visualiser le diagramme, utilisez un outil compatible PlantUML comme :
- [PlantUML Online](http://www.plantuml.com/plantuml/uml/)
- Extension VS Code PlantUML
- IntelliJ IDEA avec plugin PlantUML