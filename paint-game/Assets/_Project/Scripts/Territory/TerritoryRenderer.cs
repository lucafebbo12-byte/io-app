// TerritoryRenderer.cs — writes dirty tiles to a 1920×1920 RenderTexture each frame.
// Uses CommandBuffer + MaterialPropertyBlock to stamp quads with zero material allocations.
// Mobile fallback: Texture2D.SetPixels32 path via USE_CPU_PAINT define.
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace PaintGame
{
    [RequireComponent(typeof(TerritoryMap))]
    public class TerritoryRenderer : MonoBehaviour
    {
        [Header("Rendering")]
        [SerializeField] private RenderTexture _paintRT;
        [SerializeField] private Material      _stampMaterial;   // Unlit/Color, Blend One Zero (opaque)
        [SerializeField] private GameObject    _floorQuad;       // the 1920×1920 quad in scene

        // Pending dirty tiles buffered from game logic (may arrive from MatchManager)
        private readonly List<PaintedTile> _pendingTiles = new List<PaintedTile>(512);
        private bool _hasPending;

        private CommandBuffer    _cmd;
        private Mesh             _stampMesh;
        private MaterialPropertyBlock _mpb;
        private Camera           _rtCamera;     // orthographic camera pointed at RT

        // ── Per-tile world size (quad scale) ──────────────────────────────────
        private static readonly Vector3 _tileScale = new Vector3(
            GameConstants.TILE_SIZE, GameConstants.TILE_SIZE, 1f);

        void Awake()
        {
            _cmd       = new CommandBuffer { name = "TerritoryPaint" };
            _stampMesh = BuildQuadMesh();
            _mpb       = new MaterialPropertyBlock();

            // Set the RT on the floor quad's material
            if (_floorQuad != null)
            {
                var mr = _floorQuad.GetComponent<MeshRenderer>();
                if (mr != null) mr.sharedMaterial.SetTexture("_PaintRT", _paintRT);
            }

            // Build an off-screen orthographic camera that renders into _paintRT
            var camGO = new GameObject("RT_Camera");
            camGO.transform.SetParent(transform);
            _rtCamera = camGO.AddComponent<Camera>();
            _rtCamera.orthographic     = true;
            _rtCamera.orthographicSize = GameConstants.WORLD_H * 0.5f;
            _rtCamera.transform.position = new Vector3(
                GameConstants.WORLD_W * 0.5f,
                GameConstants.WORLD_H * 0.5f,
                -10f);
            _rtCamera.clearFlags      = CameraClearFlags.Nothing;
            _rtCamera.cullingMask     = 0;   // renders nothing normally
            _rtCamera.targetTexture   = _paintRT;
            _rtCamera.enabled         = false;  // we call Render() manually

            // Clear RT to transparent (white floor shows through where alpha=0)
            ClearRT();
        }

        void OnDestroy()
        {
            _cmd?.Dispose();
        }

        // ── Public API ────────────────────────────────────────────────────────
        /// <summary>Queue tiles to be stamped this frame. Called by MatchManager after FlushDirty().</summary>
        public void QueueTiles(List<PaintedTile> tiles)
        {
            _pendingTiles.AddRange(tiles);
            _hasPending = true;
        }

        /// <summary>Seed initial tiles without dirtying the game state.</summary>
        public void SeedTilesImmediate(List<PaintedTile> tiles)
        {
            _pendingTiles.AddRange(tiles);
            FlushToRT();
        }

        // ── Per-frame RT write ─────────────────────────────────────────────────
        void LateUpdate()
        {
            if (!_hasPending) return;
            FlushToRT();
        }

        private void FlushToRT()
        {
            if (_pendingTiles.Count == 0) { _hasPending = false; return; }

            _cmd.Clear();
            _cmd.SetRenderTarget(_paintRT);

            foreach (var tile in _pendingTiles)
            {
                Color col;
                if (tile.ownerIndex == GameConstants.OWNER_NEUTRAL)
                    col = new Color(1f, 1f, 1f, 0f);   // transparent = white floor shows
                else if (tile.ownerIndex == GameConstants.OWNER_WALL)
                    col = new Color(0.15f, 0.15f, 0.15f, 1f);
                else
                    col = GameConstants.PLAYER_COLORS[tile.ownerIndex];

                _mpb.SetColor("_Color", col);

                var matrix = Matrix4x4.TRS(
                    GameConstants.TileToWorld(tile.tx, tile.ty),
                    Quaternion.identity,
                    _tileScale
                );
                _cmd.DrawMesh(_stampMesh, matrix, _stampMaterial, 0, 0, _mpb);
            }

            Graphics.ExecuteCommandBuffer(_cmd);

            _pendingTiles.Clear();
            _hasPending = false;
        }

        // ── Helpers ───────────────────────────────────────────────────────────
        private void ClearRT()
        {
            var prev = RenderTexture.active;
            RenderTexture.active = _paintRT;
            GL.Clear(true, true, new Color(1f, 1f, 1f, 0f));
            RenderTexture.active = prev;
        }

        private static Mesh BuildQuadMesh()
        {
            var mesh = new Mesh { name = "StampQuad" };
            mesh.vertices  = new[] {
                new Vector3(-0.5f, -0.5f, 0),
                new Vector3( 0.5f, -0.5f, 0),
                new Vector3( 0.5f,  0.5f, 0),
                new Vector3(-0.5f,  0.5f, 0),
            };
            mesh.uv        = new[] { Vector2.zero, Vector2.right, Vector2.one, Vector2.up };
            mesh.triangles = new[] { 0, 2, 1, 0, 3, 2 };
            mesh.RecalculateBounds();
            return mesh;
        }
    }
}
