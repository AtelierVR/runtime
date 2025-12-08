# Hello World Example Mod

Un exemple simple de mod qui affiche "Hello World!" durant l'initialisation.

## Structure

```
hello_world/
├── nox.mod.json              # Métadonnées du mod
├── HelloWorldInitializer.cs  # Code source du mod
└── README.md                 # Ce fichier
```

## Compilation

Pour compiler ce mod, vous pouvez utiliser le compilateur C# :

```bash
# Windows (avec .NET SDK)
dotnet build

# Ou avec csc directement
csc /target:library /reference:Nox.CCK.dll /reference:UnityEngine.CoreModule.dll /out:HelloWorldMod.dll HelloWorldInitializer.cs
```

## Installation

1. Compilez le mod pour obtenir `HelloWorldMod.dll`
2. Copiez le dossier `hello_world` (avec le .dll et nox.mod.json) dans votre dossier de mods :
   - `%APPDATA%\.nox\mods\hello_world\`

## Fonctionnement

Le mod implémente `IMainModInitializer` qui fournit les méthodes de cycle de vie :

- `OnInitialize` : Appelée lors de l'initialisation générale
- `OnInitializeMain` : **C'est ici que "Hello World!" est affiché**
- `OnPostInitializeMain` : Appelée après l'initialisation complète
- `OnPreDispose` : Appelée avant le déchargement
- `OnDispose` : Appelée lors du déchargement du mod

## Output

Quand le mod est chargé, vous verrez dans la console :

```
===========================================
          Hello World!
   From HelloWorldMod example mod
===========================================
```
