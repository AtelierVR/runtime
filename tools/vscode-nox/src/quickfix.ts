import * as vscode from 'vscode';
import { DIAGNOSTIC_SOURCE, CODE_MISSING_RELATION, DiagnosticWithData } from './diagnostics';
import { ModIndex } from './modIndex';

export class AddRelationCodeAction implements vscode.CodeActionProvider {
    static readonly providedCodeActionKinds = [vscode.CodeActionKind.QuickFix];

    provideCodeActions(
        _doc: vscode.TextDocument,
        _range: vscode.Range,
        context: vscode.CodeActionContext,
    ): vscode.CodeAction[] {
        const actions: vscode.CodeAction[] = [];

        for (const diag of context.diagnostics) {
            if (diag.source !== DIAGNOSTIC_SOURCE || diag.code !== CODE_MISSING_RELATION) continue;

            const data = (diag as DiagnosticWithData).data;
            if (!data?.modId || !data?.modJsonPath) continue;

            for (const relType of ['depends', 'recommends'] as const) {
                const label = `Ajouter "${data.modId}" comme ${relType} dans nox.mod.json`;
                const action = new vscode.CodeAction(label, vscode.CodeActionKind.QuickFix);
                action.diagnostics = [diag];
                action.isPreferred = relType === 'depends';
                action.command = {
                    command: 'nox-mod.addRelation',
                    title: label,
                    arguments: [data.modJsonPath, data.modId, relType],
                };
                actions.push(action);
            }
        }

        return actions;
    }
}

/** Commande exécutée par le quick fix */
export async function cmdAddRelation(
    modJsonPath: string,
    modId: string,
    relType: string,
): Promise<void> {
    const uri = vscode.Uri.file(modJsonPath);

    // Ouvrir (ou récupérer) le document pour avoir accès au texte versionné
    let doc: vscode.TextDocument;
    try {
        doc = await vscode.workspace.openTextDocument(uri);
    } catch {
        vscode.window.showErrorMessage(`Impossible d'ouvrir ${modJsonPath}`);
        return;
    }

    const text = doc.getText();
    const indent = detectIndent(text);
    const newEntry = buildRelationEntry(modId, relType, indent);

    // Trouver l'array "relations": [...]
    const insertPos = findRelationsInsertPosition(doc, text);
    if (!insertPos) {
        vscode.window.showErrorMessage(
            `Impossible de localiser le tableau "relations" dans ${modJsonPath}`,
        );
        return;
    }

    const edit = new vscode.WorkspaceEdit();

    if (insertPos.isEmpty) {
        // Tableau vide : [] -> [\n  {...}\n]
        edit.replace(uri, insertPos.range, `[\n${newEntry}\n${indent}]`);
    } else {
        // Ajouter avant le crochet fermant, avec une virgule après la dernière entrée
        edit.insert(uri, insertPos.range.start, `,\n${newEntry}`);
    }

    const applied = await vscode.workspace.applyEdit(edit);
    if (!applied) {
        vscode.window.showErrorMessage('Échec de la modification du nox.mod.json.');
        return;
    }

    // Sauvegarder le fichier et mettre à jour l'index
    await doc.save();
    ModIndex.getInstance().indexFile(modJsonPath);
}

// ---------------------------------------------------------------------------
// Helpers
// ---------------------------------------------------------------------------

interface InsertPosition {
    /** true si le tableau relations est vide */
    isEmpty: boolean;
    /**
     * Si isEmpty=true  : range couvre `[]` entier (à remplacer)
     * Si isEmpty=false : range.start est juste avant le `]` final (insertion)
     */
    range: vscode.Range;
}

function findRelationsInsertPosition(
    doc: vscode.TextDocument,
    text: string,
): InsertPosition | undefined {
    // Trouve "relations": [
    const headerRe = /"relations"\s*:\s*\[/;
    const headerMatch = headerRe.exec(text);
    if (!headerMatch) return undefined;

    const openBracketIdx = headerMatch.index + headerMatch[0].length - 1; // position du [

    // Cherche le ] correspondant en comptant les brackets
    let depth = 1;
    let i = openBracketIdx + 1;
    while (i < text.length && depth > 0) {
        const ch = text[i];
        if (ch === '[') depth++;
        else if (ch === ']') depth--;
        if (depth > 0) i++;
    }
    // i est maintenant sur le ]

    const between = text.slice(openBracketIdx + 1, i).trim();
    const isEmpty = between === '';

    if (isEmpty) {
        // Range du [] entier
        const start = doc.positionAt(openBracketIdx);
        const end = doc.positionAt(i + 1);
        return { isEmpty: true, range: new vscode.Range(start, end) };
    } else {
        // Juste avant le ]
        const pos = doc.positionAt(i);
        return { isEmpty: false, range: new vscode.Range(pos, pos) };
    }
}

function buildRelationEntry(modId: string, relType: string, indent: string): string {
    const inner = `${indent}${indent}`;
    return (
        `${inner}{\n` +
        `${inner}${indent}"id": "${modId}",\n` +
        `${inner}${indent}"type": "${relType}",\n` +
        `${inner}${indent}"version": ">=1.0.0"\n` +
        `${inner}}`
    );
}

/** Détecte l'indentation utilisée dans le fichier (ex: "  " ou "\t") */
function detectIndent(text: string): string {
    const match = text.match(/^( +|\t+)/m);
    if (!match) return '  ';
    return match[1].length >= 4 ? '    ' : match[1][0] === '\t' ? '\t' : '  ';
}

// Export direct du raw helper pour les tests
export { detectIndent, buildRelationEntry };
