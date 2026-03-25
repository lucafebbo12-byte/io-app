import VirtualJoystickPlugin from 'phaser3-rex-plugins/plugins/virtualjoystick-plugin.js';

export const PLUGIN_KEY = 'rexVirtualJoystick';

export function createJoysticks(scene) {
  const W = scene.scale.width;
  const H = scene.scale.height;

  // LEFT joystick — movement
  const moveStick = scene.plugins.get(PLUGIN_KEY).add(scene, {
    x: W * 0.2,
    y: H * 0.75,
    radius: 60,
    base: scene.add.circle(0, 0, 60, 0x888888, 0.4).setDepth(10),
    thumb: scene.add.circle(0, 0, 30, 0xffffff, 0.7).setDepth(11),
    fixed: true
  });

  // RIGHT joystick — aim + spray
  const aimStick = scene.plugins.get(PLUGIN_KEY).add(scene, {
    x: W * 0.8,
    y: H * 0.75,
    radius: 60,
    base: scene.add.circle(0, 0, 60, 0xff4444, 0.4).setDepth(10),
    thumb: scene.add.circle(0, 0, 30, 0xff8888, 0.7).setDepth(11),
    fixed: true
  });

  return { moveStick, aimStick };
}

export function readJoysticks(moveStick, aimStick) {
  const dx = moveStick.forceX / 60;
  const dy = moveStick.forceY / 60;

  let aimAngle = 0;
  let spraying = false;
  if (aimStick.force > 5) {
    aimAngle = aimStick.angle * (Math.PI / 180); // deg→rad
    spraying = true;
  }

  return { dx, dy, aimAngle, spraying };
}
