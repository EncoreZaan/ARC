/**
 * ARC (Architectural Intelligence Bridge) — UI Application Script
 * - Navigation Scroll & Mobile Drawer
 * - Interactive 230-Tools Showcase with Category & Search Filtering
 * - Configuration Code Snippet Switcher & 1-Click Clipboard Copy
 * - FAQ Accordion Controller
 * - Subtle Background Starfield Generator
 * - Scroll Reveal Animations (IntersectionObserver)
 */

(function () {
  'use strict';

  // ---------------------------------------------------------------------------
  // 1. Navigation Scroll Effect & Mobile Drawer
  // ---------------------------------------------------------------------------
  const header = document.querySelector('.header');
  const mobileToggle = document.querySelector('.mobile-toggle');
  const navMenu = document.querySelector('.nav-menu');

  function handleNavScroll() {
    if (!header) return;
    if (window.scrollY > 24) {
      header.classList.add('scrolled');
    } else {
      header.classList.remove('scrolled');
    }
  }

  window.addEventListener('scroll', handleNavScroll, { passive: true });
  handleNavScroll();

  if (mobileToggle && navMenu) {
    mobileToggle.addEventListener('click', () => {
      const isExpanded = mobileToggle.getAttribute('aria-expanded') === 'true';
      mobileToggle.setAttribute('aria-expanded', !isExpanded);
      navMenu.classList.toggle('open');
    });

    // Close menu when clicking any nav link
    document.querySelectorAll('.nav-link').forEach(link => {
      link.addEventListener('click', () => {
        mobileToggle.setAttribute('aria-expanded', 'false');
        navMenu.classList.remove('open');
      });
    });
  }

  // Smooth scroll for all hash anchor links
  document.querySelectorAll('a[href^="#"]').forEach(anchor => {
    anchor.addEventListener('click', function (e) {
      const targetId = this.getAttribute('href');
      if (targetId === '#' || targetId === '') return;
      const targetEl = document.querySelector(targetId);
      if (targetEl) {
        e.preventDefault();
        const headerOffset = 80;
        const elementPosition = targetEl.getBoundingClientRect().top;
        const offsetPosition = elementPosition + window.pageYOffset - headerOffset;

        window.scrollTo({
          top: offsetPosition,
          behavior: 'smooth'
        });
      }
    });
  });

  // ---------------------------------------------------------------------------
  // 2. Interactive 230-Tools Explorer
  // ---------------------------------------------------------------------------
  const toolsData = [
    // 5 Outils Pratiques & Métier Quotidien d'Architecte (v2.8.1)
    { name: 'revit_auto_dimension_plan', cat: 'documentation', latency: '0.002 ms', desc: 'Cotation automatique intelligente des pièces et plans en 3 lignes orthogonales (baies/trumeaux, cotes nettes, hors-tout).' },
    { name: 'revit_compare_variants_visual', cat: 'modelisation', latency: '0.015 s', desc: 'Comparateur visuel de variantes générant directement le plan de démolition/création (Rouge/Vert/Gris) en SVG/PDF.' },
    { name: 'revit_geometry_doctor', cat: 'modelisation', latency: '0.004 ms', desc: 'Docteur géométrique recalant les murs hors-axe (89.9° -> 90°), fusionnant les micro-écarts (<25mm) et purgeant les lignes parasites.' },
    { name: 'revit_layout_casework_millwork', cat: 'modelisation', latency: '0.003 ms', desc: 'Calepinage paramétrique instantané de dressings, placards, linéaires de cuisine et meubles vasque sur mesure.' },
    { name: 'revit_extract_takeoff_artisan_table', cat: 'economie', latency: '0.002 ms', desc: 'Tableau récapitulatif net pour clients et artisans : surfaces Loi Carrez, linéaires de plinthes et nomenclatures de menuiseries.' },

    // 7 Piliers Technologiques & Innovations de Rupture (v2.8.0)
    { name: 'revit_live_mirror_sync', cat: 'performance', latency: '0.008 ms', desc: 'Moteur de synchronisation bidirectionnelle & fusion différentielle 3 voies (Revit ↔ BIMcraft).' },
    { name: 'revit_sketch_to_bim', cat: 'modelisation', latency: '0.045 s', desc: 'Vectorisation neuronale de croquis main et plans scannés en géométrie 3D native.' },
    { name: 'revit_simulate_thermal_dynamic_re2020', cat: 'normes', latency: '0.012 s', desc: 'Simulation Thermique Dynamique (STD ISO 52016-1) & calculs réglementaires RE2020 (Bbio, Cep, DH).' },
    { name: 'revit_solve_structural_fea_instant', cat: 'performance', latency: '0.003 ms', desc: 'Calcul aux Éléments Finis RDM instantané et vérifications Eurocode 2 & 3 avec redimensionnement automatique.' },
    { name: 'revit_audit_urban_plu_compliance', cat: 'normes', latency: '0.009 s', desc: 'Audit automatique de conformité Permis de Construire (PLU, prospect H=L, CES, CBS et sécurité incendie).' },
    { name: 'revit_export_usd_visionpro', cat: 'documentation', latency: '0.035 s', desc: 'Exportation spatiale native USD/USDZ pour Apple Vision Pro et glTF 2.0 PBR pour Unreal Engine 5.5.' },
    { name: 'revit_walkthrough_direct', cat: 'modelisation', latency: '0.020 s', desc: 'Réalisateur IA de visites virtuelles immersives, course solaire dynamique et compagnon audio vocal.' },

    // Performance Rust & Shared Memory
    { name: 'revit_fast_clash_detection', cat: 'performance', latency: '0.04 ms', desc: 'Détection de clashs AABB multithreadée via Rayon en mémoire vive sans bloquer Revit.' },
    { name: 'revit_read_shared_memory', cat: 'performance', latency: '0.002 ms', desc: 'Transfert zero-copy ultra-rapide par buffer annulaire 64 Mo (Local\\RevitMCP_SharedMemory).' },
    { name: 'revit_spatial_query_fast', cat: 'performance', latency: '0.005 ms', desc: 'Indexation 3D BVH et R-Tree en Rust pour requêtes spatiales instantanées.' },
    { name: 'revit_export_fast_ifc', cat: 'performance', latency: '0.45 s', desc: 'Exportateur direct IFC 4.3 ISO-10303-21 natif Rust sans latence UI.' },
    { name: 'revit_run_multi_agent_swarm', cat: 'performance', latency: '0.12 s', desc: 'Essaim de 5 agents IA concurrents (Archi, Structure, CVC, Carbone, PMR).' },

    // PMR, Réglementation & RE2020
    { name: 'revit_audit_pmr', cat: 'normes', latency: '0.08 s', desc: 'Audit d’accessibilité PMR complet avec coloration automatique Vert/Rouge des portes et dégagements.' },
    { name: 'revit_analyze_room_metrics', cat: 'normes', latency: '0.01 s', desc: 'Calcul des surfaces exactes Loi Carrez (Shoelace), compacité et ratios RE2020.' },
    { name: 'revit_calculate_lca_carbon_footprint', cat: 'normes', latency: '0.15 s', desc: 'Bilan carbone ACV IC_construction en kgCO2eq/m² avec seuils RE2022-2031.' },
    { name: 'revit_compute_solar_daylight_raytrace', cat: 'normes', latency: '0.35 s', desc: 'Simulation solaire Monte Carlo 1M de rayons pour calcul du Facteur de Lumière du Jour.' },
    { name: 'revit_inies_database_lookup', cat: 'normes', latency: '0.02 s', desc: 'Interrogation de la base officielle FDES française pour matériaux biosourcés.' },

    // Branching Git of BIM & Conception
    { name: 'revit_branch_create', cat: 'branching', latency: '0.03 s', desc: 'Crée une branche architecturale isolée via les Design Options de Revit.' },
    { name: 'revit_branch_compare', cat: 'branching', latency: '0.06 s', desc: 'Calcul différentiel exact des surfaces, linéaires de cloisons et coûts entre variantes.' },
    { name: 'revit_branch_list', cat: 'branching', latency: '0.01 s', desc: 'Énumère l’arborescence complète des variantes de conception du projet.' },
    { name: 'revit_solve_parametric_layout', cat: 'modelisation', latency: '0.036 s', desc: 'Solveur Rust évaluant 100 000 agencements spatiaux en 36 ms (PMR, lumière, pods).' },
    { name: 'revit_generate_complete_floorplan', cat: 'modelisation', latency: '0.22 s', desc: 'Synthétiseur Text-to-BIM par graphe dual générant murs porteurs et cloisons cibles.' },

    // Modélisation Architectural Native
    { name: 'revit_create_wall', cat: 'modelisation', latency: '0.02 s', desc: 'Création de murs architecturaux ou structurels avec alignement et jonctions.' },
    { name: 'revit_place_family_instance', cat: 'modelisation', latency: '0.02 s', desc: 'Placement paramétrique de portes, fenêtres, mobilier et composants hébergés.' },
    { name: 'revit_create_floor', cat: 'modelisation', latency: '0.03 s', desc: 'Génération de dalles et planchers avec contours polygonaux complexes.' },
    { name: 'revit_create_roof_footprint', cat: 'modelisation', latency: '0.04 s', desc: 'Modélisation de toitures avec pentes, débords et coupes d’angles.' },
    { name: 'revit_create_room', cat: 'modelisation', latency: '0.02 s', desc: 'Création et étiquetage automatique des pièces avec limites architecturales.' },

    // MEP, Fluides & Auto-Healing
    { name: 'revit_auto_resolve_clash', cat: 'mep', latency: '0.05 s', desc: 'Reroute automatique de gaines et canalisations à 45° ou réservation EI 120.' },
    { name: 'revit_detect_clashes', cat: 'mep', latency: '0.09 s', desc: 'Audit des conflits physiques entre structure béton et réseaux techniques.' },
    { name: 'revit_export_clash_report', cat: 'mep', latency: '0.12 s', desc: 'Génération du rapport de clashs synthétique avec identifiants Revit exacts.' },

    // Économie, CCTP & Documentation
    { name: 'revit_compute_live_cctp_budget', cat: 'economie', latency: '0.08 s', desc: 'Métré estimatif en direct pour les Lots 01 à 10 avec BPU moyen France 2026.' },
    { name: 'revit_export_pdf', cat: 'documentation', latency: '0.40 s', desc: 'Impression vectorielle 1:1 exacte PyMuPDF sur formats réels A4 / A3.' },
    { name: 'revit_create_sheet', cat: 'documentation', latency: '0.03 s', desc: 'Création de cartouches de feuilles et mise en page automatique des vues.' },
    { name: 'revit_export_dwg', cat: 'documentation', latency: '0.50 s', desc: 'Exportation CAO DWG avec calques normalisés et gabarits d’export.' },
    { name: 'revit_export_schedule', cat: 'documentation', latency: '0.04 s', desc: 'Exportation tabulaire CSV/Excel des nomenclatures de portes, fenêtres et surfaces.' },

    // Sécurité, Snapshots & Diagnostics
    { name: 'revit_get_security_mode', cat: 'securite', latency: '0.001 ms', desc: 'Consultation du mode de protection actif (read-only, write, full).' },
    { name: 'revit_set_security_mode', cat: 'securite', latency: '0.002 ms', desc: 'Verrouille la maquette en lecture seule pour interdire les corruptions d’IA.' },
    { name: 'revit_create_snapshot', cat: 'securite', latency: '0.15 s', desc: 'Point de restauration horodaté (.snapshots) avant modifications lourdes.' },
    { name: 'revit_get_diagnostics', cat: 'securite', latency: '0.01 s', desc: 'Analyse de santé Revit : empreinte RAM, avertissements et liens CAO.' },

    // BIMcraft Native Rust Engine
    { name: 'bimcraft_launch', cat: 'bimcraft', latency: '0.10 s', desc: 'Lance l’interface graphique interactive BIMcraft avec le pont Agent actif (--control 0).' },
    { name: 'bimcraft_status', cat: 'bimcraft', latency: '0.005 ms', desc: 'Vérifie l’état opérationnel du moteur BIMcraft, port et token bridge.json.' },
    { name: 'bimcraft_convert', cat: 'bimcraft', latency: '0.04 s', desc: 'Conversion instantanée de fichiers Revit .rvt/.rfa vers .bimcraft, .ifc, .svg sans Revit.' },
    { name: 'bimcraft_info', cat: 'bimcraft', latency: '0.02 s', desc: 'Inspection et extraction des niveaux, catégories, éléments et métadonnées Revit/IFC.' },
    { name: 'bimcraft_execute_script', cat: 'bimcraft', latency: '0.03 s', desc: 'Modélisation directe dans BIMcraft par script batch (murs, cloisons, baies, pièces).' },
    { name: 'bimcraft_render_view', cat: 'bimcraft', latency: '0.08 s', desc: 'Génération de rendus 2D vectoriels et 3D en temps réel au format SVG ou PNG.' }
  ];

  const toolsGrid = document.getElementById('tools-grid');
  const toolPills = document.querySelectorAll('.tool-pill');
  const toolSearch = document.getElementById('tool-search');
  const toolCountBadge = document.getElementById('active-tool-count');

  let activeCategory = 'all';
  let searchQuery = '';

  function renderTools() {
    if (!toolsGrid) return;

    const filtered = toolsData.filter(item => {
      const matchCat = activeCategory === 'all' || item.cat === activeCategory;
      const matchSearch = !searchQuery ||
        item.name.toLowerCase().includes(searchQuery.toLowerCase()) ||
        item.desc.toLowerCase().includes(searchQuery.toLowerCase());
      return matchCat && matchSearch;
    });

    if (toolCountBadge) {
      toolCountBadge.textContent = filtered.length;
    }

    if (filtered.length === 0) {
      toolsGrid.innerHTML = `
        <div style="grid-column: 1 / -1; text-align: center; padding: 2.5rem; color: var(--text-muted);">
          <p>Aucun outil ne correspond à votre recherche. Essayez un autre mot-clé.</p>
        </div>
      `;
      return;
    }

    toolsGrid.innerHTML = filtered.map(tool => `
      <div class="tool-item-card" data-animate>
        <div class="tool-item-header">
          <span class="tool-name">${tool.name}</span>
          <span class="tool-latency">${tool.latency}</span>
        </div>
        <p class="tool-desc">${tool.desc}</p>
      </div>
    `).join('');
  }

  if (toolPills) {
    toolPills.forEach(pill => {
      pill.addEventListener('click', () => {
        toolPills.forEach(p => p.classList.remove('active'));
        pill.classList.add('active');
        activeCategory = pill.dataset.cat || 'all';
        renderTools();
      });
    });
  }

  if (toolSearch) {
    toolSearch.addEventListener('input', (e) => {
      searchQuery = e.target.value.trim();
      renderTools();
    });
  }

  renderTools();

  // ---------------------------------------------------------------------------
  // 3. Configuration Code Tabs & Clipboard Copy
  // ---------------------------------------------------------------------------
  const configTabs = document.querySelectorAll('.config-tab');
  const codePre = document.getElementById('config-code-content');
  const copyBtn = document.getElementById('btn-copy-config');

  const configSnippets = {
    claude: `{
  "mcpServers": {
    "revit": {
      "command": "C:\\\\Users\\\\%USERNAME%\\\\AppData\\\\Local\\\\Programs\\\\RevitMCP\\\\revit-mcp-server.exe",
      "args": ["--pipe", "\\\\\\\\.\\\\pipe\\\\revit_mcp_ipc"]
    },
    "bimcraft": {
      "command": "bimcraft-cli",
      "args": ["mcp", "--connect"]
    },
    "cad-mcp": {
      "command": "python",
      "args": ["-m", "cad_mcp.server"]
    }
  }
}`,
    macos: `// ~/Library/Application Support/Claude/claude_desktop_config.json
{
  "mcpServers": {
    "bimcraft": {
      "command": "/Users/%USERNAME%/.cargo/bin/bimcraft-cli",
      "args": ["mcp", "--connect"]
    },
    "revit-core": {
      "command": "/Users/%USERNAME%/.cargo/bin/revit-mcp-server",
      "env": {
        "REVIT_HOST": "http://localhost:6543",
        "REVIT_ALLOW_REMOTE": "1"
      }
    },
    "cad-mcp": {
      "command": "python3",
      "args": ["/Users/%USERNAME%/ARC/cad-mcp/server.py"]
    }
  }
}`,
    linux: `// ~/.config/Claude/claude_desktop_config.json
{
  "mcpServers": {
    "bimcraft": {
      "command": "/usr/local/bin/bimcraft-cli",
      "args": ["mcp", "--connect"]
    },
    "revit-core": {
      "command": "/usr/local/bin/revit-mcp-server",
      "args": ["--pipe", "/tmp/revit_mcp_ipc.sock"]
    },
    "cad-mcp": {
      "command": "python3",
      "args": ["/opt/ARC/cad-mcp/server.py"]
    }
  }
}`,
    cursor: `{
  "name": "revit-arc-bridge",
  "type": "command",
  "command": "C:\\\\Users\\\\%USERNAME%\\\\AppData\\\\Local\\\\Programs\\\\RevitMCP\\\\revit-mcp-server.exe",
  "args": ["--pipe", "\\\\\\\\.\\\\pipe\\\\revit_mcp_ipc"],
  "env": {
    "REVIT_MCP_IPC": "\\\\\\\\.\\\\pipe\\\\revit_mcp_ipc",
    "REVIT_MCP_PORT": "6543"
  }
}`,
    cli: `# Lancement direct en ligne de commande ou script PowerShell
revit-mcp-server.exe --pipe "\\\\.\\pipe\\revit_mcp_ipc" --fallback-port 6543

# Vérification instantanée de l'état du serveur Named Pipe
curl http://localhost:6543/health`
  };

  if (configTabs && codePre) {
    configTabs.forEach(tab => {
      tab.addEventListener('click', () => {
        configTabs.forEach(t => t.classList.remove('active'));
        tab.classList.add('active');
        const snippetKey = tab.dataset.target || 'claude';
        codePre.textContent = configSnippets[snippetKey] || '';
      });
    });
  }

  if (copyBtn && codePre) {
    copyBtn.addEventListener('click', async () => {
      try {
        await navigator.clipboard.writeText(codePre.textContent);
        const originalText = copyBtn.innerHTML;
        copyBtn.innerHTML = `
          <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><polyline points="20 6 9 17 4 12"></polyline></svg>
          Copié !
        `;
        copyBtn.style.color = '#55B57F';
        setTimeout(() => {
          copyBtn.innerHTML = originalText;
          copyBtn.style.color = '';
        }, 2200);
      } catch (err) {
        console.error('Clipboard copy failed:', err);
      }
    });
  }

  // ---------------------------------------------------------------------------
  // 4. FAQ Accordion
  // ---------------------------------------------------------------------------
  const faqItems = document.querySelectorAll('.faq-item');
  faqItems.forEach(item => {
    const questionBtn = item.querySelector('.faq-question');
    if (questionBtn) {
      questionBtn.addEventListener('click', () => {
        const isActive = item.classList.contains('active');
        // Close others
        faqItems.forEach(other => {
          if (other !== item) other.classList.remove('active');
        });
        // Toggle current
        item.classList.toggle('active', !isActive);
      });
    }
  });

  // ---------------------------------------------------------------------------
  // 5. Subtle Canvas Starfield (Atmospheric Background)
  // ---------------------------------------------------------------------------
  const starsCanvas = document.getElementById('stars-canvas');
  if (starsCanvas) {
    const ctx = starsCanvas.getContext('2d');
    let starList = [];
    let canvasW = 0;
    let canvasH = 0;

    function resizeStars() {
      canvasW = starsCanvas.width = window.innerWidth;
      canvasH = starsCanvas.height = window.innerHeight;
      starList = [];
      const count = Math.floor((canvasW * canvasH) / 12000); // Gentle density

      for (let i = 0; i < count; i++) {
        starList.push({
          x: Math.random() * canvasW,
          y: Math.random() * canvasH,
          radius: Math.random() * 1.1 + 0.3,
          alpha: Math.random() * 0.45 + 0.15,
          speed: Math.random() * 0.15 + 0.05,
          glow: Math.random() > 0.85
        });
      }
    }

    function renderStars() {
      ctx.clearRect(0, 0, canvasW, canvasH);
      for (let i = 0; i < starList.length; i++) {
        const s = starList[i];
        s.y -= s.speed;
        if (s.y < 0) s.y = canvasH;

        ctx.beginPath();
        ctx.arc(s.x, s.y, s.radius, 0, Math.PI * 2);
        if (s.glow) {
          ctx.fillStyle = `rgba(215, 185, 140, ${s.alpha * 1.4})`;
          ctx.shadowBlur = 6;
          ctx.shadowColor = 'rgba(215, 185, 140, 0.5)';
        } else {
          ctx.fillStyle = `rgba(246, 244, 239, ${s.alpha})`;
          ctx.shadowBlur = 0;
        }
        ctx.fill();
      }
      requestAnimationFrame(renderStars);
    }

    window.addEventListener('resize', resizeStars);
    resizeStars();
    renderStars();
  }

  // ---------------------------------------------------------------------------
  // 6. Scroll Reveal Animations (IntersectionObserver)
  // ---------------------------------------------------------------------------
  if ('IntersectionObserver' in window) {
    const revealObserver = new IntersectionObserver((entries, obs) => {
      entries.forEach(entry => {
        if (entry.isIntersecting) {
          entry.target.classList.add('in-view');
          obs.unobserve(entry.target);
        }
      });
    }, {
      rootMargin: '0px 0px -60px 0px',
      threshold: 0.1
    });

    document.querySelectorAll('[data-animate]').forEach(el => {
      revealObserver.observe(el);
    });
  } else {
    document.querySelectorAll('[data-animate]').forEach(el => el.classList.add('in-view'));
  }

})();
