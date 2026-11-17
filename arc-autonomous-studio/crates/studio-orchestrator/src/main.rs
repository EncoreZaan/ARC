use std::env;
use std::path::PathBuf;
use studio_ai_runtime::OpenRouterFreeFirstEngine;
use studio_mcp_bridge::verify_legacy_mcp_non_regression;
use studio_orchestrator::{
    run_autonomous_architecture_pipeline, run_local_studio_api_server, sync_unified_arc_ui_snapshot,
};

fn resolve_repo_root() -> PathBuf {
    if let Ok(home) = env::var("ARC_HOME") {
        let p = PathBuf::from(home);
        if p.join("arc-autonomous-studio").exists() {
            return p;
        }
    }
    if let Ok(cwd) = env::current_dir() {
        if cwd.join("arc-autonomous-studio").exists() {
            return cwd;
        }
        if cwd.parent().map(|p| p.join("arc-autonomous-studio").exists()).unwrap_or(false) {
            return cwd.parent().unwrap().to_path_buf();
        }
    }
    if let Ok(exe) = env::current_exe() {
        for anc in exe.ancestors() {
            if anc.join("arc-autonomous-studio").exists() {
                return anc.to_path_buf();
            }
        }
    }
    let default_dev = PathBuf::from(r"c:\Users\teoba\Documents\Devs\MCP Revit");
    if default_dev.join("arc-autonomous-studio").exists() {
        return default_dev;
    }
    env::var("LOCALAPPDATA")
        .map(|d| PathBuf::from(d).join("ARC-Studio"))
        .unwrap_or(default_dev)
}

fn main() -> Result<(), Box<dyn std::error::Error>> {
    let args: Vec<String> = env::args().collect();
    let cmd = args.get(1).map(|s| s.as_str()).unwrap_or("run-cabane-e2e");

    let repo_root = resolve_repo_root();
    let studio_root = repo_root.join("arc-autonomous-studio");

    match cmd {
        "launch" => {
            // Lanceur compagnon instantané Rust : démarre BIMcraft (0 ms de blocage) + Studio daemon en arrière-plan
            let file_arg = args.get(2).map(|s| s.as_str());
            launch_bimcraft_and_studio(file_arg)?;
            Ok(())
        }
        "serve" => {
            let port: u16 = args
                .get(2)
                .and_then(|s| s.parse().ok())
                .unwrap_or(7878);
            run_local_studio_api_server(&repo_root, port)?;
            Ok(())
        }
        "sync-ui" => {
            println!("=== ARC Autonomous Studio : Synchronisation du Snapshot UI Unifié ===");
            let snap = sync_unified_arc_ui_snapshot(&repo_root)?;
            let count = snap
                .get("projects")
                .and_then(|p| p.as_array())
                .map(|a| a.len())
                .unwrap_or(0);
            println!("✅ {} projets synchronisés dans docs/studio-projects-snapshot.js", count);
            Ok(())
        }
        "discover-models" => {
            println!("=== ARC Autonomous Studio : Découverte OpenRouter (Politique FREE-FIRST) ===");
            let cfg_p = studio_root.join("config").join("studio_config.toml");
            let engine = OpenRouterFreeFirstEngine::initialize(Some(&cfg_p));
            println!("{}", serde_json::to_string_pretty(&engine.report)?);
            Ok(())
        }
        "verify-non-regression" => {
            println!("=== ARC Autonomous Studio : Vérification de Non-Régression MCP Historique ===");
            let check = verify_legacy_mcp_non_regression(&repo_root);
            println!("{}", serde_json::to_string_pretty(&check)?);
            if !check.passed {
                std::process::exit(1);
            }
            Ok(())
        }
        "run-cabane-e2e" => {
            let subject_pdf = PathBuf::from(r"C:\Users\teoba\Downloads\LA_CABANE_V3_Planches_A4_1-50.pdf");
            let plan_pdf = PathBuf::from(r"C:\Users\teoba\Downloads\Final\00_SOMMAIRE_LISEZ-MOI.pdf");
            let output_dir = studio_root
                .join("workspace_projects")
                .join("LA_CABANE_AUTONOMOUS");

            execute_and_print_pipeline(&subject_pdf, Some(&plan_pdf), &output_dir, &repo_root)
        }
        "run-london-e2e" => {
            let subject_pdf = PathBuf::from(r"C:\Users\teoba\Documents\Projet Appart Londonien\00_Sources\SUJET-2-l-appartement-londonien-2.pdf");
            let plan_pdf = PathBuf::from(r"C:\Users\teoba\Documents\Projet Appart Londonien\00_Sources\MJM-2026-27-PLAN.pdf");
            let output_dir = studio_root
                .join("workspace_projects")
                .join("REFLET_LONDON_AUTONOMOUS");

            execute_and_print_pipeline(&subject_pdf, Some(&plan_pdf), &output_dir, &repo_root)
        }
        "run" => {
            let subject_pdf = args
                .get(2)
                .map(PathBuf::from)
                .expect("Usage: arc-studio run <sujet.pdf> [plan.pdf] [output_dir]");
            let plan_pdf = args.get(3).map(PathBuf::from);
            let output_dir = args
                .get(4)
                .map(PathBuf::from)
                .unwrap_or_else(|| studio_root.join("workspace_projects").join("CUSTOM_PROJECT_AUTONOMOUS"));

            execute_and_print_pipeline(&subject_pdf, plan_pdf.as_ref().map(|p| p.as_path()), &output_dir, &repo_root)
        }
        _ => {
            println!("Usage: arc-studio [launch [fichier.bimcraft] | serve [port] | sync-ui | run-cabane-e2e | run-london-e2e | discover-models | verify-non-regression | run <sujet.pdf>]");
            Ok(())
        }
    }
}

fn launch_bimcraft_and_studio(file_arg: Option<&str>) -> Result<(), Box<dyn std::error::Error>> {
    use std::net::{SocketAddr, TcpStream};
    use std::process::{Command, Stdio};
    use std::time::Duration;

    // 1. Démarrage instantané de BIMcraft GUI (0 ms d'attente réseau grâce à BIMCRAFT_NO_UPDATE_CHECK=1)
    let bimcraft_exe = env::var("USERPROFILE")
        .or_else(|_| env::var("HOME"))
        .map(|h| PathBuf::from(h).join(".cargo").join("bin").join("bimcraft.exe"))
        .unwrap_or_else(|_| PathBuf::from("bimcraft"));

    let mut bc_cmd = Command::new(&bimcraft_exe);
    bc_cmd.env("BIMCRAFT_NO_UPDATE_CHECK", "1");
    if let Some(f) = file_arg {
        bc_cmd.arg(f);
    }
    bc_cmd.arg("--control").arg("0");
    let _ = bc_cmd
        .stdin(Stdio::null())
        .stdout(Stdio::null())
        .stderr(Stdio::null())
        .spawn();

    // 2. Vérification en < 20 ms du démon Studio et démarrage détaché si absent
    let addr: SocketAddr = "127.0.0.1:7878".parse()?;
    let already_up = TcpStream::connect_timeout(&addr, Duration::from_millis(20)).is_ok();
    if !already_up {
        if let Ok(self_exe) = env::current_exe() {
            let mut srv_cmd = Command::new(self_exe);
            srv_cmd
                .arg("serve")
                .arg("7878")
                .stdin(Stdio::null())
                .stdout(Stdio::null())
                .stderr(Stdio::null());
            #[cfg(windows)]
            {
                use std::os::windows::process::CommandExt;
                const DETACHED_PROCESS: u32 = 0x00000008;
                const CREATE_NO_WINDOW: u32 = 0x08000000;
                srv_cmd.creation_flags(DETACHED_PROCESS | CREATE_NO_WINDOW);
            }
            let _ = srv_cmd.spawn();
        }
    }
    Ok(())
}

fn execute_and_print_pipeline(
    subject_pdf: &std::path::Path,
    plan_pdf: Option<&std::path::Path>,
    output_dir: &std::path::Path,
    repo_root: &std::path::Path,
) -> Result<(), Box<dyn std::error::Error>> {
    println!("=========================================================================");
    println!("🏛️  ARC AUTONOMOUS STUDIO v0.2.0 (OpenRouter Free-First + BIMcraft 0.11.0)");
    println!("=========================================================================");
    println!("📄 Sujet PDF   : {:?}", subject_pdf);
    println!("📐 Plan PDF    : {:?}", plan_pdf);
    println!("📁 Sortie      : {:?}", output_dir);
    println!("-------------------------------------------------------------------------");

    let result = run_autonomous_architecture_pipeline(
        Some(subject_pdf),
        plan_pdf,
        output_dir,
        repo_root,
    )?;

    println!("✅ Pipeline autonome terminé en {} ms !", result.total_duration_ms);
    println!("   Archétype détecté        : {}", result.detected_archetype);
    println!(
        "   Modèles OpenRouter (Free): {} gratuits / {} total (Coût : 0.00 $)",
        result.ai_telemetry.free_models_discovered, result.ai_telemetry.total_models_discovered
    );
    println!("   Variante retenue         : {}", result.selected_variant_id);
    println!("   Modèle APM               : {}", result.apm_path);
    println!("   Rapport Télémétrie IA    : {}", result.ai_telemetry_report_path);
    println!("   Studio Workspace         : {}", result.studio_dashboard_html_path);
    println!(
        "   Non-régression MCP hist. : {} (binaire intact={}, manifest intact={}, outils={})",
        if result.non_regression_check.passed { "100% OK" } else { "ÉCHEC" },
        result.non_regression_check.binary_intact,
        result.non_regression_check.manifest_intact,
        result.non_regression_check.all_tools_count
    );

    let summary_path = output_dir.join("autonomous_run_summary.json");
    std::fs::write(&summary_path, serde_json::to_string_pretty(&result)?)?;
    println!("   Résumé JSON              : {:?}", summary_path);

    let _ = sync_unified_arc_ui_snapshot(repo_root);
    println!("   Interface ARC Unifiée    : docs/index.html (Snapshot synchronisé)");

    if !result.success {
        std::process::exit(1);
    }
    Ok(())
}

#[cfg(test)]
mod tests {
    use super::*;
    use std::path::Path;

    #[test]
    fn test_e2e_different_subject_la_cabane_and_london() {
        let repo_root = Path::new(r"c:\Users\teoba\Documents\Devs\MCP Revit");

        // 1. Test E2E sur le NOUVEAU sujet PDF : LA CABANE (différent de l'Appartement Londonien)
        let cabane_pdf = Path::new(r"C:\Users\teoba\Downloads\LA_CABANE_V3_Planches_A4_1-50.pdf");
        let cabane_out = repo_root
            .join("arc-autonomous-studio")
            .join("workspace_projects")
            .join("LA_CABANE_AUTONOMOUS");

        let res_cabane = run_autonomous_architecture_pipeline(Some(cabane_pdf), None, &cabane_out, repo_root)
            .expect("La Cabane E2E pipeline failed");

        assert!(res_cabane.success);
        assert!(res_cabane.detected_archetype.contains("ModularMicroHabitatCabane"));
        assert_eq!(res_cabane.selected_variant_id, "CAB_V3_PLANCHER_HABITE");
        assert!(res_cabane.ai_telemetry.free_first_policy_enforced);
        assert_eq!(res_cabane.bimcraft_builds.len(), 3);
        for (_, b) in &res_cabane.bimcraft_builds {
            assert!(b.elements_verified);
        }

        // 2. Test E2E de non-régression sur l'Appartement Londonien
        let london_subj = Path::new(r"C:\Users\teoba\Documents\Projet Appart Londonien\00_Sources\SUJET-2-l-appartement-londonien-2.pdf");
        let london_plan = Path::new(r"C:\Users\teoba\Documents\Projet Appart Londonien\00_Sources\MJM-2026-27-PLAN.pdf");
        let london_out = repo_root
            .join("arc-autonomous-studio")
            .join("workspace_projects")
            .join("REFLET_LONDON_AUTONOMOUS");

        let res_london = run_autonomous_architecture_pipeline(
            Some(london_subj),
            Some(london_plan),
            &london_out,
            repo_root,
        )
        .expect("London E2E pipeline failed");

        assert!(res_london.success);
        assert_eq!(res_london.selected_variant_id, "V1_ECLIPSE");
        assert!(res_london.non_regression_check.passed);
    }
}
