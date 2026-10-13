import assert from "node:assert/strict";
import test from "node:test";

function mmToFt(mm) {
  return mm / 304.8;
}

function ftToMm(ft) {
  return ft * 304.8;
}

function sqFtToSqM(sqFt) {
  return sqFt * 0.09290304;
}

test("Unit conversions - length (mm to ft and back)", () => {
  assert.equal(mmToFt(304.8), 1.0);
  assert.equal(mmToFt(0), 0);
  assert.equal(mmToFt(3000), 3000 / 304.8);
  assert.equal(ftToMm(1.0), 304.8);
  assert.equal(Math.round(ftToMm(mmToFt(2400))), 2400);
});

test("Unit conversions - area (sq ft to sq m)", () => {
  assert.equal(sqFtToSqM(0), 0);
  assert.equal(Math.round(sqFtToSqM(10.7639) * 100) / 100, 1.0);
});

test("Wall creation argument validation", () => {
  const validateWallArgs = (args) => {
    if (typeof args.startX !== "number" || typeof args.startY !== "number") {
      throw new Error("startX and startY are required numbers");
    }
    if (typeof args.endX !== "number" || typeof args.endY !== "number") {
      throw new Error("endX and endY are required numbers");
    }
    if (args.startX === args.endX && args.startY === args.endY) {
      throw new Error("Wall length cannot be zero (start and end points coincide)");
    }
    if (!args.levelName || typeof args.levelName !== "string") {
      throw new Error("levelName is required");
    }
    return true;
  };

  assert.equal(validateWallArgs({ startX: 0, startY: 0, endX: 1000, endY: 0, levelName: "Niveau 0" }), true);
  assert.throws(() => validateWallArgs({ startX: 0, startY: 0, endX: 0, endY: 0, levelName: "Niveau 0" }), /coincide/);
  assert.throws(() => validateWallArgs({ startX: 0, startY: 0, endX: 1000, endY: 0 }), /levelName/);
});
