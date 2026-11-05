/**
 * ARC (Architectural Intelligence Bridge) — 3D Centerpiece
 * Apple Pro Minimalist Aesthetic — The Holographic Parametric Glass Pavilion
 * - Clean Dielectric Crystal Glass Floor Plates (RDC, R+1, R+2, Rooftop)
 * - Precision Titanium Structural Grid & Glowing Parametric Node Connectors
 * - Responsive Kinetic Architectural Louvers & Facade Mullions
 * - Circular Laser Azimuth Dial & Polar Coordinate Ground Grid
 * - Ascending & Descending Lidar Scan Plane with Additive Light Slice
 * - Silky Inertial Parallax with Damped Mouse Follow & Zero-Spike Viewport Pausing
 */

(function () {
  'use strict';

  function isWebGLAvailable() {
    try {
      const canvas = document.createElement('canvas');
      return !!(window.WebGLRenderingContext && (canvas.getContext('webgl') || canvas.getContext('experimental-webgl')));
    } catch (e) {
      return false;
    }
  }

  const container = document.getElementById('hero-stage');
  const canvas = document.getElementById('webgl-canvas');

  if (!container || !canvas || !isWebGLAvailable() || typeof THREE === 'undefined') return;

  // ---------------------------------------------------------------------------
  // 1. Scene, Camera, Renderer Setup
  // ---------------------------------------------------------------------------
  const scene = new THREE.Scene();
  scene.fog = new THREE.FogExp2(0x070709, 0.045);

  let width = container.clientWidth || window.innerWidth;
  let height = container.clientHeight || 640;

  const camera = new THREE.PerspectiveCamera(36, width / height, 0.1, 100);
  camera.position.set(0, 0.45, 5.2);

  const renderer = new THREE.WebGLRenderer({
    canvas: canvas,
    antialias: true,
    alpha: true,
    powerPreference: 'high-performance'
  });
  renderer.setSize(width, height);
  renderer.setPixelRatio(Math.min(window.devicePixelRatio || 1, 2));
  renderer.toneMapping = THREE.ACESFilmicToneMapping;
  renderer.toneMappingExposure = 1.35;

  // ---------------------------------------------------------------------------
  // 2. Apple Pro Lighting Setup (Titanium White, Electric Azure, Platinum Titanium)
  // ---------------------------------------------------------------------------
  const ambientLight = new THREE.AmbientLight(0x181822, 1.4);
  scene.add(ambientLight);

  // Key Rim Light (Cool Titanium White)
  const keyLight = new THREE.DirectionalLight(0xF5F5F7, 3.8);
  keyLight.position.set(-3.0, 4.5, 3.5);
  scene.add(keyLight);

  // Fill Light (Iconic Apple Azure Blue)
  const azureFillLight = new THREE.DirectionalLight(0x2997FF, 2.8);
  azureFillLight.position.set(3.5, -1.5, 2.0);
  scene.add(azureFillLight);

  // Soft Warm Titanium Ground Accent (Very subtle luxury glint)
  const floorGlint = new THREE.DirectionalLight(0xE5E5EA, 1.1);
  floorGlint.position.set(0, -3.5, -1.0);
  scene.add(floorGlint);

  // Orbiting Specular Point Light (Precision Highlight Sweep)
  const specLight = new THREE.PointLight(0x64D2FF, 2.8, 12, 1.6);
  specLight.position.set(0, 1.2, 2.5);
  scene.add(specLight);

  // ---------------------------------------------------------------------------
  // 3. Root 3D Pavilion Hierarchy
  // ---------------------------------------------------------------------------
  const pavilionRoot = new THREE.Group();
  pavilionRoot.position.set(0, -0.25, 0);
  scene.add(pavilionRoot);

  // ---------------------------------------------------------------------------
  // 4. Floating Architectural Floor Slabs (Dielectric Glass & Laser Edges)
  // ---------------------------------------------------------------------------
  const slabLevels = [-0.9, -0.3, 0.3, 0.9];
  const slabWidth = 2.4;
  const slabDepth = 1.6;
  const slabThickness = 0.04;

  const glassMaterial = new THREE.MeshPhysicalMaterial({
    color: 0xF0F5FF,
    metalness: 0.05,
    roughness: 0.06,
    transmission: 0.88,
    thickness: 0.25,
    ior: 1.52,
    transparent: true,
    opacity: 0.85,
    reflectivity: 0.95,
    clearcoat: 1.0,
    clearcoatRoughness: 0.04
  });

  const laserEdgeMaterial = new THREE.LineBasicMaterial({
    color: 0x64D2FF,
    transparent: true,
    opacity: 0.65,
    linewidth: 1.5
  });

  const titaniumEdgeMaterial = new THREE.LineBasicMaterial({
    color: 0xF5F5F7,
    transparent: true,
    opacity: 0.45
  });

  const slabsGroup = new THREE.Group();
  pavilionRoot.add(slabsGroup);

  slabLevels.forEach((yPos, idx) => {
    // Slabs with varied cantilever modernism
    const scaleFactor = idx === 3 ? 0.82 : (idx === 1 ? 1.05 : 1.0);
    const curW = slabWidth * scaleFactor;
    const curD = slabDepth * scaleFactor;

    const slabGeo = new THREE.BoxGeometry(curW, slabThickness, curD);
    const slabMesh = new THREE.Mesh(slabGeo, glassMaterial);
    slabMesh.position.set(idx === 1 ? 0.08 : (idx === 3 ? -0.06 : 0), yPos, 0);
    slabsGroup.add(slabMesh);

    // Laser edge definition
    const edgesGeo = new THREE.EdgesGeometry(slabGeo);
    const edgesLine = new THREE.LineSegments(edgesGeo, idx === 3 ? laserEdgeMaterial : titaniumEdgeMaterial);
    edgesLine.position.copy(slabMesh.position);
    slabsGroup.add(edgesLine);
  });

  // ---------------------------------------------------------------------------
  // 5. Structural Slender Columns & Central Titanium Core
  // ---------------------------------------------------------------------------
  const colMaterial = new THREE.MeshStandardMaterial({
    color: 0x1E222D,
    metalness: 0.85,
    roughness: 0.22
  });

  const colGeo = new THREE.CylinderGeometry(0.016, 0.016, 1.84, 16);
  const colPositions = [
    [-1.0, 0, -0.65],
    [1.0, 0, -0.65],
    [-1.0, 0, 0.65],
    [1.0, 0, 0.65],
    [0.0, 0, -0.65],
    [0.0, 0, 0.65],
    [-1.0, 0, 0.0],
    [1.0, 0, 0.0]
  ];

  colPositions.forEach(([cx, cy, cz]) => {
    const colMesh = new THREE.Mesh(colGeo, colMaterial);
    colMesh.position.set(cx, 0, cz);
    pavilionRoot.add(colMesh);

    // Column accent lines
    const colEdges = new THREE.LineSegments(new THREE.EdgesGeometry(colGeo), titaniumEdgeMaterial);
    colEdges.position.copy(colMesh.position);
    pavilionRoot.add(colEdges);
  });

  // Central Elevator / Service Core (Dark Titanium Vertical Tower)
  const coreGeo = new THREE.BoxGeometry(0.52, 1.9, 0.45);
  const coreMaterial = new THREE.MeshStandardMaterial({
    color: 0x0D0F16,
    metalness: 0.9,
    roughness: 0.15
  });
  const coreMesh = new THREE.Mesh(coreGeo, coreMaterial);
  coreMesh.position.set(-0.25, 0.02, -0.15);
  pavilionRoot.add(coreMesh);

  const coreEdges = new THREE.LineSegments(
    new THREE.EdgesGeometry(coreGeo),
    new THREE.LineBasicMaterial({ color: 0x2997FF, transparent: true, opacity: 0.4 })
  );
  coreEdges.position.copy(coreMesh.position);
  pavilionRoot.add(coreEdges);

  // ---------------------------------------------------------------------------
  // 6. Kinetic Parametric Facade Louvers / Mullions
  // ---------------------------------------------------------------------------
  const louverGroup = new THREE.Group();
  pavilionRoot.add(louverGroup);

  const louverCount = 18;
  const louverGeo = new THREE.BoxGeometry(0.012, 1.76, 0.08);
  const louverMat = new THREE.MeshPhysicalMaterial({
    color: 0xCBD5E1,
    metalness: 0.4,
    roughness: 0.1,
    transmission: 0.6,
    transparent: true,
    opacity: 0.7
  });

  const louvers = [];
  const spanX = 2.2;
  const stepX = spanX / (louverCount - 1);

  for (let i = 0; i < louverCount; i++) {
    const lx = -spanX / 2 + i * stepX;
    const louverMesh = new THREE.Mesh(louverGeo, louverMat);
    louverMesh.position.set(lx, 0, 0.78);
    louverGroup.add(louverMesh);
    louvers.push({ mesh: louverMesh, phase: i * 0.28 });
  }

  // ---------------------------------------------------------------------------
  // 7. Holographic Polar Ground Grid & Azimuth Compass Dial
  // ---------------------------------------------------------------------------
  const polarGridGroup = new THREE.Group();
  polarGridGroup.position.set(0, -1.02, 0);
  pavilionRoot.add(polarGridGroup);

  // Polar rings
  [0.8, 1.4, 2.0, 2.6].forEach((radius, i) => {
    const ringGeo = new THREE.RingGeometry(radius - 0.003, radius + 0.003, 64);
    const ringMat = new THREE.MeshBasicMaterial({
      color: i === 3 ? 0x2997FF : 0x86868B,
      transparent: true,
      opacity: i === 3 ? 0.45 : 0.15,
      side: THREE.DoubleSide
    });
    const ringMesh = new THREE.Mesh(ringGeo, ringMat);
    ringMesh.rotation.x = Math.PI / 2;
    polarGridGroup.add(ringMesh);
  });

  // Cross axes
  const crossMat = new THREE.LineBasicMaterial({ color: 0x86868B, transparent: true, opacity: 0.2 });
  const axisGeoX = new THREE.BufferGeometry().setFromPoints([
    new THREE.Vector3(-2.8, 0, 0),
    new THREE.Vector3(2.8, 0, 0)
  ]);
  const axisGeoZ = new THREE.BufferGeometry().setFromPoints([
    new THREE.Vector3(0, 0, -2.8),
    new THREE.Vector3(0, 0, 2.8)
  ]);
  polarGridGroup.add(new THREE.Line(axisGeoX, crossMat));
  polarGridGroup.add(new THREE.Line(axisGeoZ, crossMat));

  // Compass tick markers (Cardinal axes N, E, S, W)
  const compassGroup = new THREE.Group();
  polarGridGroup.add(compassGroup);
  for (let a = 0; a < 360; a += 15) {
    const rad = (a * Math.PI) / 180;
    const r1 = 2.58;
    const r2 = a % 90 === 0 ? 2.68 : (a % 45 === 0 ? 2.64 : 2.61);
    const tickGeo = new THREE.BufferGeometry().setFromPoints([
      new THREE.Vector3(Math.cos(rad) * r1, 0, Math.sin(rad) * r1),
      new THREE.Vector3(Math.cos(rad) * r2, 0, Math.sin(rad) * r2)
    ]);
    const tickMat = new THREE.LineBasicMaterial({
      color: a % 90 === 0 ? 0x2997FF : 0x64748B,
      transparent: true,
      opacity: a % 90 === 0 ? 0.8 : 0.35
    });
    compassGroup.add(new THREE.Line(tickGeo, tickMat));
  }

  // ---------------------------------------------------------------------------
  // 8. Laser Lidar Scanning Plane (Ascending / Descending Spatial Slice)
  // ---------------------------------------------------------------------------
  const scanPlaneGeo = new THREE.PlaneGeometry(2.6, 1.8);
  const scanPlaneMat = new THREE.MeshBasicMaterial({
    color: 0x2997FF,
    transparent: true,
    opacity: 0.12,
    side: THREE.DoubleSide,
    blending: THREE.AdditiveBlending,
    depthWrite: false
  });
  const scanPlane = new THREE.Mesh(scanPlaneGeo, scanPlaneMat);
  scanPlane.rotation.x = Math.PI / 2;
  scanPlane.position.y = -0.8;
  pavilionRoot.add(scanPlane);

  // Glowing laser slice boundary
  const scanEdgeGeo = new THREE.EdgesGeometry(scanPlaneGeo);
  const scanEdgeMat = new THREE.LineBasicMaterial({
    color: 0x64D2FF,
    transparent: true,
    opacity: 0.85,
    blending: THREE.AdditiveBlending
  });
  const scanEdge = new THREE.LineSegments(scanEdgeGeo, scanEdgeMat);
  scanEdge.rotation.x = Math.PI / 2;
  scanEdge.position.y = -0.8;
  pavilionRoot.add(scanEdge);

  // ---------------------------------------------------------------------------
  // 9. Floating BIM Parametric Node Sparks (Subtle ambient constellation)
  // ---------------------------------------------------------------------------
  const sparkCount = 64;
  const sparkGeo = new THREE.BufferGeometry();
  const sparkPositions = new Float32Array(sparkCount * 3);
  const sparkColors = new Float32Array(sparkCount * 3);

  for (let i = 0; i < sparkCount; i++) {
    const theta = Math.random() * Math.PI * 2;
    const r = 0.5 + Math.random() * 1.8;
    sparkPositions[i * 3] = Math.cos(theta) * r;
    sparkPositions[i * 3 + 1] = -0.8 + Math.random() * 1.8;
    sparkPositions[i * 3 + 2] = Math.sin(theta) * r;

    // Alternating Apple Azure & Liquid White
    if (i % 3 === 0) {
      sparkColors[i * 3] = 0.16; // R (0x29)
      sparkColors[i * 3 + 1] = 0.59; // G (0x97)
      sparkColors[i * 3 + 2] = 1.0;  // B (0xFF)
    } else {
      sparkColors[i * 3] = 0.95;
      sparkColors[i * 3 + 1] = 0.95;
      sparkColors[i * 3 + 2] = 0.98;
    }
  }

  sparkGeo.setAttribute('position', new THREE.BufferAttribute(sparkPositions, 3));
  sparkGeo.setAttribute('color', new THREE.BufferAttribute(sparkColors, 3));

  const sparkMat = new THREE.PointsMaterial({
    size: 0.035,
    vertexColors: true,
    transparent: true,
    opacity: 0.75,
    blending: THREE.AdditiveBlending,
    depthWrite: false
  });
  const sparks = new THREE.Points(sparkGeo, sparkMat);
  pavilionRoot.add(sparks);

  // ---------------------------------------------------------------------------
  // 10. Inertial Parallax & Smooth Damping
  // ---------------------------------------------------------------------------
  let targetRotationY = -0.35;
  let targetRotationX = 0.12;
  let currentRotationY = -0.35;
  let currentRotationX = 0.12;

  let mouseX = 0;
  let mouseY = 0;

  function onPointerMove(e) {
    const clientX = e.touches ? e.touches[0].clientX : e.clientX;
    const clientY = e.touches ? e.touches[0].clientY : e.clientY;

    mouseX = (clientX / window.innerWidth) * 2 - 1;
    mouseY = -(clientY / window.innerHeight) * 2 + 1;

    targetRotationY = -0.35 + mouseX * 0.42;
    targetRotationX = 0.12 - mouseY * 0.22;
  }

  window.addEventListener('mousemove', onPointerMove, { passive: true });
  window.addEventListener('touchmove', onPointerMove, { passive: true });

  // ---------------------------------------------------------------------------
  // 11. Viewport Visibility Observer (Zero CPU/GPU when out of view)
  // ---------------------------------------------------------------------------
  let isVisible = true;
  if ('IntersectionObserver' in window) {
    const observer = new IntersectionObserver((entries) => {
      isVisible = entries[0].isIntersecting;
    }, { threshold: 0.05 });
    observer.observe(container);
  }

  // ---------------------------------------------------------------------------
  // 12. Main Animation Render Loop
  // ---------------------------------------------------------------------------
  const clock = new THREE.Clock();

  function animate() {
    requestAnimationFrame(animate);

    if (!isVisible) return;

    const elapsedTime = clock.getElapsedTime();

    // Smooth inertial lerp
    currentRotationY += (targetRotationY - currentRotationY) * 0.045;
    currentRotationX += (targetRotationX - currentRotationX) * 0.045;

    // Continuous subtle architectural rotation
    pavilionRoot.rotation.y = currentRotationY + Math.sin(elapsedTime * 0.15) * 0.08;
    pavilionRoot.rotation.x = currentRotationX + Math.cos(elapsedTime * 0.18) * 0.03;

    // Gentle vertical elevation breathing (2 cm)
    pavilionRoot.position.y = -0.25 + Math.sin(elapsedTime * 0.8) * 0.035;

    // Kinetic Louvers: subtle wave modulation
    louvers.forEach(({ mesh, phase }) => {
      mesh.rotation.y = Math.sin(elapsedTime * 1.2 + phase) * 0.35;
    });

    // Lidar scan plane: smooth vertical harmonic sweep between -0.85 and +0.85
    const scanY = Math.sin(elapsedTime * 1.1) * 0.82;
    scanPlane.position.y = scanY;
    scanEdge.position.y = scanY;

    // Slow continuous azimuth compass dial rotation
    compassGroup.rotation.y = elapsedTime * 0.06;

    // Dynamic specular light orbit
    specLight.position.x = Math.cos(elapsedTime * 0.8) * 2.6;
    specLight.position.z = Math.sin(elapsedTime * 0.8) * 2.6;

    // Sparks slow ascent
    const positions = sparkGeo.attributes.position.array;
    for (let i = 0; i < sparkCount; i++) {
      positions[i * 3 + 1] += 0.0025;
      if (positions[i * 3 + 1] > 1.2) {
        positions[i * 3 + 1] = -0.9;
      }
    }
    sparkGeo.attributes.position.needsUpdate = true;

    renderer.render(scene, camera);
  }

  animate();

  // ---------------------------------------------------------------------------
  // 13. Responsive Resize Handler
  // ---------------------------------------------------------------------------
  window.addEventListener('resize', () => {
    width = container.clientWidth || window.innerWidth;
    height = container.clientHeight || 640;

    camera.aspect = width / height;
    camera.updateProjectionMatrix();

    renderer.setSize(width, height);
    renderer.setPixelRatio(Math.min(window.devicePixelRatio || 1, 2));
  }, { passive: true });

})();
