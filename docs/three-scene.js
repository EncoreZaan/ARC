/**
 * ARC (Architectural Intelligence Bridge) — 3D Hero Experience
 * WebGL / Three.js Interactive Scene
 * - Liquid Metallic / Glass Dome with 3D Simplex noise deformation
 * - Champagne Gold & Midnight Cobalt Fresnel & Specular caustics
 * - Interior 3D Architectural BIM Wireframe Pavilion with data flow pulses
 * - Fluid Molten Ribbon & Ambient Particle Field
 * - Interactive Mouse Parallax, Scroll Elevation & Viewport Intersection Throttling
 */

(function () {
  'use strict';

  // Check WebGL availability
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
  const videoFallback = document.getElementById('hero-video-fallback');
  const toggle3DBtn = document.getElementById('btn-mode-3d');
  const toggleVideoBtn = document.getElementById('btn-mode-video');

  if (!container || !canvas) return;

  if (!isWebGLAvailable() || typeof THREE === 'undefined') {
    if (videoFallback) videoFallback.classList.add('active');
    if (canvas) canvas.style.display = 'none';
    return;
  }

  // ---------------------------------------------------------------------------
  // Scene, Camera, Renderer Setup
  // ---------------------------------------------------------------------------
  const scene = new THREE.Scene();
  scene.fog = new THREE.FogExp2(0x0B080D, 0.09);

  let width = container.clientWidth || window.innerWidth;
  let height = container.clientHeight || 640;

  const camera = new THREE.PerspectiveCamera(36, width / height, 0.1, 100);
  camera.position.set(0, 0.2, 5.6);

  const renderer = new THREE.WebGLRenderer({
    canvas: canvas,
    antialias: true,
    alpha: true,
    powerPreference: 'high-performance'
  });
  renderer.setSize(width, height);
  renderer.setPixelRatio(Math.min(window.devicePixelRatio || 1, 2));
  renderer.toneMapping = THREE.ACESFilmicToneMapping;
  renderer.toneMappingExposure = 1.25;

  // ---------------------------------------------------------------------------
  // Lighting Setup (Prune, Champagne & Cobalt)
  // ---------------------------------------------------------------------------
  const ambientLight = new THREE.AmbientLight(0x2B242C, 0.85);
  scene.add(ambientLight);

  // Key Rim Light (Champagne Gold)
  const keyLight = new THREE.DirectionalLight(0xF5E5C9, 2.8);
  keyLight.position.set(-2.5, 4.0, 3.0);
  scene.add(keyLight);

  // Fill Light (Midnight Cobalt)
  const fillLight = new THREE.DirectionalLight(0x274C8F, 2.2);
  fillLight.position.set(3.5, -2.0, 2.0);
  scene.add(fillLight);

  // Soft Base Accent Light
  const baseLight = new THREE.DirectionalLight(0x503C4B, 1.2);
  baseLight.position.set(0, -4.0, -1.0);
  scene.add(baseLight);

  // Orbiting Specular Point Light (Simulating moving highlight from video)
  const orbitLight = new THREE.PointLight(0xFFF1D8, 3.0, 10, 1.5);
  orbitLight.position.set(0, 1.5, 2.5);
  scene.add(orbitLight);

  // ---------------------------------------------------------------------------
  // Root 3D Object Group (For Entrance Animation & Mouse Tilt)
  // ---------------------------------------------------------------------------
  const orbRoot = new THREE.Group();
  orbRoot.position.set(0, -0.65, 0); // Emerge from bottom of hero
  scene.add(orbRoot);

  // ---------------------------------------------------------------------------
  // 1. The Liquid Metallic / Glass Dome (Outer Shell)
  // ---------------------------------------------------------------------------
  const domeGeometry = new THREE.SphereGeometry(1.68, 96, 96, 0, Math.PI * 2, 0, Math.PI * 0.76);

  // Custom GLSL Shader for Organic Fluid Deformation & Caustics
  const liquidShader = {
    uniforms: {
      uTime: { value: 0.0 },
      uLightPos: { value: new THREE.Vector3(-2.0, 3.5, 3.0) },
      uOrbitLightPos: { value: new THREE.Vector3(0, 1.5, 2.5) },
      uColorBase: { value: new THREE.Color(0x0B080D) },
      uColorPlum: { value: new THREE.Color(0x2B242C) },
      uColorGold: { value: new THREE.Color(0xD7B98C) },
      uColorGoldBright: { value: new THREE.Color(0xFFF3DD) },
      uColorCobalt: { value: new THREE.Color(0x274C8F) },
      uColorNavy: { value: new THREE.Color(0x0E1422) },
      uRoughness: { value: 0.18 },
      uMetalness: { value: 0.92 },
      uOpacity: { value: 0.88 }
    },
    vertexShader: `
      uniform float uTime;
      varying vec3 vNormal;
      varying vec3 vViewPosition;
      varying vec3 vWorldPosition;
      varying vec2 vUv;
      varying float vDisp;

      // 3D Simplex Noise generator
      vec3 mod289(vec3 x) { return x - floor(x * (1.0 / 289.0)) * 289.0; }
      vec4 mod289(vec4 x) { return x - floor(x * (1.0 / 289.0)) * 289.0; }
      vec4 permute(vec4 x) { return mod289(((x*34.0)+1.0)*x); }
      vec4 taylorInvSqrt(vec4 r) { return 1.79284291400159 - 0.85373472095314 * r; }

      float snoise(vec3 v) {
        const vec2 C = vec2(1.0/6.0, 1.0/3.0);
        const vec4 D = vec4(0.0, 0.5, 1.0, 2.0);
        vec3 i  = floor(v + dot(v, C.yyy));
        vec3 x0 = v - i + dot(i, C.xxx);
        vec3 g = step(x0.yzx, x0.xyz);
        vec3 l = 1.0 - g;
        vec3 i1 = min(g.xyz, l.zxy);
        vec3 i2 = max(g.xyz, l.zxy);
        vec3 x1 = x0 - i1 + C.xxx;
        vec3 x2 = x0 - i2 + C.yyy;
        vec3 x3 = x0 - D.yyy;
        i = mod289(i);
        vec4 p = permute(permute(permute(
                  i.z + vec4(0.0, i1.z, i2.z, 1.0))
                + i.y + vec4(0.0, i1.y, i2.y, 1.0))
                + i.x + vec4(0.0, i1.x, i2.x, 1.0));
        float n_ = 0.142857142857;
        vec3 ns = n_ * D.wyz - D.xzx;
        vec4 j = p - 49.0 * floor(p * ns.z * ns.z);
        vec4 x_ = floor(j * ns.z);
        vec4 y_ = floor(j - 7.0 * x_);
        vec4 x = x_ *ns.x + ns.yyyy;
        vec4 y = y_ *ns.x + ns.yyyy;
        vec4 h = 1.0 - abs(x) - abs(y);
        vec4 b0 = vec4(x.xy, y.xy);
        vec4 b1 = vec4(x.zw, y.zw);
        vec4 s0 = floor(b0)*2.0 + 1.0;
        vec4 s1 = floor(b1)*2.0 + 1.0;
        vec4 sh = -step(h, vec4(0.0));
        vec4 a0 = b0.xzyw + s0.xzyw*sh.xxyy;
        vec4 a1 = b1.xzyw + s1.xzyw*sh.zzww;
        vec3 p0 = vec3(a0.xy, h.x);
        vec3 p1 = vec3(a0.zw, h.y);
        vec3 p2 = vec3(a1.xy, h.z);
        vec3 p3 = vec3(a1.zw, h.w);
        vec4 norm = taylorInvSqrt(vec4(dot(p0,p0), dot(p1,p1), dot(p2, p2), dot(p3,p3)));
        p0 *= norm.x; p1 *= norm.y; p2 *= norm.z; p3 *= norm.w;
        vec4 m = max(0.6 - vec4(dot(x0,x0), dot(x1,x1), dot(x2,x2), dot(x3,x3)), 0.0);
        m = m * m;
        return 42.0 * dot(m*m, vec4(dot(p0,x0), dot(p1,x1), dot(p2,x2), dot(p3,x3)));
      }

      void main() {
        vUv = uv;
        vec3 pos = position;

        // Slow organic undulating liquid deformation
        float slowTime = uTime * 0.45;
        float n1 = snoise(pos * 0.85 + vec3(slowTime * 0.35, slowTime * 0.25, 0.0));
        float n2 = snoise(pos * 1.6 - vec3(0.0, slowTime * 0.5, slowTime * 0.3));
        float disp = (n1 * 0.065 + n2 * 0.025);
        vDisp = disp;

        // Apply displacement along normal
        pos += normal * disp;

        vec4 worldPos = modelMatrix * vec4(pos, 1.0);
        vWorldPosition = worldPos.xyz;

        vec4 mvPosition = modelViewMatrix * vec4(pos, 1.0);
        vViewPosition = -mvPosition.xyz;

        // Recompute smooth perturbed normal
        vNormal = normalize(normalMatrix * (normal + vec3(n1 * 0.15, n2 * 0.15, 0.0)));
        gl_Position = projectionMatrix * mvPosition;
      }
    `,
    fragmentShader: `
      uniform float uTime;
      uniform vec3 uLightPos;
      uniform vec3 uOrbitLightPos;
      uniform vec3 uColorBase;
      uniform vec3 uColorPlum;
      uniform vec3 uColorGold;
      uniform vec3 uColorGoldBright;
      uniform vec3 uColorCobalt;
      uniform vec3 uColorNavy;
      uniform float uRoughness;
      uniform float uOpacity;

      varying vec3 vNormal;
      varying vec3 vViewPosition;
      varying vec3 vWorldPosition;
      varying vec2 vUv;
      varying float vDisp;

      void main() {
        vec3 N = normalize(vNormal);
        vec3 V = normalize(vViewPosition);

        // Direction to main light
        vec3 L1 = normalize(uLightPos - vWorldPosition);
        vec3 L_orbit = normalize(uOrbitLightPos - vWorldPosition);

        // Halfway vectors for specular
        vec3 H1 = normalize(L1 + V);
        vec3 H_orbit = normalize(L_orbit + V);

        // Fresnel term (strong at glancing angles)
        float NdotV = max(dot(N, V), 0.0);
        float fresnel = pow(1.0 - NdotV, 2.8);
        float rimTop = pow(1.0 - NdotV, 2.2) * max(N.y * 0.6 + 0.4, 0.0);
        float rimBottom = pow(1.0 - NdotV, 3.2) * max(-N.y * 0.7 + 0.3, 0.0);

        // Specular highlights
        float NdotH1 = max(dot(N, H1), 0.0);
        float spec1 = pow(NdotH1, 38.0);

        float NdotH_orbit = max(dot(N, H_orbit), 0.0);
        float spec_orbit = pow(NdotH_orbit, 64.0);

        // Fluid specular ribbon caustics (matching moving reflections from video)
        float causticSweep = smoothstep(0.82, 0.98, dot(N, L_orbit)) * 0.9;
        float ribbonWave = sin(vWorldPosition.x * 2.5 + vWorldPosition.y * 3.0 + uTime * 1.2);
        float ribbonGlint = smoothstep(0.65, 0.98, ribbonWave * fresnel) * 0.6;

        // Base shading: Deep smoky plum obsidian
        vec3 baseColor = mix(uColorBase, uColorPlum, max(dot(N, L1) * 0.4, 0.0) + vDisp * 0.5);

        // Cobalt / midnight blue interior reflection
        vec3 cobaltReflection = mix(uColorNavy, uColorCobalt, rimBottom + max(dot(N, vec3(0.8, -0.4, 0.2)), 0.0) * 0.7);

        // Champagne Gold specular & rim reflection
        vec3 goldReflection = mix(uColorGold, uColorGoldBright, spec1 + spec_orbit * 0.8 + causticSweep);

        // Composite Color
        vec3 finalColor = baseColor;
        finalColor += cobaltReflection * (rimBottom * 1.8 + 0.15);
        finalColor += goldReflection * (rimTop * 2.2 + spec1 * 1.5 + spec_orbit * 2.4 + ribbonGlint * 1.2);

        // Subsurface warm amber ambient inside
        finalColor += uColorGold * 0.08 * (1.0 - NdotV);

        // Edge fade at dome bottom
        float bottomFade = smoothstep(0.0, 0.25, vUv.y);

        gl_FragColor = vec4(finalColor, uOpacity * bottomFade);
      }
    `,
    transparent: true,
    depthWrite: false,
    side: THREE.DoubleSide
  };

  const liquidMaterial = new THREE.ShaderMaterial(liquidShader);
  const liquidDome = new THREE.Mesh(domeGeometry, liquidMaterial);
  orbRoot.add(liquidDome);

  // ---------------------------------------------------------------------------
  // 2. The Liquid Metallic Orbital Ribbon (Echoing Video Fluid Ring)
  // ---------------------------------------------------------------------------
  const ribbonCurve = new THREE.CatmullRomCurve3([
    new THREE.Vector3(-1.9, 0.2, 0.3),
    new THREE.Vector3(-1.2, 1.2, 0.8),
    new THREE.Vector3(0.3, 1.55, 0.9),
    new THREE.Vector3(1.5, 0.8, 0.4),
    new THREE.Vector3(1.8, -0.4, -0.2),
    new THREE.Vector3(0.5, -0.9, -0.8),
    new THREE.Vector3(-1.1, -0.6, -0.6),
    new THREE.Vector3(-1.9, 0.2, 0.3)
  ]);
  ribbonCurve.closed = true;

  const ribbonGeo = new THREE.TubeGeometry(ribbonCurve, 128, 0.045, 16, true);
  const ribbonMat = new THREE.MeshPhysicalMaterial({
    color: 0x1B1622,
    emissive: 0xD7B98C,
    emissiveIntensity: 0.35,
    metalness: 0.95,
    roughness: 0.12,
    clearcoat: 1.0,
    clearcoatRoughness: 0.05,
    transparent: true,
    opacity: 0.85
  });
  const ribbonMesh = new THREE.Mesh(ribbonGeo, ribbonMat);
  orbRoot.add(ribbonMesh);

  // ---------------------------------------------------------------------------
  // 3. Embedded Architectural BIM Wireframe Pavilion (Inside the Dome)
  // ---------------------------------------------------------------------------
  const archiGroup = new THREE.Group();
  archiGroup.position.set(0, -0.15, 0);
  archiGroup.scale.set(0.72, 0.72, 0.72);
  orbRoot.add(archiGroup);

  // Colors for Architectural Pavilion
  const colorGoldWire = 0xD7B98C;
  const colorGoldDim = 0x8C7452;
  const colorCobaltWire = 0x3E6DB8;

  // Floor Plates (Chamfered Rectangles)
  const plateMat = new THREE.LineBasicMaterial({
    color: colorGoldWire,
    transparent: true,
    opacity: 0.75,
    linewidth: 1
  });

  const plateDimMat = new THREE.LineBasicMaterial({
    color: colorGoldDim,
    transparent: true,
    opacity: 0.45,
    linewidth: 1
  });

  const glassFloorMat = new THREE.MeshBasicMaterial({
    color: 0x1B1624,
    transparent: true,
    opacity: 0.28,
    side: THREE.DoubleSide
  });

  function createFloorPlate(y, w, d, inset = 0) {
    const group = new THREE.Group();

    // Solid faint glass plane
    const planeGeo = new THREE.PlaneGeometry(w, d);
    const plane = new THREE.Mesh(planeGeo, glassFloorMat);
    plane.rotation.x = Math.PI / 2;
    plane.position.y = y;
    group.add(plane);

    // Outer wireframe edge
    const boxGeo = new THREE.BoxGeometry(w, 0.03, d);
    const edges = new THREE.EdgesGeometry(boxGeo);
    const line = new THREE.LineSegments(edges, plateMat);
    line.position.y = y;
    group.add(line);

    // Floor beam grid (BIM structure)
    const gridCols = 4;
    const gridRows = 3;
    const gridGeo = new THREE.BufferGeometry();
    const gridPoints = [];

    for (let i = 1; i < gridCols; i++) {
      const gx = -w / 2 + (w / gridCols) * i;
      gridPoints.push(new THREE.Vector3(gx, y + 0.01, -d / 2));
      gridPoints.push(new THREE.Vector3(gx, y + 0.01, d / 2));
    }
    for (let j = 1; j < gridRows; j++) {
      const gz = -d / 2 + (d / gridRows) * j;
      gridPoints.push(new THREE.Vector3(-w / 2, y + 0.01, gz));
      gridPoints.push(new THREE.Vector3(w / 2, y + 0.01, gz));
    }

    gridGeo.setFromPoints(gridPoints);
    const gridLines = new THREE.LineSegments(gridGeo, plateDimMat);
    group.add(gridLines);

    return group;
  }

  // 3 Architectural Levels
  archiGroup.add(createFloorPlate(-0.6, 2.2, 1.8));   // Ground Level
  archiGroup.add(createFloorPlate(0.05, 1.9, 1.5));   // Level 1 (Mezzanine)
  archiGroup.add(createFloorPlate(0.7, 1.5, 1.2));    // Level 2 (Roof Terrace)

  // Structural Column Grid (Vertical Beams)
  const columnMat = new THREE.LineBasicMaterial({
    color: colorGoldWire,
    transparent: true,
    opacity: 0.65
  });

  const columnCoords = [
    [-0.9, -0.7], [0.9, -0.7], [-0.9, 0.7], [0.9, 0.7],
    [-0.3, -0.7], [0.3, -0.7], [-0.3, 0.7], [0.3, 0.7],
    [-0.9, 0.0], [0.9, 0.0]
  ];

  const columnLinesGeo = new THREE.BufferGeometry();
  const columnPoints = [];

  columnCoords.forEach(([cx, cz]) => {
    // Ground to roof or mezzanine
    const topY = (Math.abs(cx) > 0.8 || Math.abs(cz) > 0.6) ? 0.05 : 0.7;
    columnPoints.push(new THREE.Vector3(cx, -0.6, cz));
    columnPoints.push(new THREE.Vector3(cx, topY, cz));
  });

  columnLinesGeo.setFromPoints(columnPoints);
  const columnMesh = new THREE.LineSegments(columnLinesGeo, columnMat);
  archiGroup.add(columnMesh);

  // Roof Pergola Louvers / Solar Fins (RE2020 Architecture)
  const louverGeo = new THREE.BufferGeometry();
  const louverPoints = [];
  const louverCount = 9;
  for (let k = 0; k < louverCount; k++) {
    const lz = -0.55 + (1.1 / (louverCount - 1)) * k;
    louverPoints.push(new THREE.Vector3(-0.72, 0.78, lz));
    louverPoints.push(new THREE.Vector3(0.72, 0.78, lz));
  }
  louverGeo.setFromPoints(louverPoints);
  const louversMesh = new THREE.LineSegments(louverGeo, plateDimMat);
  archiGroup.add(louversMesh);

  // Glowing Nodes at Structural Joints
  const nodeGeo = new THREE.SphereGeometry(0.022, 8, 8);
  const nodeMat = new THREE.MeshBasicMaterial({
    color: 0xFFF2D9,
    transparent: true,
    opacity: 0.9
  });

  const nodesGroup = new THREE.Group();
  columnCoords.forEach(([cx, cz]) => {
    const n1 = new THREE.Mesh(nodeGeo, nodeMat);
    n1.position.set(cx, -0.6, cz);
    nodesGroup.add(n1);

    const n2 = new THREE.Mesh(nodeGeo, nodeMat);
    n2.position.set(cx, 0.05, cz);
    nodesGroup.add(n2);
  });
  archiGroup.add(nodesGroup);

  // Interior Volumetric Core Glow
  const coreGeo = new THREE.BoxGeometry(0.4, 1.2, 0.4);
  const coreMat = new THREE.MeshBasicMaterial({
    color: 0xD7B98C,
    transparent: true,
    opacity: 0.08
  });
  const coreMesh = new THREE.Mesh(coreGeo, coreMat);
  coreMesh.position.set(0, 0.0, 0);
  archiGroup.add(coreMesh);

  // ---------------------------------------------------------------------------
  // 4. Data Pulse Flow (Metaphor for AI / BIM Live Data Transfer)
  // ---------------------------------------------------------------------------
  const pulseCount = 18;
  const pulseGeometry = new THREE.BufferGeometry();
  const pulsePositions = new Float32Array(pulseCount * 3);
  const pulseProgress = [];
  const pulseTracks = [];

  // Define pathways along columns & beams
  for (let p = 0; p < pulseCount; p++) {
    const colIdx = p % columnCoords.length;
    const [px, pz] = columnCoords[colIdx];
    pulseTracks.push({
      start: new THREE.Vector3(px, -0.6, pz),
      end: new THREE.Vector3(px, 0.7, pz),
      speed: 0.004 + Math.random() * 0.006
    });
    pulseProgress.push(Math.random());
  }

  pulseGeometry.setAttribute('position', new THREE.BufferAttribute(pulsePositions, 3));

  const pulseMaterial = new THREE.PointsMaterial({
    color: 0xFFF8E7,
    size: 0.065,
    transparent: true,
    opacity: 0.85,
    blending: THREE.AdditiveBlending
  });
  const pulsePointsMesh = new THREE.Points(pulseGeometry, pulseMaterial);
  archiGroup.add(pulsePointsMesh);

  // ---------------------------------------------------------------------------
  // 5. Ambient 3D Particle Star Field (Dust & Depth)
  // ---------------------------------------------------------------------------
  const starCount = 280;
  const starGeo = new THREE.BufferGeometry();
  const starPositions = new Float32Array(starCount * 3);
  const starOriginalY = new Float32Array(starCount);

  for (let s = 0; s < starCount; s++) {
    const sx = (Math.random() - 0.5) * 12;
    const sy = (Math.random() - 0.5) * 8;
    const sz = (Math.random() - 0.5) * 8 - 1;
    starPositions[s * 3] = sx;
    starPositions[s * 3 + 1] = sy;
    starPositions[s * 3 + 2] = sz;
    starOriginalY[s] = sy;
  }

  starGeo.setAttribute('position', new THREE.BufferAttribute(starPositions, 3));

  const starMat = new THREE.PointsMaterial({
    color: 0xD7B98C,
    size: 0.035,
    transparent: true,
    opacity: 0.45,
    blending: THREE.AdditiveBlending
  });
  const starPoints = new THREE.Points(starGeo, starMat);
  scene.add(starPoints);

  // ---------------------------------------------------------------------------
  // 6. Smooth Mouse Parallax & Scroll Elevation
  // ---------------------------------------------------------------------------
  let mouseX = 0;
  let mouseY = 0;
  let targetRotationX = 0;
  let targetRotationY = 0;
  let currentRotationX = 0;
  let currentRotationY = 0;
  let scrollProgress = 0;
  let isVisible = true;

  function onPointerMove(e) {
    const clientX = e.clientX || (e.touches && e.touches[0].clientX) || 0;
    const clientY = e.clientY || (e.touches && e.touches[0].clientY) || 0;
    mouseX = (clientX / window.innerWidth) * 2 - 1;
    mouseY = -(clientY / window.innerHeight) * 2 + 1;

    targetRotationY = mouseX * 0.28;
    targetRotationX = -mouseY * 0.18;
  }

  window.addEventListener('mousemove', onPointerMove, { passive: true });
  window.addEventListener('touchmove', onPointerMove, { passive: true });

  // Scroll Tracking for Orb Elevation
  function onScroll() {
    const scrollY = window.scrollY || window.pageYOffset;
    scrollProgress = Math.min(scrollY / window.innerHeight, 1.5);
  }
  window.addEventListener('scroll', onScroll, { passive: true });

  // Window Resize
  function onResize() {
    if (!container) return;
    width = container.clientWidth || window.innerWidth;
    height = container.clientHeight || 640;
    camera.aspect = width / height;
    camera.updateProjectionMatrix();
    renderer.setSize(width, height);
  }
  window.addEventListener('resize', onResize);

  // Intersection Observer to pause rendering when hero is out of view (100% GPU efficiency)
  if ('IntersectionObserver' in window) {
    const observer = new IntersectionObserver((entries) => {
      entries.forEach(entry => {
        isVisible = entry.isIntersecting;
      });
    }, { threshold: 0.05 });
    observer.observe(container);
  }

  // ---------------------------------------------------------------------------
  // 7. Mode Switcher (Interactive 3D WebGL vs Cinematic Video Loop)
  // ---------------------------------------------------------------------------
  if (toggle3DBtn && toggleVideoBtn && videoFallback) {
    toggle3DBtn.addEventListener('click', () => {
      toggle3DBtn.classList.add('active');
      toggleVideoBtn.classList.remove('active');
      canvas.style.display = 'block';
      videoFallback.classList.remove('active');
    });

    toggleVideoBtn.addEventListener('click', () => {
      toggleVideoBtn.classList.add('active');
      toggle3DBtn.classList.remove('active');
      canvas.style.display = 'none';
      videoFallback.classList.add('active');
      const vid = videoFallback.querySelector('video');
      if (vid && vid.paused) {
        vid.play().catch(() => {});
      }
    });
  }

  // ---------------------------------------------------------------------------
  // 8. Animation Loop
  // ---------------------------------------------------------------------------
  const clock = new THREE.Clock();
  let entranceTime = 0;

  function animate() {
    requestAnimationFrame(animate);

    if (!isVisible) return;

    const delta = clock.getDelta();
    const elapsedTime = clock.getElapsedTime();

    // Smooth Entrance (Dome rises gracefully during first 2 seconds)
    if (entranceTime < 2.0) {
      entranceTime += delta;
      const t = Math.min(entranceTime / 2.0, 1.0);
      const ease = 1 - Math.pow(1 - t, 3); // Cubic ease out
      orbRoot.position.y = -1.6 + 0.95 * ease;
      liquidMaterial.uniforms.uOpacity.value = 0.88 * ease;
    }

    // Scroll Elevation & Scale
    const scrollLift = scrollProgress * 0.45;
    const scrollRot = scrollProgress * 0.35;
    orbRoot.position.y = (-0.65 + scrollLift);

    // Mouse Lerp Damping
    currentRotationX += (targetRotationX - currentRotationX) * 0.045;
    currentRotationY += (targetRotationY - currentRotationY) * 0.045;

    orbRoot.rotation.x = currentRotationX + scrollRot * 0.2;
    orbRoot.rotation.y = currentRotationY + elapsedTime * 0.08;

    // Architectural Pavilion Subtle Internal Orbit
    archiGroup.rotation.y = -elapsedTime * 0.04;

    // Metallic Ribbon Breathing & Motion
    ribbonMesh.rotation.y = elapsedTime * 0.12;
    ribbonMesh.rotation.z = Math.sin(elapsedTime * 0.4) * 0.1;

    // Update Shader Uniforms
    liquidMaterial.uniforms.uTime.value = elapsedTime;

    // Orbit Specular Point Light (Dynamic highlight sweep across liquid dome)
    const orbitAngle = elapsedTime * 0.65;
    const orbitRadius = 2.4;
    orbitLight.position.x = Math.cos(orbitAngle) * orbitRadius;
    orbitLight.position.z = Math.sin(orbitAngle) * orbitRadius + 1.2;
    orbitLight.position.y = 1.0 + Math.sin(elapsedTime * 0.8) * 0.5;
    liquidMaterial.uniforms.uOrbitLightPos.value.copy(orbitLight.position);

    // Animate Data Pulses along Structural Columns
    const posAttr = pulseGeometry.attributes.position;
    for (let p = 0; p < pulseCount; p++) {
      const track = pulseTracks[p];
      pulseProgress[p] += track.speed;
      if (pulseProgress[p] > 1.0) pulseProgress[p] = 0;

      const prog = pulseProgress[p];
      const curX = track.start.x + (track.end.x - track.start.x) * prog;
      const curY = track.start.y + (track.end.y - track.start.y) * prog;
      const curZ = track.start.z + (track.end.z - track.start.z) * prog;

      posAttr.setXYZ(p, curX, curY, curZ);
    }
    posAttr.needsUpdate = true;

    // Drifting Star Field
    const starPos = starGeo.attributes.position;
    for (let s = 0; s < starCount; s++) {
      let curY = starPos.getY(s) - 0.003;
      if (curY < -4) curY = 4;
      starPos.setY(s, curY);
    }
    starPos.needsUpdate = true;

    // Render
    renderer.render(scene, camera);
  }

  animate();
})();
