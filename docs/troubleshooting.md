# GUIDE DE DÉPANNAGE — REVIT AI BRIDGE

Ce guide détaille les solutions aux problèmes fréquents rencontrés lors de l'utilisation du pont MCP entre Claude et Autodesk Revit.

---

## 1. Claude n'affiche pas les outils Revit (icône marteau absente)

### Cause 1 : Claude Desktop n'a pas été redémarré après modification du fichier JSON
- Claude Desktop lit sa configuration au démarrage.
- **Solution** : Fermez complètement Claude Desktop (clic droit sur l'icône dans la zone de notification Windows près de l'horloge > Quitter) puis relancez Claude Desktop.

### Cause 2 : Chemin du serveur ou commande Node.js introuvable
- Ouvrez un terminal PowerShell et exécutez le diagnostic :
  ```powershell
  .\scripts\diagnose.ps1
  ```
- Vérifiez que `node -v` répond correctement et que le fichier `C:\Users\teoba\Documents\Devs\MCP Revit\mcp-server\dist\index.js` existe.

---

## 2. Erreur : "Cannot reach Revit addin at http://localhost:6543"

### Cause 1 : Autodesk Revit 2027 n'est pas encore lancé
- L'add-in s'active et démarre le serveur HTTP local au lancement d'Autodesk Revit.
- **Solution** : Lancez Autodesk Revit 2027 et ouvrez un projet `.rvt` (ou le projet de test fourni).

### Cause 2 : Dialogue de sécurité d'Autodesk au premier démarrage
- Au tout premier démarrage après l'installation d'un add-in non signé par Autodesk, Revit affiche une boîte de dialogue demandant l'autorisation :
  *"L'éditeur du complément n'a pas pu être vérifié..."*
- **Action requise** : Cliquez sur **"Toujours charger"** (*Always Load*).

### Cause 3 : Le port 6543 est déjà utilisé par une autre application
- Pour vérifier quel processus écoute sur le port 6543 :
  ```powershell
  Get-NetTCPConnection -LocalPort 6543 -ErrorAction SilentlyContinue
  ```
- Si un autre processus bloque le port, fermez-le puis relancez Revit.

---

## 3. Erreur : "No active document open"

### Cause : Aucun projet n'est ouvert dans Revit
- Revit est ouvert sur la page d'accueil d'Autodesk, mais aucun fichier `.rvt` ou `.rfa` n'est chargé dans l'éditeur.
- **Solution** : Ouvrez un document Revit ou créez un nouveau projet (Fichier > Nouveau > Projet).

---

## 4. Erreur : "Revit API timeout after 120 s"

### Cause : Une opération complexe bloque l'interface ou une boîte de dialogue modale est ouverte
- Revit API est strictement synchrone et monothreadée. Si une boîte de dialogue modale (ex: Alerte de sauvegarde, boîte de dialogue de famille) est ouverte à l'écran, Revit suspend le traitement des `ExternalEvent`.
- **Solution** : Fermez la boîte de dialogue dans Revit. Pour les exports ou requêtes lourdes, vous pouvez augmenter `REVIT_TIMEOUT_MS` dans la configuration Claude.

---

## 5. Comment vérifier manuellement le pont local dans un navigateur

Ouvrez votre navigateur web ou faites un appel curl sur :
```
http://localhost:6543/healthz
```
Si le pont fonctionne, vous recevrez un JSON de santé :
```json
{
  "ok": true,
  "service": "RevitMCP",
  "version": "2.0.0",
  "startedUtc": "2026-10-13T18:25:00Z",
  "uptimeSeconds": 15.2,
  "queueTimeoutSeconds": 10.0,
  "revitApiTimeoutSeconds": 120.0,
  "maxRequestBytes": 2097152
}
```
