import { TILE_SIZE, SPRAY_RANGE, SPRAY_HALF_ANGLE, MAP_W, MAP_H } from './constants.js';

/**
 * Returns array of {x, y} tile coords inside the spray cone.
 * Uses ray-casting: fires rays across the cone spread.
 */
export function getConeTiles(
  px,
  py,
  aimAngle,
  range = SPRAY_RANGE,
  halfAngle = SPRAY_HALF_ANGLE,
  mapW = MAP_W,
  mapH = MAP_H
) {
  const seen = new Set();
  const tiles = [];
  const rayStep = TILE_SIZE;
  const rayCount = 12;

  for (let i = 0; i <= rayCount; i++) {
    const angle = aimAngle - halfAngle + (2 * halfAngle * i / rayCount);
    const cosA = Math.cos(angle);
    const sinA = Math.sin(angle);

    for (let dist = 0; dist <= range; dist += rayStep) {
      const wx = px + cosA * dist;
      const wy = py + sinA * dist;
      const tx = Math.floor(wx / TILE_SIZE);
      const ty = Math.floor(wy / TILE_SIZE);
      if (tx < 0 || ty < 0 || tx >= mapW || ty >= mapH) break;
      const key = tx * 1000 + ty;
      if (!seen.has(key)) {
        seen.add(key);
        tiles.push({ x: tx, y: ty });
      }
    }
  }
  return tiles;
}
