# GUIDE DE DÉPANNAGE — REVIT AI BRIDGE (v2.2.0)

Ce guide détaille les solutions aux problèmes fréquents rencontrés lors de l'utilisation du pont MCP entre Claude et Autodesk Revit.

---

## 1. Claude n'affiche pas les outils Revit (icône marteau absente)

### Cause 1 : Claude Desktop n'a pas été redémarré après modification du fichier JSON
- Claude Desktop lit sa configuration au démarrage.
- **Solution** : Fermez complètement Claude Desktop (clic droit sur l'icône dans la zone de notification Windows près de l'horloge > Quitter) puis relancez Claude Desktop.

### Cause 2 : Chemin du serveur Rust ou Node.js introuvable
- Vérifiez que le binaire `revit-mcp-server.exe` existe :
  ```powershell
  Test-Path "revit-mcp-server\target\release\revit-mcp-server.exe"
  ```
- Si vous utilisez l'installateur, réexécutez simplement `RevitMCP-Setup-v2.2.0.exe` qui reconfigure automatiquement Claude Desktop.

---

## 2. Erreur : "Cannot reach Revit addin at http://localhost:6543"

### Cause 1 : Autodesk Revit n'est pas encore lancé
- L'add-in s'active et démarre le serveur HTTP local au lancement d'Autodesk Revit.
- **Solution** : Lancez Autodesk Revit (2027, 2026, 2025 ou 2024) et ouvrez un projet `.rvt`.

### Cause 2 : Dialogue de sécurité d'Autodesk ("Complément non signé")
- Par défaut sur Revit 2024-2027, les compléments compilés localement peuvent afficher la boîte de dialogue de sécurité :
  *"L'éditeur du complément n'a pas pu être vérifié..."*
- **Solution permanente intégrée** :
  L'installateur `RevitMCP-Setup-v2.2.0.exe` (ou le script `scripts/install.ps1`) configure automatiquement le commutateur officiel dans `Revit.ini` :
  ```ini
  [Misc]
  DisableAddInSecurity=1
  ```
  Et enregistre le GUID canonique `a1b2c3d4-e5f6-7890-abcd-ef1234567890` dans le Registre Windows (`HKCU:\SOFTWARE\Autodesk\Revit\Autodesk Revit [Version]\CodeSigning`).

### Cause 3 : Le port 6543 est déjà utilisé par une autre application
- Pour vérifier quel processus écoute sur le port 6543 :
  ```powershell
  Get-NetTCPConnection -LocalPort 6543 -ErrorAction SilentlyContinue
  ```
- Si un autre processus bloque le port, fermez-le puis relancez Revit.

---

## 3. Le panneau latéral "Revit AI Copilot" n'apparaît pas dans Revit

### Cause : Le panneau est masqué ou n'a pas été affiché
- Allez dans l'onglet **"Revit AI Copilot"** du Ruban supérieur de Revit.
- Cliquez sur le premier bouton **"Panneau Copilote"**.
- Le panneau ancrable WPF s'affichera immédiatement sur le côté droit de votre écran.

---

## 4. Erreur : "No active document open"

### Cause : Aucun projet n'est ouvert dans Revit
- Revit est ouvert sur la page d'accueil d'Autodesk, mais aucun fichier `.rvt` ou `.rfa` n'est chargé dans l'éditeur.
- **Solution** : Ouvrez un document Revit ou créez un nouveau projet (Fichier > Nouveau > Projet). Vous pouvez également utiliser l'outil `revit_open_document` pour ouvrir un projet sans action manuelle.

---

## 5. Erreur : "Revit API timeout after 120 s"

### Cause : Une opération complexe bloque l'interface ou une boîte de dialogue modale est ouverte
- Revit API est strictement synchrone et monothreadée. Si une boîte de dialogue modale native (ex: Alerte de sauvegarde, boîte de dialogue de famille) est ouverte à l'écran, Revit suspend le traitement des `ExternalEvent`.
- **Solution** :
  1. Utilisez l'outil `revit_dismiss_dialogs` pour refermer automatiquement les dialogues non bloquants.
  2. Ramenez Revit au premier plan via `revit_bring_to_front` et fermez la boîte de dialogue dans Revit.
  3. Pour les exports ou requêtes lourdes (nuages de points, clash tests volumineux), augmentez `REVIT_TIMEOUT_MS` dans la configuration Claude (ex. `180000`).

---

## 6. Erreur : "PERMISSION_DENIED: Code execution is disabled"

### Cause : `allowCodeExecution` est désactivé
- Par mesure de sécurité pour la distribution du dépôt, l'exécution dynamique (`revit_execute_csharp`, `revit_execute_python`, `revit_run_dynamo_inline`) est bridée à `false` par défaut dans `RevitMCP.config.json`.
- **Solution** :
  Dans le dossier des add-ins (`%APPDATA%\Autodesk\Revit\Addins\2027\RevitMCP.config.json`), passez :
  ```json
  "allowCodeExecution": true
  ```
  Puis redémarrez Revit.

---

## 7. Vérification manuelle du pont local dans un navigateur

Ouvrez votre navigateur web ou faites un appel HTTP sur :
```
http://localhost:6543/healthz
```
Si le pont fonctionne, vous recevrez un statut JSON :
```json
{
  "ok": true,
  "service": "RevitMCP",
  "version": "2.2.0",
  "uptimeSeconds": 1421.19,
  "queueTimeoutSeconds": 10.0,
  "revitApiTimeoutSeconds": 120.0,
  "maxRequestBytes": 2097152,
  "openDocuments": [
    {
      "title": "NomDuProjet",
      "path": "C:\\Projets\\NomDuProjet.rvt",
      "isActive": true,
      "isModified": false,
      "isFamilyDocument": false
    }
  ]
}
```
