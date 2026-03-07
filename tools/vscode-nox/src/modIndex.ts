import * as vscode from 'vscode';
import * as fs from 'fs';
import * as path from 'path';
import { parse as parseJsoncSafe } from 'jsonc-parser';
import { log } from './logger';

export interface ModRelation {
    id: string;
    type: 'depends' | 'recommends' | 'suggests' | 'breaks' | 'conflicts' | string;
    version?: string;
}

export interface ModInfo {
    id: string;
    provides: string[];
    relations: ModRelation[];
    filePath: string;
}

export class ModIndex {
    private static _instance: ModIndex | undefined;

    /** Map filePath -> ModInfo */
    private byPath = new Map<string, ModInfo>();
    /** Map id/provide -> ModInfo (chaque alias pointe vers le même ModInfo) */
    private byId = new Map<string, ModInfo>();

    static getInstance(): ModIndex {
        if (!ModIndex._instance) {
            ModIndex._instance = new ModIndex();
        }
        return ModIndex._instance;
    }

    async buildAll(): Promise<void> {
        this.byPath.clear();
        this.byId.clear();

        const jsonFiles  = await vscode.workspace.findFiles('**/nox.mod.json',  '**/node_modules/**');
        const jsoncFiles = await vscode.workspace.findFiles('**/nox.mod.jsonc', '**/node_modules/**');
        const all = [...jsonFiles, ...jsoncFiles];

        log(`buildAll: ${all.length} fichier(s) nox.mod.json trouvé(s) par findFiles`);

        for (const uri of all) {
            this.indexFile(uri.fsPath);
        }

        log(`buildAll: ${this.byPath.size} mod(s) indexé(s) après filtrage asmdef`);
    }

    indexFile(filePath: string): void {
        // Un nox.mod.json n'est valide que si un .asmdef existe dans le même dossier
        const dir = path.dirname(filePath);
        if (!hasAsmdefSibling(dir)) {
            log(`  SKIP (no asmdef) ${filePath}`);
            log(`    fichiers dans le dossier : ${listDir(dir)}`);
            this.removeFile(filePath);
            return;
        }

        try {
            const raw = fs.readFileSync(filePath, 'utf8');
            const json = parseJsonc(raw);

            const info: ModInfo = {
                id: (json.id as string) ?? '',
                provides: Array.isArray(json.provides) ? (json.provides as string[]) : [],
                relations: Array.isArray(json.relations)
                    ? (json.relations as (string | ModRelation)[]).map(normalizeRelation)
                    : [],
                filePath,
            };

            // Supprimer l'ancienne entrée si le fichier existait déjà
            this.removeFile(filePath);

            this.byPath.set(filePath, info);
            if (info.id) {
                this.byId.set(info.id, info);
            }
            for (const p of info.provides) {
                this.byId.set(p, info);
            }
            log(`  OK  id="${info.id}" provides=[${info.provides.join(', ')}]  ${filePath}`);
        } catch (err) {
            log(`  ERR parse failed: ${filePath} — ${err}`);
        }
    }

    removeFile(filePath: string): void {
        const existing = this.byPath.get(filePath);
        if (!existing) return;

        if (existing.id) this.byId.delete(existing.id);
        for (const p of existing.provides) this.byId.delete(p);
        this.byPath.delete(filePath);
    }

    /** Retourne le ModInfo du nox.mod.json le plus proche (remonte l'arbre) */
    findModInfoFor(csFilePath: string): ModInfo | undefined {
        const modJsonPath = this.findNearestModJsonPath(csFilePath);
        if (!modJsonPath) return undefined;
        return this.byPath.get(modJsonPath);
    }

    /** Retourne le chemin du nox.mod.json le plus proche */
    findNearestModJsonPath(filePath: string): string | undefined {
        const roots = (vscode.workspace.workspaceFolders ?? []).map(f => f.uri.fsPath);
        let dir = path.dirname(filePath);

        // Limite : on remonte jusqu'à la racine workspace ou la vraie racine FS
        while (true) {
            for (const name of ['nox.mod.json', 'nox.mod.jsonc']) {
                const candidate = path.join(dir, name);
                if (this.byPath.has(candidate)) {
                    return candidate;
                }
            }
            // Arrêt si on est à la racine workspace
            if (roots.some(r => path.normalize(dir) === path.normalize(r))) break;
            const parent = path.dirname(dir);
            if (parent === dir) break; // racine FS
            dir = parent;
        }
        return undefined;
    }

    /** Vérifie si un mod ID (ou alias) est connu dans le workspace */
    isKnownId(id: string): boolean {
        return this.byId.has(id);
    }

    /** Retourne tous les mods indexés */
    allMods(): ModInfo[] {
        return [...this.byPath.values()];
    }

    /**
     * Appelé quand un .asmdef est créé/supprimé dans un dossier :
     * re-évalue le nox.mod.json du même dossier.
     */
    reindexDir(dir: string): void {
        for (const name of ['nox.mod.json', 'nox.mod.jsonc']) {
            const candidate = path.join(dir, name);
            if (fs.existsSync(candidate)) {
                this.indexFile(candidate);
                return;
            }
        }
    }
}

/** Parse du JSON avec commentaires (// et /* *\/) via jsonc-parser */
export function parseJsonc(content: string): Record<string, unknown> {
    const errors: import('jsonc-parser').ParseError[] = [];
    const result = parseJsoncSafe(content, errors, {
        allowTrailingComma: true,
        disallowComments: false,
    });
    if (errors.length > 0) {
        throw new SyntaxError(
            `jsonc-parser: ${errors.length} erreur(s) — code ${errors[0].error} offset ${errors[0].offset}`,
        );
    }
    return result as Record<string, unknown>;
}

/** Vérifie qu'au moins un fichier .asmdef existe directement dans `dir` */
function hasAsmdefSibling(dir: string): boolean {
    try {
        return fs.readdirSync(dir).some(f => f.endsWith('.asmdef'));
    } catch {
        return false;
    }
}

/** Liste les fichiers du dossier (pour le debug), tronqué à 10 entrées */
function listDir(dir: string): string {
    try {
        const files = fs.readdirSync(dir);
        const preview = files.slice(0, 10).join(', ');
        return files.length > 10 ? `${preview} … (+${files.length - 10})` : preview;
    } catch (e) {
        return `(erreur: ${e})`;
    }
}

/**
 * Normalise une entrée de `relations` :
 * - string "mod-id"  →  { id: "mod-id", type: "depends" }
 * - objet complet    →  tel quel
 */
function normalizeRelation(entry: string | ModRelation): ModRelation {
    if (typeof entry === 'string') {
        return { id: entry, type: 'depends' };
    }
    return entry;
}
