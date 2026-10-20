use flate2::write::GzEncoder;
use flate2::Compression;
use std::env;
use std::fs::File;
use std::path::{Path, PathBuf};
use tar::Builder;

fn main() {
    let out_dir = env::var("OUT_DIR").expect("OUT_DIR not set");
    let payload_path = PathBuf::from(&out_dir).join("payload.tar.gz");

    // Base root of repository is parent of installer directory
    let manifest_dir = env::var("CARGO_MANIFEST_DIR").expect("CARGO_MANIFEST_DIR not set");
    let root = Path::new(&manifest_dir).parent().expect("Failed to get parent repo directory");

    let addin_bin = root.join("revit-addin").join("RevitMCP").join("bin").join("Release").join("net10.0-windows");
    let addin_src = root.join("revit-addin").join("RevitMCP");
    let server_dist = root.join("mcp-server").join("dist");

    // Tell cargo to rebuild if source payload files change
    println!("cargo:rerun-if-changed={}", addin_bin.join("RevitMCP.dll").display());
    println!("cargo:rerun-if-changed={}", addin_src.join("RevitMCP.addin").display());
    println!("cargo:rerun-if-changed={}", addin_src.join("RevitMCP.config.json").display());
    println!("cargo:rerun-if-changed={}", server_dist.join("index.bundle.mjs").display());

    let tar_gz = File::create(&payload_path).expect("Failed to create payload.tar.gz");
    let enc = GzEncoder::new(tar_gz, Compression::best());
    let mut tar = Builder::new(enc);

    // Helper to add file with custom archive path
    let mut add_file = |src: &Path, arch_path: &str| {
        if src.exists() {
            let mut f = File::open(src).unwrap_or_else(|_| panic!("Failed to open {}", src.display()));
            tar.append_file(arch_path, &mut f)
                .unwrap_or_else(|_| panic!("Failed to archive {}", arch_path));
        } else {
            panic!("Required payload file not found: {}", src.display());
        }
    };

    // 1. Add-in files
    add_file(&addin_bin.join("RevitMCP.dll"), "addin/RevitMCP.dll");
    add_file(&addin_src.join("RevitMCP.addin"), "addin/RevitMCP.addin");
    add_file(&addin_src.join("RevitMCP.config.json"), "addin/RevitMCP.config.json");
    add_file(&addin_bin.join("Newtonsoft.Json.dll"), "addin/Newtonsoft.Json.dll");
    add_file(&addin_bin.join("Microsoft.CodeAnalysis.dll"), "addin/Microsoft.CodeAnalysis.dll");
    add_file(&addin_bin.join("Microsoft.CodeAnalysis.CSharp.dll"), "addin/Microsoft.CodeAnalysis.CSharp.dll");

    // 2. Server bundle
    add_file(&server_dist.join("index.bundle.mjs"), "server/index.bundle.mjs");

    tar.finish().expect("Failed to finalize tar archive");
    println!("cargo:warning=RevitMCP payload successfully packed into payload.tar.gz");
}
