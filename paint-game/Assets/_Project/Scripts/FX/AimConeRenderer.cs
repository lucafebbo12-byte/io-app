// AimConeRenderer.cs — draws filled aim cone as a mesh each frame.
// Uses Graphics.DrawMesh to avoid any LineRenderer GC.
using UnityEngine;

namespace PaintGame
{
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public class AimConeRenderer : MonoBehaviour
    {
        [SerializeField] private Material _coneMaterial;

        private PlayerStats _stats;
        private Mesh        _mesh;
        private WeaponBase  _weapon;

        private const int CONE_SEGMENTS = 12;

        void Awake()
        {
            if (_mesh == null)
            {
                _mesh = new Mesh { name = "AimCone" };
                GetComponent<MeshFilter>().mesh = _mesh;
            }

            if (_coneMaterial == null)
            {
                var shader = Shader.Find("Unlit/Color");
                if (shader != null)
                    _coneMaterial = new Material(shader);
            }

            var mr = GetComponent<MeshRenderer>();
            if (mr != null && _coneMaterial != null)
                mr.sharedMaterial = _coneMaterial;
        }

        public void Init(PlayerStats stats, WeaponBase weapon)
        {
            _stats  = stats;
            _weapon = weapon;
            _mesh   = new Mesh { name = "AimCone" };
            GetComponent<MeshFilter>().mesh = _mesh;
        }

        void LateUpdate()
        {
            if (_stats == null || !_stats.Alive) { GetComponent<MeshRenderer>().enabled = false; return; }

            GetComponent<MeshRenderer>().enabled = _stats.Ink > 0f;
            RebuildMesh();
        }

        private void RebuildMesh()
        {
            float halfAngle = _weapon != null ? _weapon.Config.sprayHalfAngle
                                              : GameConstants.SPRAY_HALF_ANGLE_RAD;
            float range     = _weapon != null ? _weapon.Config.sprayRange
                                              : GameConstants.SPRAY_RANGE;
            float aimAngle  = _stats.AimAngle;
            Vector2 origin  = _stats.WorldPos;

            int vertCount = CONE_SEGMENTS + 2;
            var verts = new Vector3[vertCount];
            var tris  = new int[CONE_SEGMENTS * 3];

            verts[0] = Vector3.zero;  // apex (local space, origin = player pos)

            for (int i = 0; i <= CONE_SEGMENTS; i++)
            {
                float a = aimAngle - halfAngle + 2f * halfAngle * i / CONE_SEGMENTS;
                verts[i + 1] = new Vector3(Mathf.Cos(a) * range, Mathf.Sin(a) * range, 0f);
            }

            for (int i = 0; i < CONE_SEGMENTS; i++)
            {
                tris[i * 3 + 0] = 0;
                tris[i * 3 + 1] = i + 1;
                tris[i * 3 + 2] = i + 2;
            }

            _mesh.Clear();
            _mesh.vertices  = verts;
            _mesh.triangles = tris;
            _mesh.RecalculateBounds();

            // Position the GameObject at player world pos
            transform.position = new Vector3(origin.x, origin.y, 0f);

            // Tint material with ink alpha
            if (_coneMaterial != null)
            {
                float alpha = Mathf.Lerp(0.05f, 0.18f, _stats.Ink / GameConstants.INK_MAX);
                _coneMaterial.color = new Color(
                    _stats.PlayerColor.r, _stats.PlayerColor.g, _stats.PlayerColor.b, alpha);
            }
        }
    }
}
