import * as vscode from 'vscode';

let _channel: vscode.OutputChannel | undefined;

export function getChannel(): vscode.OutputChannel {
    if (!_channel) {
        _channel = vscode.window.createOutputChannel('Nox Mod Tools');
    }
    return _channel;
}

export function log(msg: string): void {
    getChannel().appendLine(`[${new Date().toLocaleTimeString()}] ${msg}`);
}

export function disposeChannel(): void {
    _channel?.dispose();
    _channel = undefined;
}
