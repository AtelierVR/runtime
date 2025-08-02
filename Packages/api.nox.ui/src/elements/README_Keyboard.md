# Nox Keyboard System

Un système de clavier flexible et modulaire pour Unity permettant de créer des claviers virtuels avec différents layouts et modes d'entrée.

## Vue d'ensemble

Le système se compose de plusieurs composants :

- **`Keyboard`** : MonoBehaviour principal qui gère l'état du clavier et les interactions
- **`IKeyboardLayout`** : Interface pour créer des layouts de clavier personnalisés
- **`QwertyKeyboardLayout`** : Layout QWERTY standard pour la saisie de texte
- **`NumericKeyboardLayout`** : Layout numérique pour la saisie de nombres
- **`KeyboardKey`** : Composant pour les touches individuelles avec feedback visuel
- **`KeyboardExample`** : Script d'exemple montrant l'utilisation du système

## Installation

1. Les fichiers sont situés dans `Packages/api.nox.ui/src/elements/` et `Packages/api.nox.ui/src/layouts/`
2. Assurez-vous que les namespaces `Nox.UI` et `Nox.CCK.Utils` sont accessibles
3. Ajoutez les scripts à votre projet Unity

## Utilisation de base

### 1. Création d'un clavier simple

```csharp
// Créer un GameObject pour le clavier
GameObject keyboardObj = new GameObject("MyKeyboard");

// Ajouter le composant Keyboard
Keyboard keyboard = keyboardObj.AddComponent<Keyboard>();

// Créer un container pour les touches
GameObject container = new GameObject("KeyContainer");
container.transform.SetParent(keyboardObj.transform);

// Ajouter un layout QWERTY
QwertyKeyboardLayout layout = keyboardObj.AddComponent<QwertyKeyboardLayout>();

// Configurer le clavier
keyboard.SetLayout(layout);
keyboard.InitializeKeyboard();
```

### 2. Configuration via l'Inspector

1. Créez un GameObject avec le composant `Keyboard`
2. Assignez ou créez un Transform pour `keyContainer`
3. Ajoutez un composant layout (ex: `QwertyKeyboardLayout`)
4. Assignez le layout au champ `layoutComponent` du Keyboard
5. Configurez les paramètres audio, visuels et de comportement

### 3. Gestion des événements

```csharp
// S'abonner aux événements du clavier
keyboard.OnKeyPressed.AddListener(OnKeyPressed);
keyboard.OnTextChanged.AddListener(OnTextChanged);
keyboard.OnSubmit.AddListener(OnSubmit);

void OnKeyPressed(string key) {
    Debug.Log($"Touche pressée: {key}");
}

void OnTextChanged(string newText) {
    Debug.Log($"Texte changé: {newText}");
}

void OnSubmit(string finalText) {
    Debug.Log($"Texte soumis: {finalText}");
}
```

## Layouts disponibles

### QwertyKeyboardLayout

Layout standard QWERTY avec :
- Rangées de touches standard (nombres, lettres)
- Touches spéciales (Shift, Enter, Backspace, Space)
- Support des majuscules/minuscules
- Modes d'entrée : "text", "password", "email"

### NumericKeyboardLayout

Layout numérique avec :
- Pavé numérique 3x4
- Touches d'action (Clear, Backspace, Enter)
- Options : point décimal, signe négatif, opérateurs mathématiques
- Modes d'entrée : "numeric", "decimal", "integer", "pin"

## Création d'un layout personnalisé

Pour créer votre propre layout, implémentez l'interface `IKeyboardLayout` :

```csharp
public class CustomKeyboardLayout : MonoBehaviour, IKeyboardLayout {
    public string GetLayoutName() => "Custom";
    
    public void Initialize(Keyboard keyboard) {
        // Initialiser le layout
    }
    
    public void CreateKeys(Transform container) {
        // Créer et positionner les touches
    }
    
    public void OnKeyPressed(string key) {
        // Gérer les pressions de touches
    }
    
    public void OnKeyReleased(string key) {
        // Gérer les relâchements de touches
    }
    
    public void Cleanup() {
        // Nettoyer les ressources
    }
    
    public Vector2 GetPreferredSize() {
        // Retourner la taille préférée
        return new Vector2(400, 200);
    }
    
    public bool SupportsInputMode(string mode) {
        // Définir les modes d'entrée supportés
        return mode == "custom_mode";
    }
}
```

## Propriétés du Keyboard

### Configuration de base

- **`inputMode`** : Mode d'entrée ("text", "numeric", "password", etc.)
- **`caseSensitive`** : Sensibilité à la casse
- **`autoHide`** : Masquage automatique après soumission
- **`maxInputLength`** : Longueur maximale de l'entrée (0 = illimité)

### Paramètres visuels

- **`keyContainer`** : Container pour les touches
- **`defaultKeyPrefab`** : Prefab par défaut pour les touches
- **`keySpacing`** : Espacement entre les touches
- **`background`** : Image de fond du clavier

### Audio

- **`keyPressSound`** : Son joué lors des pressions de touches
- **`audioSource`** : Source audio pour les sons

## Méthodes principales

### Contrôle de base
- `Show()` : Afficher le clavier
- `Hide()` : Masquer le clavier
- `Toggle()` : Basculer la visibilité

### Gestion du texte
- `SetText(string text)` : Définir le texte
- `ClearText()` : Effacer le texte
- `AppendText(string text)` : Ajouter du texte

### Configuration
- `SetLayout(IKeyboardLayout layout)` : Changer de layout
- `SetInputMode(string mode)` : Changer le mode d'entrée

### Traitement des touches
- `ProcessKeyPress(string key)` : Traiter une pression de touche
- `ProcessKeyReleased(string key)` : Traiter un relâchement de touche

## Événements disponibles

- **`OnKeyPressed`** : Touche pressée
- **`OnKeyReleased`** : Touche relâchée
- **`OnTextChanged`** : Texte modifié
- **`OnSubmit`** : Texte soumis (Enter pressé)
- **`OnKeyboardShown`** : Clavier affiché
- **`OnKeyboardHidden`** : Clavier masqué

## Exemple complet

Voir `KeyboardExample.cs` pour un exemple détaillé d'utilisation montrant :
- Configuration du clavier
- Changement de layouts
- Gestion des événements
- Création programmatique
- Démo automatique

## Notes importantes

1. Assurez-vous que votre scène a un EventSystem pour les interactions UI
2. Les layouts doivent être des MonoBehaviour attachés à des GameObjects
3. Le container de touches doit avoir un RectTransform pour le positionnement
4. Les prefabs de touches doivent avoir un composant Button pour l'interaction
5. Utilisez Canvas et GraphicRaycaster pour l'affichage en mode World Space

## Dépendances

- Unity UI (UnityEngine.UI)
- Nox.CCK.Utils pour le logging
- TextMeshPro (optionnel, pour de meilleurs rendus de texte)
