/**
 * ARC (Architectural Intelligence Bridge) — 3D Centerpiece
 * High-Precision WebGL / Three.js Interactive Scene
 * - Liquid Metallic Obsidian Dome with 3D Simplex noise deformation
 * - Champagne Gold & Midnight Cobalt Fresnel & Dynamic Specular Caustics
 * - Interior 3D Architectural BIM Wireframe Pavilion with real-time data pulses
 * - Fluid Molten Ribbon & Ambient Particle Field
 * - Cursor & Gyroscope Parallax, Scroll Elevation & Viewport Throttling
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
  // Scene, Camera, Renderer Setup
  // ---------------------------------------------------------------------------
  const scene = new THREE.Scene();
  scene.fog = new THREE.FogExp2(0x080B16, 0.045);

  let width = container.clientWidth || window.innerWidth;
  let height = container.clientHeight || 640;

  const camera = new THREE.PerspectiveCamera(36, width / height, 0.1, 100);
  camera.position.set(0, 0.25, 5.5);

  const renderer = new THREE.WebGLRenderer({
    canvas: canvas,
    antialias: true,
    alpha: true,
    powerPreference: 'high-performance'
  });
  renderer.setSize(width, height);
  renderer.setPixelRatio(Math.min(window.devicePixelRatio || 1, 2));
  renderer.toneMapping = THREE.ACESFilmicToneMapping;
  renderer.toneMappingExposure = 1.45;

  // ---------------------------------------------------------------------------
  // Lighting Setup (Radiant Indigo, Sunburst Gold & Cyber Sapphire)
  // ---------------------------------------------------------------------------
  const ambientLight = new THREE.AmbientLight(0x1E293B, 1.3);
  scene.add(ambientLight);

  // Key Rim Light (Sunburst Gold)
  const keyLight = new THREE.DirectionalLight(0xFFFBEB, 4.4);
  keyLight.position.set(-2.5, 4.2, 3.0);
  scene.add(keyLight);

  // Fill Light (Cyber Sapphire / Azure)
  const fillLight = new THREE.DirectionalLight(0x38BDF8, 3.6);
  fillLight.position.set(3.5, -2.0, 2.0);
  scene.add(fillLight);

  // Base Floor Accent Light (Deep Amethyst / Neon Violet)
  const baseLight = new THREE.DirectionalLight(0x818CF8, 2.2);
  baseLight.position.set(0, -4.0, -1.0);
  scene.add(baseLight);

  // Orbiting Specular Point Light (Dynamic continuous reflection sweep)
  const orbitLight = new THREE.PointLight(0xFFEAA7, 4.6, 14, 1.5);
  orbitLight.position.set(0, 1.5, 2.6);
  scene.add(orbitLight);

  // ---------------------------------------------------------------------------
  // Root 3D Object Group (For Entrance Animation & Parallax Tilt)
  // ---------------------------------------------------------------------------
  const orbRoot = new THREE.Group();
  orbRoot.position.set(0, -0.65, 0); // Emerge from bottom of hero
  scene.add(orbRoot);

  // ---------------------------------------------------------------------------
  // 1. The Liquid Metallic Obsidian Dome (Outer Shell)
  // ---------------------------------------------------------------------------
  const domeGeometry = new THREE.SphereGeometry(1.68, 96, 96, 0, Math.PI * 2, 0, Math.PI * 0.76);

  const liquidShader = {
    uniforms: {
      uTime: { value: 0.0 },
      uLightPos: { value: new THREE.Vector3(-2.2, 3.8, 3.0) },
      uOrbitLightPos: { value: new THREE.Vector3(0, 1.5, 2.6) },
      uColorBase: { value: new THREE.Color(0x0A0E1A) },
      uColorPlum: { value: new THREE.Color(0x1E1B4B) },
      uColorGold: { value: new THREE.Color(0xF59E0B) },
      uColorGoldBright: { value: new THREE.Color(0xFFFBEB) },
      uColorCobalt: { value: new THREE.Color(0x38BDF8) },
      uColorNavy: { value: new THREE.Color(0x0F172A) },
      uRoughness: { value: 0.10 },
      uMetalness: { value: 0.96 },
      uOpacity: { value: 0.92 }
    },
    vertexShader: `
      uniform float uTime;
      varying vec3 vNormal;
      varying vec3 vViewPosition;
      varying vec3 vWorldPosition;
      varying vec2 vUv;
      varying float vDisp;

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

        float slowTime = uTime * 0.40;
        float n1 = snoise(pos * 0.85 + vec3(slowTime * 0.35, slowTime * 0.25, 0.0));
        float n2 = snoise(pos * 1.6 - vec3(0.0, slowTime * 0.5, slowTime * 0.3));
        float disp = (n1 * 0.065 + n2 * 0.025);
        vDisp = disp;

        pos += normal * disp;

        vec4 worldPos = modelMatrix * vec4(pos, 1.0);
        vWorldPosition = worldPos.xyz;

        vec4 mvPosition = modelViewMatrix * vec4(pos, 1.0);
        vViewPosition = -mvPosition.xyz;

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

        vec3 L1 = normalize(uLightPos - vWorldPosition);
        vec3 L_orbit = normalize(uOrbitLightPos - vWorldPosition);

        vec3 H1 = normalize(L1 + V);
        vec3 H_orbit = normalize(L_orbit + V);

        float NdotV = max(dot(N, V), 0.0);
        float fresnel = pow(1.0 - NdotV, 2.8);
        float rimTop = pow(1.0 - NdotV, 2.2) * max(N.y * 0.6 + 0.4, 0.0);
        float rimBottom = pow(1.0 - NdotV, 3.2) * max(-N.y * 0.7 + 0.3, 0.0);

        float NdotH1 = max(dot(N, H1), 0.0);
        float spec1 = pow(NdotH1, 40.0);

        float NdotH_orbit = max(dot(N, H_orbit), 0.0);
        float spec_orbit = pow(NdotH_orbit, 68.0);

        float causticSweep = smoothstep(0.82, 0.98, dot(N, L_orbit)) * 0.95;
        float ribbonWave = sin(vWorldPosition.x * 2.5 + vWorldPosition.y * 3.0 + uTime * 1.2);
        float ribbonGlint = smoothstep(0.65, 0.98, ribbonWave * fresnel) * 0.65;

        vec3 baseColor = mix(uColorBase, uColorPlum, max(dot(N, L1) * 0.4, 0.0) + vDisp * 0.5);
        vec3 cobaltReflection = mix(uColorNavy, uColorCobalt, rimBottom + max(dot(N, vec3(0.8, -0.4, 0.2)), 0.0) * 0.7);
        vec3 goldReflection = mix(uColorGold, uColorGoldBright, spec1 + spec_orbit * 0.8 + causticSweep);

        vec3 finalColor = baseColor;
        finalColor += cobaltReflection * (rimBottom * 1.8 + 0.15);
        finalColor += goldReflection * (rimTop * 2.4 + spec1 * 1.6 + spec_orbit * 2.6 + ribbonGlint * 1.3);
        finalColor += uColorGold * 0.08 * (1.0 - NdotV);

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
  // 2. The Liquid Metallic Orbital Ribbon
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
    color: 0x1E1B4B,
    emissive: 0xF59E0B,
    emissiveIntensity: 0.65,
    metalness: 0.98,
    roughness: 0.08,
    clearcoat: 1.0,
    clearcoatRoughness: 0.03,
    transparent: true,
    opacity: 0.92
  });
  const ribbonMesh = new THREE.Mesh(ribbonGeo, ribbonMat);
  orbRoot.add(ribbonMesh);

  // ---------------------------------------------------------------------------
  // 3. Embedded Architectural BIM Wireframe Pavilion
  // ---------------------------------------------------------------------------
  const archiGroup = new THREE.Group();
  archiGroup.position.set(0, -0.15, 0);
  archiGroup.scale.set(0.72, 0.72, 0.72);
  orbRoot.add(archiGroup);

  const colorGoldWire = 0xFCD34D;
  const colorGoldDim = 0xD97706;

  const plateMat = new THREE.LineBasicMaterial({
    color: colorGoldWire,
    transparent: true,
    opacity: 0.88,
    linewidth: 1
  });

  const plateDimMat = new THREE.LineBasicMaterial({
    color: 0x38BDF8,
    transparent: true,
    opacity: 0.65,
    linewidth: 1
  });

  const glassFloorMat = new THREE.MeshBasicMaterial({
    color: 0x1E293B,
    transparent: true,
    opacity: 0.35,
    side: THREE.DoubleSide
  });

  function createFloorPlate(y, w, d) {
    const group = new THREE.Group();

    const planeGeo = new THREE.PlaneGeometry(w, d);
    const plane = new THREE.Mesh(planeGeo, glassFloorMat);
    plane.rotation.x = Math.PI / 2;
    plane.position.y = y;
    group.add(plane);

    const boxGeo = new THREE.BoxGeometry(w, 0.03, d);
    const edges = new THREE.EdgesGeometry(boxGeo);
    const line = new THREE.LineSegments(edges, plateMat);
    line.position.y = y;
    group.add(line);

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
  archiGroup.add(createFloorPlate(0.05, 1.9, 1.5));   // Level 1
  archiGroup.add(createFloorPlate(0.7, 1.5, 1.2));    // Level 2 (Roof Terrace)

  // Structural Column Grid
  const columnMat = new THREE.LineBasicMaterial({
    color: colorGoldWire,
    transparent: true,
    opacity: 0.68
  });

  const columnCoords = [
    [-0.9, -0.7], [0.9, -0.7], [-0.9, 0.7], [0.9, 0.7],
    [-0.3, -0.7], [0.3, -0.7], [-0.3, 0.7], [0.3, 0.7],
    [-0.9, 0.0], [0.9, 0.0]
  ];

  const columnLinesGeo = new THREE.BufferGeometry();
  const columnPoints = [];

  columnCoords.forEach(([cx, cz]) => {
    const topY = (Math.abs(cx) > 0.8 || Math.abs(cz) > 0.6) ? 0.05 : 0.7;
    columnPoints.push(new THREE.Vector3(cx, -0.6, cz));
    columnPoints.push(new THREE.Vector3(cx, topY, cz));
  });

  columnLinesGeo.setFromPoints(columnPoints);
  const columnMesh = new THREE.LineSegments(columnLinesGeo, columnMat);
  archiGroup.add(columnMesh);

  // Roof Pergola Louvers (RE2020 Architectural Shading)
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
  const nodeGeo = new THREE.SphereGeometry(0.028, 12, 12);
  const nodeMat = new THREE.MeshBasicMaterial({
    color: 0xFFFFFF,
    transparent: true,
    opacity: 1.0
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
    color: 0xF59E0B,
    transparent: true,
    opacity: 0.22
  });
  const coreMesh = new THREE.Mesh(coreGeo, coreMat);
  coreMesh.position.set(0, 0.0, 0);
  archiGroup.add(coreMesh);

  // ---------------------------------------------------------------------------
  // 4. Data Pulse Flow (Real-time BIM telemetry)
  // ---------------------------------------------------------------------------
  const pulseCount = 24;
  const pulseGeometry = new THREE.BufferGeometry();
  const pulsePositions = new Float32Array(pulseCount * 3);
  const pulseProgress = [];
  const pulseTracks = [];

  for (let p = 0; p < pulseCount; p++) {
    const colIdx = p % columnCoords.length;
    const [px, pz] = columnCoords[colIdx];
    pulseTracks.push({
      start: new THREE.Vector3(px, -0.6, pz),
      end: new THREE.Vector3(px, 0.7, pz),
      speed: 0.005 + Math.random() * 0.008
    });
    pulseProgress.push(Math.random());
  }

  pulseGeometry.setAttribute('position', new THREE.BufferAttribute(pulsePositions, 3));

  const pulseMaterial = new THREE.PointsMaterial({
    color: 0x38BDF8,
    size: 0.085,
    transparent: true,
    opacity: 0.95,
    blending: THREE.AdditiveBlending
  });
  const pulsePointsMesh = new THREE.Points(pulseGeometry, pulseMaterial);
  archiGroup.add(pulsePointsMesh);

  // ---------------------------------------------------------------------------
  // 5. Ambient 3D Particle Field
  // ---------------------------------------------------------------------------
  const starCount = 280;
  const starGeo = new THREE.BufferGeometry();
  const starPositions = new Float32Array(starCount * 3);

  for (let s = 0; s < starCount; s++) {
    starPositions[s * 3] = (Math.random() - 0.5) * 12;
    starPositions[s * 3 + 1] = (Math.random() - 0.5) * 8;
    starPositions[s * 3 + 2] = (Math.random() - 0.5) * 8 - 1;
  }

  starGeo.setAttribute('position', new THREE.BufferAttribute(starPositions, 3));

  const starMat = new THREE.PointsMaterial({
    color: 0x38BDF8,
    size: 0.045,
    transparent: true,
    opacity: 0.70,
    blending: THREE.AdditiveBlending
  });
  const starPoints = new THREE.Points(starGeo, starMat);
  scene.add(starPoints);

  // ---------------------------------------------------------------------------
  // 6. Smooth Parallax & Elevation
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

  function onScroll() {
    const scrollY = window.scrollY || window.pageYOffset;
    scrollProgress = Math.min(scrollY / window.innerHeight, 1.5);
  }
  window.addEventListener('scroll', onScroll, { passive: true });

  function onResize() {
    if (!container) return;
    width = container.clientWidth || window.innerWidth;
    height = container.clientHeight || 640;
    camera.aspect = width / height;
    camera.updateProjectionMatrix();
    renderer.setSize(width, height);
  }
  window.addEventListener('resize', onResize);

  if ('IntersectionObserver' in window) {
    const observer = new IntersectionObserver((entries) => {
      entries.forEach(entry => {
        isVisible = entry.isIntersecting;
      });
    }, { threshold: 0.05 });
    observer.observe(container);
  }

  // ---------------------------------------------------------------------------
  // 7. Animation Loop
  // ---------------------------------------------------------------------------
  const clock = new THREE.Clock();
  let entranceTime = 0;

  function animate() {
    requestAnimationFrame(animate);

    if (!isVisible) return;

    const delta = clock.getDelta();
    const elapsedTime = clock.getElapsedTime();

    if (entranceTime < 2.0) {
      entranceTime += delta;
      const t = Math.min(entranceTime / 2.0, 1.0);
      const ease = 1 - Math.pow(1 - t, 3);
      orbRoot.position.y = -1.6 + 0.95 * ease;
      liquidMaterial.uniforms.uOpacity.value = 0.90 * ease;
    }

    const scrollLift = scrollProgress * 0.45;
    const scrollRot = scrollProgress * 0.35;
    orbRoot.position.y = (-0.65 + scrollLift);

    currentRotationX += (targetRotationX - currentRotationX) * 0.045;
    currentRotationY += (targetRotationY - currentRotationY) * 0.045;

    orbRoot.rotation.x = currentRotationX + scrollRot * 0.2;
    orbRoot.rotation.y = currentRotationY + elapsedTime * 0.08;

    archiGroup.rotation.y = -elapsedTime * 0.04;

    ribbonMesh.rotation.y = elapsedTime * 0.12;
    ribbonMesh.rotation.z = Math.sin(elapsedTime * 0.4) * 0.1;

    liquidMaterial.uniforms.uTime.value = elapsedTime;

    const orbitAngle = elapsedTime * 0.65;
    const orbitRadius = 2.4;
    orbitLight.position.x = Math.cos(orbitAngle) * orbitRadius;
    orbitLight.position.z = Math.sin(orbitAngle) * orbitRadius + 1.2;
    orbitLight.position.y = 1.0 + Math.sin(elapsedTime * 0.8) * 0.5;
    liquidMaterial.uniforms.uOrbitLightPos.value.copy(orbitLight.position);

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

    const starPos = starGeo.attributes.position;
    for (let s = 0; s < starCount; s++) {
      let curY = starPos.getY(s) - 0.003;
      if (curY < -4) curY = 4;
      starPos.setY(s, curY);
    }
    starPos.needsUpdate = true;

    renderer.render(scene, camera);
  }

  animate();
})();
