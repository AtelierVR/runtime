"use strict";
var __create = Object.create;
var __defProp = Object.defineProperty;
var __getOwnPropDesc = Object.getOwnPropertyDescriptor;
var __getOwnPropNames = Object.getOwnPropertyNames;
var __getProtoOf = Object.getPrototypeOf;
var __hasOwnProp = Object.prototype.hasOwnProperty;
var __commonJS = (cb, mod) => function __require() {
  return mod || (0, cb[__getOwnPropNames(cb)[0]])((mod = { exports: {} }).exports, mod), mod.exports;
};
var __export = (target, all) => {
  for (var name in all)
    __defProp(target, name, { get: all[name], enumerable: true });
};
var __copyProps = (to, from, except, desc) => {
  if (from && typeof from === "object" || typeof from === "function") {
    for (let key of __getOwnPropNames(from))
      if (!__hasOwnProp.call(to, key) && key !== except)
        __defProp(to, key, { get: () => from[key], enumerable: !(desc = __getOwnPropDesc(from, key)) || desc.enumerable });
  }
  return to;
};
var __toESM = (mod, isNodeMode, target) => (target = mod != null ? __create(__getProtoOf(mod)) : {}, __copyProps(
  // If the importer is in node compatibility mode or this is not an ESM
  // file that has been converted to a CommonJS file using a Babel-
  // compatible transform (i.e. "__esModule" has not been set), then set
  // "default" to the CommonJS "module.exports" for node compatibility.
  isNodeMode || !mod || !mod.__esModule ? __defProp(target, "default", { value: mod, enumerable: true }) : target,
  mod
));
var __toCommonJS = (mod) => __copyProps(__defProp({}, "__esModule", { value: true }), mod);

// node_modules/jsonc-parser/lib/umd/main.js
var require_main = __commonJS({
  "node_modules/jsonc-parser/lib/umd/main.js"(exports2, module2) {
    (function(factory) {
      if (typeof module2 === "object" && typeof module2.exports === "object") {
        var v = factory(require, exports2);
        if (v !== void 0) module2.exports = v;
      } else if (typeof define === "function" && define.amd) {
        define(["require", "exports", "./impl/format", "./impl/edit", "./impl/scanner", "./impl/parser"], factory);
      }
    })(function(require2, exports3) {
      "use strict";
      Object.defineProperty(exports3, "__esModule", { value: true });
      exports3.applyEdits = exports3.modify = exports3.format = exports3.printParseErrorCode = exports3.ParseErrorCode = exports3.stripComments = exports3.visit = exports3.getNodeValue = exports3.getNodePath = exports3.findNodeAtOffset = exports3.findNodeAtLocation = exports3.parseTree = exports3.parse = exports3.getLocation = exports3.SyntaxKind = exports3.ScanError = exports3.createScanner = void 0;
      const formatter = require2("./impl/format");
      const edit = require2("./impl/edit");
      const scanner = require2("./impl/scanner");
      const parser = require2("./impl/parser");
      exports3.createScanner = scanner.createScanner;
      var ScanError;
      (function(ScanError2) {
        ScanError2[ScanError2["None"] = 0] = "None";
        ScanError2[ScanError2["UnexpectedEndOfComment"] = 1] = "UnexpectedEndOfComment";
        ScanError2[ScanError2["UnexpectedEndOfString"] = 2] = "UnexpectedEndOfString";
        ScanError2[ScanError2["UnexpectedEndOfNumber"] = 3] = "UnexpectedEndOfNumber";
        ScanError2[ScanError2["InvalidUnicode"] = 4] = "InvalidUnicode";
        ScanError2[ScanError2["InvalidEscapeCharacter"] = 5] = "InvalidEscapeCharacter";
        ScanError2[ScanError2["InvalidCharacter"] = 6] = "InvalidCharacter";
      })(ScanError || (exports3.ScanError = ScanError = {}));
      var SyntaxKind;
      (function(SyntaxKind2) {
        SyntaxKind2[SyntaxKind2["OpenBraceToken"] = 1] = "OpenBraceToken";
        SyntaxKind2[SyntaxKind2["CloseBraceToken"] = 2] = "CloseBraceToken";
        SyntaxKind2[SyntaxKind2["OpenBracketToken"] = 3] = "OpenBracketToken";
        SyntaxKind2[SyntaxKind2["CloseBracketToken"] = 4] = "CloseBracketToken";
        SyntaxKind2[SyntaxKind2["CommaToken"] = 5] = "CommaToken";
        SyntaxKind2[SyntaxKind2["ColonToken"] = 6] = "ColonToken";
        SyntaxKind2[SyntaxKind2["NullKeyword"] = 7] = "NullKeyword";
        SyntaxKind2[SyntaxKind2["TrueKeyword"] = 8] = "TrueKeyword";
        SyntaxKind2[SyntaxKind2["FalseKeyword"] = 9] = "FalseKeyword";
        SyntaxKind2[SyntaxKind2["StringLiteral"] = 10] = "StringLiteral";
        SyntaxKind2[SyntaxKind2["NumericLiteral"] = 11] = "NumericLiteral";
        SyntaxKind2[SyntaxKind2["LineCommentTrivia"] = 12] = "LineCommentTrivia";
        SyntaxKind2[SyntaxKind2["BlockCommentTrivia"] = 13] = "BlockCommentTrivia";
        SyntaxKind2[SyntaxKind2["LineBreakTrivia"] = 14] = "LineBreakTrivia";
        SyntaxKind2[SyntaxKind2["Trivia"] = 15] = "Trivia";
        SyntaxKind2[SyntaxKind2["Unknown"] = 16] = "Unknown";
        SyntaxKind2[SyntaxKind2["EOF"] = 17] = "EOF";
      })(SyntaxKind || (exports3.SyntaxKind = SyntaxKind = {}));
      exports3.getLocation = parser.getLocation;
      exports3.parse = parser.parse;
      exports3.parseTree = parser.parseTree;
      exports3.findNodeAtLocation = parser.findNodeAtLocation;
      exports3.findNodeAtOffset = parser.findNodeAtOffset;
      exports3.getNodePath = parser.getNodePath;
      exports3.getNodeValue = parser.getNodeValue;
      exports3.visit = parser.visit;
      exports3.stripComments = parser.stripComments;
      var ParseErrorCode;
      (function(ParseErrorCode2) {
        ParseErrorCode2[ParseErrorCode2["InvalidSymbol"] = 1] = "InvalidSymbol";
        ParseErrorCode2[ParseErrorCode2["InvalidNumberFormat"] = 2] = "InvalidNumberFormat";
        ParseErrorCode2[ParseErrorCode2["PropertyNameExpected"] = 3] = "PropertyNameExpected";
        ParseErrorCode2[ParseErrorCode2["ValueExpected"] = 4] = "ValueExpected";
        ParseErrorCode2[ParseErrorCode2["ColonExpected"] = 5] = "ColonExpected";
        ParseErrorCode2[ParseErrorCode2["CommaExpected"] = 6] = "CommaExpected";
        ParseErrorCode2[ParseErrorCode2["CloseBraceExpected"] = 7] = "CloseBraceExpected";
        ParseErrorCode2[ParseErrorCode2["CloseBracketExpected"] = 8] = "CloseBracketExpected";
        ParseErrorCode2[ParseErrorCode2["EndOfFileExpected"] = 9] = "EndOfFileExpected";
        ParseErrorCode2[ParseErrorCode2["InvalidCommentToken"] = 10] = "InvalidCommentToken";
        ParseErrorCode2[ParseErrorCode2["UnexpectedEndOfComment"] = 11] = "UnexpectedEndOfComment";
        ParseErrorCode2[ParseErrorCode2["UnexpectedEndOfString"] = 12] = "UnexpectedEndOfString";
        ParseErrorCode2[ParseErrorCode2["UnexpectedEndOfNumber"] = 13] = "UnexpectedEndOfNumber";
        ParseErrorCode2[ParseErrorCode2["InvalidUnicode"] = 14] = "InvalidUnicode";
        ParseErrorCode2[ParseErrorCode2["InvalidEscapeCharacter"] = 15] = "InvalidEscapeCharacter";
        ParseErrorCode2[ParseErrorCode2["InvalidCharacter"] = 16] = "InvalidCharacter";
      })(ParseErrorCode || (exports3.ParseErrorCode = ParseErrorCode = {}));
      function printParseErrorCode(code) {
        switch (code) {
          case 1:
            return "InvalidSymbol";
          case 2:
            return "InvalidNumberFormat";
          case 3:
            return "PropertyNameExpected";
          case 4:
            return "ValueExpected";
          case 5:
            return "ColonExpected";
          case 6:
            return "CommaExpected";
          case 7:
            return "CloseBraceExpected";
          case 8:
            return "CloseBracketExpected";
          case 9:
            return "EndOfFileExpected";
          case 10:
            return "InvalidCommentToken";
          case 11:
            return "UnexpectedEndOfComment";
          case 12:
            return "UnexpectedEndOfString";
          case 13:
            return "UnexpectedEndOfNumber";
          case 14:
            return "InvalidUnicode";
          case 15:
            return "InvalidEscapeCharacter";
          case 16:
            return "InvalidCharacter";
        }
        return "<unknown ParseErrorCode>";
      }
      exports3.printParseErrorCode = printParseErrorCode;
      function format(documentText, range, options) {
        return formatter.format(documentText, range, options);
      }
      exports3.format = format;
      function modify(text, path3, value, options) {
        return edit.setProperty(text, path3, value, options);
      }
      exports3.modify = modify;
      function applyEdits(text, edits) {
        let sortedEdits = edits.slice(0).sort((a, b) => {
          const diff = a.offset - b.offset;
          if (diff === 0) {
            return a.length - b.length;
          }
          return diff;
        });
        let lastModifiedOffset = text.length;
        for (let i = sortedEdits.length - 1; i >= 0; i--) {
          let e = sortedEdits[i];
          if (e.offset + e.length <= lastModifiedOffset) {
            text = edit.applyEdit(text, e);
          } else {
            throw new Error("Overlapping edit");
          }
          lastModifiedOffset = e.offset;
        }
        return text;
      }
      exports3.applyEdits = applyEdits;
    });
  }
});

// src/extension.ts
var extension_exports = {};
__export(extension_exports, {
  activate: () => activate,
  deactivate: () => deactivate
});
module.exports = __toCommonJS(extension_exports);
var vscode5 = __toESM(require("vscode"));
var path2 = __toESM(require("path"));

// src/modIndex.ts
var vscode2 = __toESM(require("vscode"));
var fs = __toESM(require("fs"));
var path = __toESM(require("path"));
var import_jsonc_parser = __toESM(require_main());

// src/logger.ts
var vscode = __toESM(require("vscode"));
var _channel;
function getChannel() {
  if (!_channel) {
    _channel = vscode.window.createOutputChannel("Nox Mod Tools");
  }
  return _channel;
}
function log(msg) {
  getChannel().appendLine(`[${(/* @__PURE__ */ new Date()).toLocaleTimeString()}] ${msg}`);
}
function disposeChannel() {
  _channel?.dispose();
  _channel = void 0;
}

// src/modIndex.ts
var ModIndex = class _ModIndex {
  constructor() {
    /** Map filePath -> ModInfo */
    this.byPath = /* @__PURE__ */ new Map();
    /** Map id/provide -> ModInfo (chaque alias pointe vers le même ModInfo) */
    this.byId = /* @__PURE__ */ new Map();
  }
  static getInstance() {
    if (!_ModIndex._instance) {
      _ModIndex._instance = new _ModIndex();
    }
    return _ModIndex._instance;
  }
  async buildAll() {
    this.byPath.clear();
    this.byId.clear();
    const jsonFiles = await vscode2.workspace.findFiles("**/nox.mod.json", "**/node_modules/**");
    const jsoncFiles = await vscode2.workspace.findFiles("**/nox.mod.jsonc", "**/node_modules/**");
    const all = [...jsonFiles, ...jsoncFiles];
    log(`buildAll: ${all.length} fichier(s) nox.mod.json trouv\xE9(s) par findFiles`);
    for (const uri of all) {
      this.indexFile(uri.fsPath);
    }
    log(`buildAll: ${this.byPath.size} mod(s) index\xE9(s) apr\xE8s filtrage asmdef`);
  }
  indexFile(filePath) {
    const dir = path.dirname(filePath);
    if (!hasAsmdefSibling(dir)) {
      log(`  SKIP (no asmdef) ${filePath}`);
      log(`    fichiers dans le dossier : ${listDir(dir)}`);
      this.removeFile(filePath);
      return;
    }
    try {
      const raw = fs.readFileSync(filePath, "utf8");
      const json = parseJsonc(raw);
      const info = {
        id: json.id ?? "",
        provides: Array.isArray(json.provides) ? json.provides : [],
        relations: Array.isArray(json.relations) ? json.relations.map(normalizeRelation) : [],
        filePath
      };
      this.removeFile(filePath);
      this.byPath.set(filePath, info);
      if (info.id) {
        this.byId.set(info.id, info);
      }
      for (const p of info.provides) {
        this.byId.set(p, info);
      }
      log(`  OK  id="${info.id}" provides=[${info.provides.join(", ")}]  ${filePath}`);
    } catch (err) {
      log(`  ERR parse failed: ${filePath} \u2014 ${err}`);
    }
  }
  removeFile(filePath) {
    const existing = this.byPath.get(filePath);
    if (!existing) return;
    if (existing.id) this.byId.delete(existing.id);
    for (const p of existing.provides) this.byId.delete(p);
    this.byPath.delete(filePath);
  }
  /** Retourne le ModInfo du nox.mod.json le plus proche (remonte l'arbre) */
  findModInfoFor(csFilePath) {
    const modJsonPath = this.findNearestModJsonPath(csFilePath);
    if (!modJsonPath) return void 0;
    return this.byPath.get(modJsonPath);
  }
  /** Retourne le chemin du nox.mod.json le plus proche */
  findNearestModJsonPath(filePath) {
    const roots = (vscode2.workspace.workspaceFolders ?? []).map((f) => f.uri.fsPath);
    let dir = path.dirname(filePath);
    while (true) {
      for (const name of ["nox.mod.json", "nox.mod.jsonc"]) {
        const candidate = path.join(dir, name);
        if (this.byPath.has(candidate)) {
          return candidate;
        }
      }
      if (roots.some((r) => path.normalize(dir) === path.normalize(r))) break;
      const parent = path.dirname(dir);
      if (parent === dir) break;
      dir = parent;
    }
    return void 0;
  }
  /** Vérifie si un mod ID (ou alias) est connu dans le workspace */
  isKnownId(id) {
    return this.byId.has(id);
  }
  /** Retourne tous les mods indexés */
  allMods() {
    return [...this.byPath.values()];
  }
  /**
   * Appelé quand un .asmdef est créé/supprimé dans un dossier :
   * re-évalue le nox.mod.json du même dossier.
   */
  reindexDir(dir) {
    for (const name of ["nox.mod.json", "nox.mod.jsonc"]) {
      const candidate = path.join(dir, name);
      if (fs.existsSync(candidate)) {
        this.indexFile(candidate);
        return;
      }
    }
  }
};
function parseJsonc(content) {
  const errors = [];
  const result = (0, import_jsonc_parser.parse)(content, errors, {
    allowTrailingComma: true,
    disallowComments: false
  });
  if (errors.length > 0) {
    throw new SyntaxError(
      `jsonc-parser: ${errors.length} erreur(s) \u2014 code ${errors[0].error} offset ${errors[0].offset}`
    );
  }
  return result;
}
function hasAsmdefSibling(dir) {
  try {
    return fs.readdirSync(dir).some((f) => f.endsWith(".asmdef"));
  } catch {
    return false;
  }
}
function listDir(dir) {
  try {
    const files = fs.readdirSync(dir);
    const preview = files.slice(0, 10).join(", ");
    return files.length > 10 ? `${preview} \u2026 (+${files.length - 10})` : preview;
  } catch (e) {
    return `(erreur: ${e})`;
  }
}
function normalizeRelation(entry) {
  if (typeof entry === "string") {
    return { id: entry, type: "depends" };
  }
  return entry;
}

// src/diagnostics.ts
var vscode3 = __toESM(require("vscode"));
var DIAGNOSTIC_SOURCE = "nox-mod";
var CODE_MISSING_RELATION = "missing-relation";
var GET_MOD_RE = /\.GetMod\s*\(\s*(["'])([^"']+)\1\s*\)/g;
function registerDiagnostics(context) {
  const collection = vscode3.languages.createDiagnosticCollection(DIAGNOSTIC_SOURCE);
  function refresh(doc) {
    if (doc.languageId !== "csharp") return;
    refreshDoc(doc, collection);
  }
  context.subscriptions.push(
    vscode3.workspace.onDidOpenTextDocument(refresh),
    vscode3.workspace.onDidChangeTextDocument((e) => refresh(e.document)),
    vscode3.workspace.onDidSaveTextDocument(refresh),
    vscode3.workspace.onDidCloseTextDocument((doc) => collection.delete(doc.uri))
  );
  vscode3.workspace.textDocuments.forEach(refresh);
  return collection;
}
function refreshAllCSharp(collection) {
  vscode3.workspace.textDocuments.filter((d) => d.languageId === "csharp").forEach((d) => refreshDoc(d, collection));
}
function refreshDoc(doc, collection) {
  const index = ModIndex.getInstance();
  const modInfo = index.findModInfoFor(doc.uri.fsPath);
  if (!modInfo) {
    collection.delete(doc.uri);
    return;
  }
  const declared = /* @__PURE__ */ new Set([
    ...modInfo.relations.map((r) => r.id),
    modInfo.id,
    ...modInfo.provides
  ]);
  const text = doc.getText();
  const diagnostics = [];
  GET_MOD_RE.lastIndex = 0;
  let match;
  while ((match = GET_MOD_RE.exec(text)) !== null) {
    const modId = match[2];
    if (declared.has(modId)) continue;
    const idStartInDoc = match.index + match[0].indexOf(match[1]) + 1;
    const startPos = doc.positionAt(idStartInDoc);
    const endPos = doc.positionAt(idStartInDoc + modId.length);
    const range = new vscode3.Range(startPos, endPos);
    const isKnown = index.isKnownId(modId);
    const message = isKnown ? `"${modId}" est utilis\xE9 via GetMod() mais absent des relations dans nox.mod.json` : `"${modId}" est utilis\xE9 via GetMod() mais absent des relations et inconnu dans le workspace`;
    const diag = new vscode3.Diagnostic(
      range,
      message,
      vscode3.DiagnosticSeverity.Warning
    );
    diag.source = DIAGNOSTIC_SOURCE;
    diag.code = CODE_MISSING_RELATION;
    diag.data = { modId, modJsonPath: modInfo.filePath };
    diagnostics.push(diag);
  }
  collection.set(doc.uri, diagnostics);
}

// src/quickfix.ts
var vscode4 = __toESM(require("vscode"));
var AddRelationCodeAction = class {
  static {
    this.providedCodeActionKinds = [vscode4.CodeActionKind.QuickFix];
  }
  provideCodeActions(_doc, _range, context) {
    const actions = [];
    for (const diag of context.diagnostics) {
      if (diag.source !== DIAGNOSTIC_SOURCE || diag.code !== CODE_MISSING_RELATION) continue;
      const data = diag.data;
      if (!data?.modId || !data?.modJsonPath) continue;
      for (const relType of ["depends", "recommends"]) {
        const label = `Ajouter "${data.modId}" comme ${relType} dans nox.mod.json`;
        const action = new vscode4.CodeAction(label, vscode4.CodeActionKind.QuickFix);
        action.diagnostics = [diag];
        action.isPreferred = relType === "depends";
        action.command = {
          command: "nox-mod.addRelation",
          title: label,
          arguments: [data.modJsonPath, data.modId, relType]
        };
        actions.push(action);
      }
    }
    return actions;
  }
};
async function cmdAddRelation(modJsonPath, modId, relType) {
  const uri = vscode4.Uri.file(modJsonPath);
  let doc;
  try {
    doc = await vscode4.workspace.openTextDocument(uri);
  } catch {
    vscode4.window.showErrorMessage(`Impossible d'ouvrir ${modJsonPath}`);
    return;
  }
  const text = doc.getText();
  const indent = detectIndent(text);
  const newEntry = buildRelationEntry(modId, relType, indent);
  const insertPos = findRelationsInsertPosition(doc, text);
  if (!insertPos) {
    vscode4.window.showErrorMessage(
      `Impossible de localiser le tableau "relations" dans ${modJsonPath}`
    );
    return;
  }
  const edit = new vscode4.WorkspaceEdit();
  if (insertPos.isEmpty) {
    edit.replace(uri, insertPos.range, `[
${newEntry}
${indent}]`);
  } else {
    edit.insert(uri, insertPos.range.start, `,
${newEntry}`);
  }
  const applied = await vscode4.workspace.applyEdit(edit);
  if (!applied) {
    vscode4.window.showErrorMessage("\xC9chec de la modification du nox.mod.json.");
    return;
  }
  await doc.save();
  ModIndex.getInstance().indexFile(modJsonPath);
}
function findRelationsInsertPosition(doc, text) {
  const headerRe = /"relations"\s*:\s*\[/;
  const headerMatch = headerRe.exec(text);
  if (!headerMatch) return void 0;
  const openBracketIdx = headerMatch.index + headerMatch[0].length - 1;
  let depth = 1;
  let i = openBracketIdx + 1;
  while (i < text.length && depth > 0) {
    const ch = text[i];
    if (ch === "[") depth++;
    else if (ch === "]") depth--;
    if (depth > 0) i++;
  }
  const between = text.slice(openBracketIdx + 1, i).trim();
  const isEmpty = between === "";
  if (isEmpty) {
    const start = doc.positionAt(openBracketIdx);
    const end = doc.positionAt(i + 1);
    return { isEmpty: true, range: new vscode4.Range(start, end) };
  } else {
    const pos = doc.positionAt(i);
    return { isEmpty: false, range: new vscode4.Range(pos, pos) };
  }
}
function buildRelationEntry(modId, relType, indent) {
  const inner = `${indent}${indent}`;
  return `${inner}{
${inner}${indent}"id": "${modId}",
${inner}${indent}"type": "${relType}",
${inner}${indent}"version": ">=1.0.0"
${inner}}`;
}
function detectIndent(text) {
  const match = text.match(/^( +|\t+)/m);
  if (!match) return "  ";
  return match[1].length >= 4 ? "    " : match[1][0] === "	" ? "	" : "  ";
}

// src/extension.ts
async function activate(context) {
  context.subscriptions.push({ dispose: disposeChannel });
  const index = ModIndex.getInstance();
  log("Extension activ\xE9e");
  await index.buildAll();
  const watcher = vscode5.workspace.createFileSystemWatcher(
    "**/{nox.mod.json,nox.mod.jsonc}",
    false,
    false,
    false
  );
  watcher.onDidCreate((uri) => {
    index.indexFile(uri.fsPath);
    refreshAllCSharp(collection);
  });
  watcher.onDidChange((uri) => {
    index.indexFile(uri.fsPath);
    refreshAllCSharp(collection);
  });
  watcher.onDidDelete((uri) => {
    index.removeFile(uri.fsPath);
    refreshAllCSharp(collection);
  });
  context.subscriptions.push(watcher);
  const asmdefWatcher = vscode5.workspace.createFileSystemWatcher("**/*.asmdef");
  const onAsmdefChange = (uri) => {
    index.reindexDir(path2.dirname(uri.fsPath));
    refreshAllCSharp(collection);
  };
  asmdefWatcher.onDidCreate(onAsmdefChange);
  asmdefWatcher.onDidDelete(onAsmdefChange);
  context.subscriptions.push(asmdefWatcher);
  const collection = registerDiagnostics(context);
  context.subscriptions.push(collection);
  context.subscriptions.push(
    vscode5.languages.registerCodeActionsProvider(
      { language: "csharp" },
      new AddRelationCodeAction(),
      { providedCodeActionKinds: AddRelationCodeAction.providedCodeActionKinds }
    )
  );
  context.subscriptions.push(
    vscode5.commands.registerCommand(
      "nox-mod.addRelation",
      (modJsonPath, modId, relType) => cmdAddRelation(modJsonPath, modId, relType)
    ),
    vscode5.commands.registerCommand("nox-mod.rebuildIndex", async () => {
      const ch = getChannel();
      ch.show(true);
      log("--- Rebuild index ---");
      await index.buildAll();
      refreshAllCSharp(collection);
      const count = index.allMods().length;
      log(`--- Termin\xE9 : ${count} mod(s) index\xE9(s) ---`);
      vscode5.window.showInformationMessage(
        `Nox: index reconstruit (${count} mods trouv\xE9s)`
      );
    }),
    vscode5.commands.registerCommand("nox-mod.showLog", () => {
      getChannel().show();
    })
  );
}
function deactivate() {
}
// Annotate the CommonJS export names for ESM import in node:
0 && (module.exports = {
  activate,
  deactivate
});
