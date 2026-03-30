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
        private Mesh             _paintBlobMesh;
        private MaterialPropertyBlock _mpb;
        private Camera           _rtCamera;     // orthographic camera pointed at RT

        // Arena palette — bright neutral floor, dark charcoal walls
        private static readonly Color NeutralColor = new Color(0.97f, 0.97f, 0.97f, 1f);
        private static readonly Color WallColor    = new Color(0.15f, 0.15f, 0.18f, 1f);

        // ── Per-tile world size (quad scale) ──────────────────────────────────
        private static readonly Vector3 _tileScale = new Vector3(
            GameConstants.TILE_SIZE, GameConstants.TILE_SIZE, 1f);
        private static readonly Vector3 _blobScale = new Vector3(
            GameConstants.TILE_SIZE * 2.2f, GameConstants.TILE_SIZE * 2.2f, 1f);

        void Awake()
        {
            _cmd       = new CommandBuffer { name = "TerritoryPaint" };
            _stampMesh = BuildQuadMesh();
            _paintBlobMesh = BuildCircleMesh(24);
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

            // Clear RT to neutral color, then immediately stamp wall tiles
            ClearRT();
            SeedWalls();
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
                    col = NeutralColor;
                else if (tile.ownerIndex == GameConstants.OWNER_WALL)
                    col = WallColor;
                else
                    col = GameConstants.PLAYER_COLORS[tile.ownerIndex];

                _mpb.SetColor("_Color", col);

                bool isPaintOwner = tile.ownerIndex != GameConstants.OWNER_NEUTRAL &&
                                    tile.ownerIndex != GameConstants.OWNER_WALL;
                var mesh = isPaintOwner ? _paintBlobMesh : _stampMesh;
                var scale = isPaintOwner ? _blobScale : _tileScale;
                var matrix = Matrix4x4.TRS(
                    GameConstants.TileToWorld(tile.tx, tile.ty),
                    Quaternion.identity,
                    scale
                );
                _cmd.DrawMesh(mesh, matrix, _stampMaterial, 0, 0, _mpb);
            }

            Graphics.ExecuteCommandBuffer(_cmd);

            _pendingTiles.Clear();
            _hasPending = false;
        }

        // ── Wall seeding ──────────────────────────────────────────────────────
        /// <summary>Stamps all out-of-blob tiles as walls on startup.</summary>
        private void SeedWalls()
        {
            var wallTiles = new System.Collections.Generic.List<PaintedTile>(4096);
            for (int ty = 0; ty < GameConstants.MAP_H; ty++)
            for (int tx = 0; tx < GameConstants.MAP_W; tx++)
            {
                if (GameConstants.IsWall(tx, ty))
                    wallTiles.Add(new PaintedTile(tx, ty, GameConstants.OWNER_WALL));
            }
            SeedTilesImmediate(wallTiles);
        }

        // ── Helpers ───────────────────────────────────────────────────────────
        private void ClearRT()
        {
            var prev = RenderTexture.active;
            RenderTexture.active = _paintRT;
            GL.Clear(true, true, NeutralColor);
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

        private static Mesh BuildCircleMesh(int segments)
        {
            var mesh = new Mesh { name = "PaintBlobCircle" };
            var vertices = new Vector3[segments + 1];
            var uv = new Vector2[segments + 1];
            var triangles = new int[segments * 3];

            vertices[0] = Vector3.zero;
            uv[0] = new Vector2(0.5f, 0.5f);

            for (int i = 0; i < segments; i++)
            {
                float a = i * Mathf.PI * 2f / segments;
                float x = Mathf.Cos(a) * 0.5f;
                float y = Mathf.Sin(a) * 0.5f;
                vertices[i + 1] = new Vector3(x, y, 0f);
                uv[i + 1] = new Vector2(x + 0.5f, y + 0.5f);

                int tri = i * 3;
                triangles[tri] = 0;
                triangles[tri + 1] = i + 1;
                triangles[tri + 2] = (i == segments - 1) ? 1 : i + 2;
            }

            mesh.vertices = vertices;
            mesh.uv = uv;
            mesh.triangles = triangles;
            mesh.RecalculateBounds();
            return mesh;
        }
    }
}
