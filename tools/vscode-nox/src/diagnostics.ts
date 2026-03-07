import * as vscode from 'vscode';
import { ModIndex } from './modIndex';

export const DIAGNOSTIC_SOURCE = 'nox-mod';
export const CODE_MISSING_RELATION = 'missing-relation';

/** Données attachées à chaque diagnostic pour le quick fix */
export interface DiagnosticData {
    modId: string;
    modJsonPath: string;
}

/** Type étendu car @types/vscode peut ne pas exposer .data selon la version */
export interface DiagnosticWithData extends vscode.Diagnostic {
    data?: DiagnosticData;
}

/**
 * Regex qui capture l'ID dans .GetMod("xxx") ou .GetMod('xxx').
 * Groupe 1 = l'ID du mod.
 */
const GET_MOD_RE = /\.GetMod\s*\(\s*(["'])([^"']+)\1\s*\)/g;

export function registerDiagnostics(context: vscode.ExtensionContext): vscode.DiagnosticCollection {
    const collection = vscode.languages.createDiagnosticCollection(DIAGNOSTIC_SOURCE);

    function refresh(doc: vscode.TextDocument): void {
        if (doc.languageId !== 'csharp') return;
        refreshDoc(doc, collection);
    }

    context.subscriptions.push(
        vscode.workspace.onDidOpenTextDocument(refresh),
        vscode.workspace.onDidChangeTextDocument(e => refresh(e.document)),
        vscode.workspace.onDidSaveTextDocument(refresh),
        vscode.workspace.onDidCloseTextDocument(doc => collection.delete(doc.uri)),
    );

    // Documents déjà ouverts au démarrage
    vscode.workspace.textDocuments.forEach(refresh);

    return collection;
}

export function refreshAllCSharp(collection: vscode.DiagnosticCollection): void {
    vscode.workspace.textDocuments
        .filter(d => d.languageId === 'csharp')
        .forEach(d => refreshDoc(d, collection));
}

function refreshDoc(doc: vscode.TextDocument, collection: vscode.DiagnosticCollection): void {
    const index = ModIndex.getInstance();
    const modInfo = index.findModInfoFor(doc.uri.fsPath);

    if (!modInfo) {
        collection.delete(doc.uri);
        return;
    }

    // IDs considérés comme "déclarés" : relations + propres identités du mod
    const declared = new Set<string>([
        ...modInfo.relations.map(r => r.id),
        modInfo.id,
        ...modInfo.provides,
    ]);

    const text = doc.getText();
    const diagnostics: vscode.Diagnostic[] = [];

    GET_MOD_RE.lastIndex = 0;
    let match: RegExpExecArray | null;

    while ((match = GET_MOD_RE.exec(text)) !== null) {
        const modId = match[2]; // groupe 2 = l'ID (groupe 1 = le quote char)

        if (declared.has(modId)) continue;

        // Range : on surligne uniquement le contenu de la string (sans les guillemets)
        const idStartInDoc = match.index + match[0].indexOf(match[1]) + 1; // +1 pour sauter le quote
        const startPos = doc.positionAt(idStartInDoc);
        const endPos = doc.positionAt(idStartInDoc + modId.length);
        const range = new vscode.Range(startPos, endPos);

        const isKnown = index.isKnownId(modId);
        const message = isKnown
            ? `"${modId}" est utilisé via GetMod() mais absent des relations dans nox.mod.json`
            : `"${modId}" est utilisé via GetMod() mais absent des relations et inconnu dans le workspace`;

        const diag = new vscode.Diagnostic(
            range,
            message,
            vscode.DiagnosticSeverity.Warning
        );
        diag.source = DIAGNOSTIC_SOURCE;
        diag.code = CODE_MISSING_RELATION;
        (diag as DiagnosticWithData).data = { modId, modJsonPath: modInfo.filePath };

        diagnostics.push(diag);
    }

    collection.set(doc.uri, diagnostics);
}
