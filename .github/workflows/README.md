# GitHub Actions Secrets Configuration

Pour que le workflow fonctionne, vous devez configurer les secrets suivants dans votre repository GitHub :

## Secrets Requis

### Méthode 1 : Unity Personal License (Gratuit)
Allez dans `Settings > Secrets and variables > Actions` et ajoutez :

1. **UNITY_LICENSE** : Votre fichier de licence Unity
   - Obtenir la licence : Exécutez Unity avec `-createManualActivationFile`
   - Activez manuellement sur le site Unity
   - Récupérez le fichier `.ulf`
   - Copiez tout le contenu du fichier dans ce secret

2. **UNITY_EMAIL** : Votre email Unity
3. **UNITY_PASSWORD** : Votre mot de passe Unity

### Méthode 2 : Unity Pro/Plus License
1. **UNITY_SERIAL** : Votre clé de série Unity Pro/Plus
2. **UNITY_EMAIL** : Votre email Unity
3. **UNITY_PASSWORD** : Votre mot de passe Unity

## Configuration du Workflow

### Activer/Désactiver des plateformes
Dans `build.yml`, modifiez la matrice `strategy.matrix.include` :

```yaml
matrix:
  include:
    - targetPlatform: win64
      os: ubuntu-latest
      buildTarget: StandaloneWindows64
    # Ajoutez d'autres plateformes ici
```

### Changer la version Unity
Modifiez la variable d'environnement :

```yaml
env:
  UNITY_VERSION: 6000.0.27f1  # Changez ici
```

### Triggers personnalisés
Le workflow se déclenche sur :
- Push vers `main`, `indev-*`, `develop`
- Pull requests vers `main`, `develop`
- Manuellement via l'interface GitHub Actions

### Build Options
Paramètres disponibles via `customParameters` :
- `-buildTarget` : win64, linux64, osx, android, ios
- `-development` : true/false
- `-allowDebugging` : true/false
- `-customBuildPath` : Chemin de sortie
- `-customBuildName` : Nom du build

## Commandes Utiles

### Tester localement la méthode de build :
```bash
# Windows
Unity.exe -quit -batchmode -executeMethod dev.nox.game_builder.BuildGame.PerformBuild -buildTarget win64 -customBuildPath ./Builds -development true -logFile -

# Linux/Mac
unity-editor -quit -batchmode -executeMethod dev.nox.game_builder.BuildGame.PerformBuild -buildTarget linux64 -customBuildPath ./Builds -development true -logFile -
```

### Activer le build manuel
Dans `build.yml`, changez la ligne :
```yaml
if: false  # Mettre à true pour activer
```

## Structure des Artifacts

Les builds générés seront disponibles dans les artifacts GitHub :
- `Build-win64` : Build Windows
- `Build-linux64` : Build Linux
- `Build-osx` : Build macOS
- `Build-Logs-*` : Logs de build pour debug

## Releases Automatiques

Quand vous créez un tag Git :
```bash
git tag v1.0.0
git push origin v1.0.0
```

Le workflow créera automatiquement une release GitHub avec tous les builds.

## Optimisations

### Cache Library
Le workflow cache le dossier `Library` pour accélérer les builds suivants.

### Free Disk Space
Sur Ubuntu, l'espace disque est libéré automatiquement pour éviter les problèmes.

## Troubleshooting

### Build échoue avec "License not found"
- Vérifiez que les secrets UNITY_LICENSE/UNITY_EMAIL/UNITY_PASSWORD sont correctement configurés
- Pour Personal License, utilisez la méthode d'activation manuelle

### Erreur "No space left on device"
- Le step "Free Disk Space" devrait résoudre ce problème
- Augmentez la valeur de `retention-days` pour les artifacts si nécessaire

### Build timeout
- Les builds Unity peuvent prendre 30-60 minutes
- Ajoutez `timeout-minutes: 90` dans le job si nécessaire

## Resources
- [Unity GameCI Documentation](https://game.ci/docs/)
- [Unity Builder Action](https://github.com/game-ci/unity-builder)
