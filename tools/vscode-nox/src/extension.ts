import * as vscode from 'vscode';
import * as path from 'path';
import { ModIndex } from './modIndex';
import { registerDiagnostics, refreshAllCSharp } from './diagnostics';
import { AddRelationCodeAction, cmdAddRelation } from './quickfix';
import { getChannel, disposeChannel, log } from './logger';

export async function activate(context: vscode.ExtensionContext): Promise<void> {
    context.subscriptions.push({ dispose: disposeChannel });

    const index = ModIndex.getInstance();
    log('Extension activée');

    // -----------------------------------------------------------------------
    // 1. Construire l'index initial de tous les nox.mod.json du workspace
    // -----------------------------------------------------------------------
    await index.buildAll();

    // -----------------------------------------------------------------------
    // 2. Watcher : maintenir l'index à jour quand les nox.mod.json changent
    // -----------------------------------------------------------------------
    const watcher = vscode.workspace.createFileSystemWatcher(
        '**/{nox.mod.json,nox.mod.jsonc}',
        false, false, false,
    );

    watcher.onDidCreate(uri => {
        index.indexFile(uri.fsPath);
        refreshAllCSharp(collection);
    });
    watcher.onDidChange(uri => {
        index.indexFile(uri.fsPath);
        refreshAllCSharp(collection);
    });
    watcher.onDidDelete(uri => {
        index.removeFile(uri.fsPath);
        refreshAllCSharp(collection);
    });
    context.subscriptions.push(watcher);

    // -----------------------------------------------------------------------
    // 3. Watcher : si un .asmdef est ajouté/supprimé, re-évaluer le mod du dossier
    // -----------------------------------------------------------------------
    const asmdefWatcher = vscode.workspace.createFileSystemWatcher('**/*.asmdef');

    const onAsmdefChange = (uri: vscode.Uri) => {
        index.reindexDir(path.dirname(uri.fsPath));
        refreshAllCSharp(collection);
    };
    asmdefWatcher.onDidCreate(onAsmdefChange);
    asmdefWatcher.onDidDelete(onAsmdefChange);
    context.subscriptions.push(asmdefWatcher);

    // -----------------------------------------------------------------------
    // 4. Diagnostics C# (GetMod sans relation déclarée)
    // -----------------------------------------------------------------------
    const collection = registerDiagnostics(context);
    context.subscriptions.push(collection);

    // -----------------------------------------------------------------------
    // 5. Quick Fix : ajouter la relation manquante dans nox.mod.json
    // -----------------------------------------------------------------------
    context.subscriptions.push(
        vscode.languages.registerCodeActionsProvider(
            { language: 'csharp' },
            new AddRelationCodeAction(),
            { providedCodeActionKinds: AddRelationCodeAction.providedCodeActionKinds },
        ),
    );

    // -----------------------------------------------------------------------
    // 6. Commandes
    // -----------------------------------------------------------------------
    context.subscriptions.push(
        vscode.commands.registerCommand(
            'nox-mod.addRelation',
            (modJsonPath: string, modId: string, relType: string) =>
                cmdAddRelation(modJsonPath, modId, relType),
        ),
        vscode.commands.registerCommand('nox-mod.rebuildIndex', async () => {
            const ch = getChannel();
            ch.show(true); // focus l'output channel
            log('--- Rebuild index ---');
            await index.buildAll();
            refreshAllCSharp(collection);
            const count = index.allMods().length;
            log(`--- Terminé : ${count} mod(s) indexé(s) ---`);
            vscode.window.showInformationMessage(
                `Nox: index reconstruit (${count} mods trouvés)`,
            );
        }),
        vscode.commands.registerCommand('nox-mod.showLog', () => {
            getChannel().show();
        }),
    );
}

export function deactivate(): void {
    // Le DiagnosticCollection et le channel sont disposés via context.subscriptions
}
