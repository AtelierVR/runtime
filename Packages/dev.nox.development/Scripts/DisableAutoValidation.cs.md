# Désactivation des fenêtres de validation automatiques

## Problème

Certaines fenêtres s'ouvrent automatiquement au démarrage de Unity et peuvent causer des bugs d'interface, notamment :

1. **XR Interaction Toolkit - Hands Sample Validation**
   - Fenêtre : `Project Settings > Project Validation`
   - Déclencheur : `[InitializeOnLoadMethod]`
   
2. **XR Interaction Toolkit - Starter Assets Validation**
   - Fenêtre : `Project Settings > Project Validation`
   - Déclencheur : `[InitializeOnLoadMethod]`

3. **Autohand - Setup Wizard**
   - Fenêtre : `Auto Hand Setup`
   - Déclencheur : `[InitializeOnLoad]`

4. **Autohand - Pose Data Updater**
   - Fenêtre : `Update Pose Data`
   - Déclencheur : `[InitializeOnLoadMethod]`

## Solution

Utilisez le menu **`Nox/Settings/Disable Auto Validation Windows`** pour activer/désactiver l'ouverture automatique de ces fenêtres.

### Comment faire

1. Allez dans le menu Unity : **Nox > Settings > Disable Auto Validation Windows**
2. Cliquez pour activer (✓) ou désactiver la protection
3. Redémarrez Unity pour appliquer les changements

### État actuel

- ✓ **Activé** : Les fenêtres de validation ne s'ouvriront plus automatiquement
- ✗ **Désactivé** : Les fenêtres s'ouvrent normalement (comportement par défaut)

## Fenêtres affectées

### Désactivées par cette option
- XR Interaction Toolkit Validation (les 2 versions)

### Non affectées (nécessitent une modification manuelle)
- Autohand Setup Wizard
- Autohand Pose Data Updater

Pour désactiver complètement Autohand, commentez ou supprimez les attributs `[InitializeOnLoad]` dans :
- `Packages/com.earnestrobot.autohand/Scripts/Editor/AutoHandSetupWizard.cs`
- `Packages/com.earnestrobot.autohand/Scripts/Editor/AutoHandUpdateDataWizard.cs`

## Préférence EditorPrefs

La préférence est stockée dans : `Nox.DisableAutoValidation` (bool)

Vous pouvez également la modifier programmatiquement :
```csharp
EditorPrefs.SetBool("Nox.DisableAutoValidation", true);
```

