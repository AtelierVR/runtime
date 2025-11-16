# Rigging Avatar Module - Architecture

## Vue d'ensemble

Le système de rigging pour les avatars supporte maintenant deux implémentations :

### 🎯 FinalIK VR (Préféré)
- **Actif quand** : Le define `HAS_FINALIK` est présent
- **Composant** : `VRIK` (Virtual Reality IK)
- **Avantages** :
  - Optimisé pour la VR
  - Meilleure performance
  - Solver IK plus naturel et réaliste
  - Support natif des systèmes VR (tête, mains, bassin)
  - Moins de configuration nécessaire

### 🔧 RigBuilder (Legacy)
- **Actif quand** : `HAS_FINALIK` n'est pas défini
- **Composant** : Unity Animation Rigging (`RigBuilder`)
- **Utilisation** : Fallback pour les projets sans FinalIK

## Structure des fichiers

```
Riggings/
├── RiggingAvatarModule.cs          # Point d'entrée principal
├── FinalIKRigGenerator.cs          # Générateur VRIK (HAS_FINALIK)
├── IKRigGenerator.cs               # Générateur RigBuilder (legacy)
├── RiggingAvatarModuleExtension.cs # Extensions compatibles avec les 2 systèmes
├── HeadTarget.cs                   # Gestion du target de tête
└── IKRigParameters.cs              # Configuration des paramètres
```

## Compilation conditionnelle

Le code utilise des directives de préprocesseur pour séparer les deux implémentations :

```csharp
#if HAS_FINALIK
    // Code FinalIK VR
#else
    // Code RigBuilder (legacy)
#endif
```

## Configuration

Le define `HAS_FINALIK` est automatiquement ajouté quand le package `rootmotion.finalik` est présent dans le projet (voir `Nox.CCK.Avatars.Rigging.asmdef`).

## Migration

Si vous migrez d'un système à l'autre :

1. **Vers FinalIK** : Installez FinalIK, le système détectera automatiquement sa présence
2. **Vers RigBuilder** : Supprimez FinalIK, le système reviendra automatiquement au mode legacy

## API publique

Les méthodes suivantes sont disponibles dans `RiggingAvatarModule` :

- `GetAnchor()` : Récupère l'ancre de rigging (commun)
- `GetVRIK()` : Récupère le composant VRIK (FinalIK uniquement)
- `GetRigBuilder()` : Récupère le RigBuilder (Legacy uniquement)
- `Setup(IRuntimeAvatar)` : Configure le système de rigging approprié

## Extensions

Les méthodes d'extension dans `RiggingAvatarModuleExtension` s'adaptent automatiquement :

- `IsActive(HumanBodyBones)` : Vérifie si un bone est actif
- `SetActive(HumanBodyBones, bool)` : Active/désactive un bone
- `GetOrAddPart(HumanBodyBones, Transform)` : Récupère ou crée une part IK

Ces méthodes fonctionnent de manière transparente avec les deux systèmes.
