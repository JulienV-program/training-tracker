# TrainingTracker

Application locale (.NET 8 / Blazor Server) qui récupère tes séances Strava, les stocke dans une base SQLite locale, génère des graphiques détaillés et un commentaire IA façon coach sportif via un modèle [Ollama](https://ollama.com) tournant en local. 100% local/offline-first : pas de cloud, pas de service tiers en dehors de l'API Strava elle-même.

## Fonctionnalités

- Synchronisation des activités Strava (OAuth refresh token) : résumé, streams seconde par seconde (FC, puissance, cadence, altitude, vitesse, GPS, température, mouvement), laps et splits par km, tracé de parcours.
- Cache local en SQLite (EF Core) — une seule synchro par activité, ré-import manuel possible.
- Graphiques interactifs (Chart.js, vendorisé en local) : courbe FC/puissance/altitude/cadence/température, graphique en barres des splits, tracé GPS du parcours.
- Allure affichée selon le type de sport : km/h à vélo, min/km à pied, min/100m en natation. Cadence course à pied corrigée (Strava ne compte qu'une jambe).
- Commentaire IA généré par un modèle Ollama local (analyse façon coach : points forts, points à travailler, conseil pour la prochaine séance), mis en cache.
- Export CSV des données d'une activité (fonctionnalité conservée du projet d'origine).

## Architecture

Solution .NET 8 en clean architecture :

```
MyTracker.Domain          Modèles métier, interfaces, orchestrateur (ActivityService)
MyTracker.Infrastructure  Client Strava, client Ollama, persistance SQLite (EF Core)
MyTracker.Web             Application Blazor Server (UI, graphiques, IA)
MyTracker.App             Petit utilitaire console (export CSV en ligne de commande)
```

## Prérequis

- [.NET SDK 8.0](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Ollama](https://ollama.com) installé et lancé en local (`ollama serve`), avec un modèle adapté téléchargé — recommandé : `ollama pull qwen2.5:7b-instruct` (bon compromis qualité/RAM, fonctionne sur 16 Go)
- Une application Strava API (créée sur [strava.com/settings/api](https://www.strava.com/settings/api)) pour obtenir `ClientId`, `ClientSecret` et un `RefreshToken`

## Configuration

Les secrets ne sont jamais committés. À configurer en local :

**MyTracker.Web** (via `dotnet user-secrets`) :
```bash
cd MyTracker.Web
dotnet user-secrets init
dotnet user-secrets set "Strava:ClientId" "<ton-client-id>"
dotnet user-secrets set "Strava:ClientSecret" "<ton-client-secret>"
dotnet user-secrets set "Strava:RefreshToken" "<ton-refresh-token>"
```

**MyTracker.App** (console, via variables d'environnement) :
```bash
export Strava__ClientId="<ton-client-id>"
export Strava__ClientSecret="<ton-client-secret>"
export Strava__RefreshToken="<ton-refresh-token>"
```

Voir `MyTracker.Web/appsettings.json.example` pour le détail des clés de configuration disponibles (Ollama, connexion SQLite).

La base SQLite est stockée par défaut dans `~/.trainingtracker/trainingtracker.db` (chemin partagé entre Web et App, configurable via `ConnectionStrings:TrainingTrackerDb`).

## Lancer l'application

```bash
cd MyTracker.Web
dotnet run
```

Puis ouvrir [http://localhost:5215](http://localhost:5215).

Flux d'utilisation : `/activities` → "🔄 Actualiser Strava" → "📥 Importer" sur une séance → "📊 Analyser" pour voir les graphiques et le commentaire IA. Le bouton "🔄" sur une activité déjà importée permet de la ré-importer (utile si l'app a été mise à jour avec de nouvelles données capturées).

## Utilitaire console

```bash
cd MyTracker.App
dotnet run
```

Exporte la dernière activité Strava en CSV dans le répertoire courant.
