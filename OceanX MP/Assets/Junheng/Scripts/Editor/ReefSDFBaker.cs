using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace OceanX.BoidsGPU.EditorTools
{
    /// <summary>
    /// Bakes the reef's actual geometry into a distance field the boid simulation can sample, replacing
    /// the 381 hand-placed obstacle boxes.
    ///
    /// WHY: each box was already fitted to one mesh (Rockwork's 112 + Corals' 269 == the 381 affecters),
    /// but a BOX is the wrong shape for reef rock. A well-fitted box around SM_Rock_10 still encloses
    /// 15,238 m3, nearly all of it open water that fish then refuse to swim through. Sampling the real
    /// surface removes that slack — and it is also CHEAPER: the kernel currently walks all 381 affecters
    /// five times per boid (~116M tests/sec at full population), versus a handful of texture samples here.
    ///
    /// WHAT IT PRODUCES: a Texture3D of UNSIGNED distance-to-nearest-reef-surface, in metres, covering the
    /// simulation bounds. Unsigned is deliberate — see the note on hollow shells in BuildOccupancy below.
    /// The gradient of that field points away from the nearest surface, which is exactly the escape
    /// direction the avoidance code needs, and it is continuous everywhere (no "closest obstacle" to flip
    /// between, which was the source of a whole family of artefacts).
    /// </summary>
    public static class ReefSDFBaker
    {
        private const string OutputPath = "Assets/Junheng/Data/Reef/ReefSDF.asset";

        // Covers a ~205m edge at the sampling spacing used below — far larger than anything real here.
        // Purely a backstop against a degenerate triangle hanging the bake; if it ever trips, the bake
        // says so rather than silently leaving a hole.
        private const int MaxStepsPerTriangle = 1024;

        // Meshes gathered from the volume's reef roots, resolved once per bake.
        private static List<MeshFilter> _pendingFilters = new List<MeshFilter>();
        private static int oversizedTriangles;

        // Collects every MeshFilter under the volume's configured reef roots. Authored on the component
        // rather than hardcoded here, so adding solid geometry to the scene is a scene change, not a code
        // change — which is what keeps the field 1:1 with what is actually there.
        private static List<MeshFilter> CollectReefMeshes(ReefSDFVolume volume)
        {
            List<MeshFilter> filters = new List<MeshFilter>();
            if (volume.ReefRoots == null) return filters;
            foreach (Transform root in volume.ReefRoots)
            {
                if (root == null) continue;
                // Inactive renderers are excluded on purpose: something switched off is not in the scene,
                // so it should not be solid either.
                filters.AddRange(root.GetComponentsInChildren<MeshFilter>(false));
            }
            return filters;
        }

        [MenuItem("OceanX/Bake Reef SDF")]
        public static void Bake()
        {
            ReefSDFVolume volume = Object.FindFirstObjectByType<ReefSDFVolume>();
            if (volume == null)
            {
                EditorUtility.DisplayDialog("Bake Reef SDF",
                    "No ReefSDFVolume in the scene. Add one to the Boids_Simulation_GPU object first — it " +
                    "defines the region to bake and holds the result.", "OK");
                return;
            }

            Bounds bounds = volume.Bounds;
            float voxel = Mathf.Max(0.05f, volume.VoxelSize);
            int nx = Mathf.CeilToInt(bounds.size.x / voxel);
            int ny = Mathf.CeilToInt(bounds.size.y / voxel);
            int nz = Mathf.CeilToInt(bounds.size.z / voxel);
            if (nx > 2048 || ny > 2048 || nz > 2048)
            {
                EditorUtility.DisplayDialog("Bake Reef SDF",
                    $"Grid {nx}x{ny}x{nz} exceeds Unity's 2048 Texture3D limit. Increase Voxel Size.", "OK");
                return;
            }

            if (volume.ReefRoots == null || volume.ReefRoots.Length == 0)
            {
                EditorUtility.DisplayDialog("Bake Reef SDF",
                    "ReefSDFVolume has no Reef Roots assigned. Add the hierarchies fish must not swim " +
                    "through (Rockwork, Corals, SM_Landscape) and bake again.", "OK");
                return;
            }

            // Every reef mesh ships with Read/Write disabled, so its triangles are not on the CPU and the
            // bake would silently see an empty reef. Turn readability on just for the bake and put it back
            // afterwards: leaving it on would keep a CPU copy of millions of triangles resident forever,
            // for a bake that runs once.
            List<string> reopened = EnableReadWrite(volume);
            _pendingFilters = CollectReefMeshes(volume);
            oversizedTriangles = 0;

            try
            {
                bool[] solid = BuildOccupancy(bounds, voxel, nx, ny, nz, out int markedTriangles, out int skippedMeshes);
                if (solid == null) return; // cancelled

                float[] distance = ChamferDistance(solid, nx, ny, nz, voxel);
                if (distance == null) return; // cancelled

                // Validate BEFORE restoring readability — this is the only point at which we can compare
                // the field against the geometry it came from. Sampling mesh CENTRES is not enough: a
                // centre sits inside a hollow shell and reads ~0 even when the shell has gaps. Only
                // sampling points ON the triangles catches a hole, which is what fish swim through.
                string coverage = ValidateCoverage(distance, bounds, voxel, nx, ny, nz);

                Texture3D tex = WriteTexture(distance, nx, ny, nz);
                SaveAsset(tex);

                // Record the grid's ACTUAL extent, not the requested one. nx = ceil(size/voxel), so the
                // grid usually covers slightly MORE than was asked for (27.50m for a 27.16m request). The
                // shader maps world -> uvw by dividing by this size, so handing it the requested size
                // stretches the field over the real grid and misaligns it against the rock by up to a
                // voxel — the reef would be subtly in the wrong place.
                Bounds bakedBounds = new Bounds(
                    bounds.min + new Vector3(nx * voxel, ny * voxel, nz * voxel) * 0.5f,
                    new Vector3(nx * voxel, ny * voxel, nz * voxel));
                volume.SetBakedField(tex, bakedBounds, voxel);
                EditorUtility.SetDirty(volume);

                int solidCount = 0;
                for (int i = 0; i < solid.Length; i++) if (solid[i]) solidCount++;

                Debug.Log($"[ReefSDFBaker] Baked {OutputPath}\n" +
                          $"  roots: {string.Join(", ", System.Array.ConvertAll(volume.ReefRoots, r => r == null ? "<none>" : r.name))}\n" +
                          $"  meshes: {_pendingFilters.Count}\n" +
                          $"  grid {nx}x{ny}x{nz} = {solid.Length:N0} voxels @ {voxel}m ({solid.Length * 2 / 1048576f:F1} MB)\n" +
                          $"  triangles rasterised: {markedTriangles:N0}\n" +
                          $"  solid voxels: {solidCount:N0} ({100f * solidCount / solid.Length:F1}% of the volume)\n" +
                          (skippedMeshes > 0
                              ? $"  WARNING {skippedMeshes} mesh(es) skipped as not Read/Write enabled — they are NOT in the field.\n"
                              : "  all reef meshes readable\n") +
                          (oversizedTriangles > 0
                              ? $"  WARNING {oversizedTriangles:N0} triangle(s) hit the {MaxStepsPerTriangle}-step cap and may be under-sampled.\n"
                              : "  no triangle hit the sampling cap\n") +
                          coverage);
            }
            finally
            {
                // Always restore, even if the bake threw or was cancelled — otherwise the project is left
                // carrying the CPU copies.
                RestoreReadWrite(reopened);
                EditorUtility.ClearProgressBar();
            }
        }

        // Turns on Read/Write for every model asset backing the reef, and returns the ones we changed so
        // they can be put back. Returns asset paths, not importers, because the importer objects do not
        // survive the reimport.
        private static List<string> EnableReadWrite(ReefSDFVolume volume)
        {
            HashSet<string> paths = new HashSet<string>();
            foreach (MeshFilter mf in CollectReefMeshes(volume))
            {
                if (mf.sharedMesh == null || mf.sharedMesh.isReadable) continue;
                string path = AssetDatabase.GetAssetPath(mf.sharedMesh);
                if (!string.IsNullOrEmpty(path)) paths.Add(path);
            }

            List<string> changed = new List<string>();
            try
            {
                AssetDatabase.StartAssetEditing();
                foreach (string path in paths)
                {
                    if (AssetImporter.GetAtPath(path) is ModelImporter mi && !mi.isReadable)
                    {
                        mi.isReadable = true;
                        mi.SaveAndReimport();
                        changed.Add(path);
                    }
                }
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
            }
            if (changed.Count > 0)
                Debug.Log($"[ReefSDFBaker] Temporarily enabled Read/Write on {changed.Count} model asset(s) for the bake.");
            return changed;
        }

        private static void RestoreReadWrite(List<string> paths)
        {
            if (paths == null || paths.Count == 0) return;
            try
            {
                AssetDatabase.StartAssetEditing();
                foreach (string path in paths)
                {
                    if (AssetImporter.GetAtPath(path) is ModelImporter mi && mi.isReadable)
                    {
                        mi.isReadable = false;
                        mi.SaveAndReimport();
                    }
                }
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
            }
            Debug.Log($"[ReefSDFBaker] Restored Read/Write to off on {paths.Count} model asset(s).");
        }

        /// <summary>
        /// Marks every voxel that contains reef surface, by point-sampling each triangle at sub-voxel
        /// density and stamping the voxel the sample lands in.
        ///
        /// This gives a SHELL, not a filled solid, because these meshes are hollow. That is fine, and is
        /// why the field is unsigned: a fish cannot get inside a hollow rock without first crossing the
        /// shell, and the shell is a whole voxel thick, so "inside" is unreachable in practice. Trying to
        /// sign the field properly would mean inside/outside tests on 381 non-watertight meshes, which is
        /// both much harder and buys nothing here.
        /// </summary>
        private static bool[] BuildOccupancy(Bounds bounds, float voxel, int nx, int ny, int nz,
                                             out int triangleCount, out int skippedMeshes)
        {
            bool[] solid = new bool[nx * ny * nz];
            triangleCount = 0;
            skippedMeshes = 0;

            List<MeshFilter> filters = _pendingFilters;
            Vector3 min = bounds.min;
            float inv = 1f / voxel;
            // Sample spacing must be under half a voxel or a triangle can straddle one without ever
            // landing a sample in it, leaving a hole in the reef that fish would swim straight through.
            float sampleSpacing = voxel * 0.4f;

            for (int f = 0; f < filters.Count; f++)
            {
                if ((f & 7) == 0 && EditorUtility.DisplayCancelableProgressBar("Baking Reef SDF",
                        $"Rasterising reef geometry ({f}/{filters.Count} meshes)", 0.5f * f / filters.Count))
                    return null;

                Mesh mesh = filters[f].sharedMesh;
                if (mesh == null) continue;
                if (!mesh.isReadable) { skippedMeshes++; continue; }

                Transform tr = filters[f].transform;
                Vector3[] verts = mesh.vertices;
                int[] idx = mesh.triangles;

                // To world space once, rather than per triangle.
                for (int i = 0; i < verts.Length; i++) verts[i] = tr.TransformPoint(verts[i]);

                for (int t = 0; t < idx.Length; t += 3)
                {
                    Vector3 a = verts[idx[t]], b = verts[idx[t + 1]], c = verts[idx[t + 2]];
                    triangleCount++;

                    Vector3 ab = b - a, ac = c - a;
                    // Steps must follow the triangle's actual size. A previous version clamped this to 64,
                    // which at this spacing only covers a ~13m edge — larger triangles (a terrain, a big
                    // rock face) were then sampled coarser than a voxel and could pass straight BETWEEN
                    // voxels, leaving holes fish swim through. Total work is bounded by surface area /
                    // spacing^2 regardless, so a high cap costs nothing in practice; it only exists so a
                    // single degenerate triangle cannot hang the bake.
                    int steps = Mathf.CeilToInt(Mathf.Max(ab.magnitude, ac.magnitude) / sampleSpacing);
                    if (steps > MaxStepsPerTriangle) { steps = MaxStepsPerTriangle; oversizedTriangles++; }
                    steps = Mathf.Max(steps, 1);

                    for (int i = 0; i <= steps; i++)
                    {
                        for (int j = 0; j <= steps - i; j++)
                        {
                            Vector3 p = a + ab * ((float)i / steps) + ac * ((float)j / steps);
                            int vxi = (int)((p.x - min.x) * inv);
                            int vyi = (int)((p.y - min.y) * inv);
                            int vzi = (int)((p.z - min.z) * inv);
                            if (vxi < 0 || vyi < 0 || vzi < 0 || vxi >= nx || vyi >= ny || vzi >= nz) continue;
                            solid[vxi + nx * (vyi + ny * vzi)] = true;
                        }
                    }
                }
            }
            return solid;
        }

        /// <summary>
        /// Unsigned distance from every voxel to the nearest solid voxel, via a two-pass chamfer transform.
        /// Exact Euclidean distance would need a jump-flood; chamfer is within a few percent, which is far
        /// finer than the box approximation it replaces and costs one pass each way.
        /// </summary>
        private static float[] ChamferDistance(bool[] solid, int nx, int ny, int nz, float voxel)
        {
            float[] d = new float[solid.Length];
            const float Big = 1e9f;
            for (int i = 0; i < d.Length; i++) d[i] = solid[i] ? 0f : Big;

            // Chamfer weights for a 3x3x3 neighbourhood, in voxels.
            float w1 = 1f, w2 = Mathf.Sqrt(2f), w3 = Mathf.Sqrt(3f);

            for (int pass = 0; pass < 2; pass++)
            {
                bool forward = pass == 0;
                if (EditorUtility.DisplayCancelableProgressBar("Baking Reef SDF",
                        forward ? "Distance transform (forward)" : "Distance transform (backward)",
                        0.5f + 0.25f * pass))
                    return null;

                for (int zi = 0; zi < nz; zi++)
                {
                    int z = forward ? zi : nz - 1 - zi;
                    for (int yi = 0; yi < ny; yi++)
                    {
                        int y = forward ? yi : ny - 1 - yi;
                        for (int xi = 0; xi < nx; xi++)
                        {
                            int x = forward ? xi : nx - 1 - xi;
                            int id = x + nx * (y + ny * z);
                            float best = d[id];
                            if (best == 0f) continue;

                            // Only look at neighbours already finalised by this pass's sweep direction.
                            for (int dz = -1; dz <= 1; dz++)
                                for (int dy = -1; dy <= 1; dy++)
                                    for (int dx = -1; dx <= 1; dx++)
                                    {
                                        if (dx == 0 && dy == 0 && dz == 0) continue;
                                        int order = dz != 0 ? dz : (dy != 0 ? dy : dx);
                                        if (forward ? order > 0 : order < 0) continue;

                                        int px = x + dx, py = y + dy, pz = z + dz;
                                        if (px < 0 || py < 0 || pz < 0 || px >= nx || py >= ny || pz >= nz) continue;

                                        int steps = Mathf.Abs(dx) + Mathf.Abs(dy) + Mathf.Abs(dz);
                                        float w = steps == 1 ? w1 : (steps == 2 ? w2 : w3);
                                        float cand = d[px + nx * (py + ny * pz)] + w;
                                        if (cand < best) best = cand;
                                    }
                            d[id] = best;
                        }
                    }
                }
            }

            // Voxels -> metres.
            for (int i = 0; i < d.Length; i++) d[i] = Mathf.Min(d[i] * voxel, 1000f);
            return d;
        }

        /// <summary>
        /// Checks the finished field against the geometry it was built from, by sampling points ON the
        /// source triangles and asking what distance the field reports there. A surface point must read as
        /// solid; anything reading further than a voxel away is a HOLE, and a hole is a place fish will
        /// swim straight through solid-looking rock.
        ///
        /// This has to run while the meshes are still readable, i.e. before RestoreReadWrite.
        /// </summary>
        private static string ValidateCoverage(float[] distance, Bounds bounds, float voxel, int nx, int ny, int nz)
        {
            Vector3 min = bounds.min;
            float inv = 1f / voxel;
            System.Random rnd = new System.Random(12345); // fixed seed: two bakes of the same reef compare directly

            int tested = 0, solidHit = 0, withinVoxel = 0, holes = 0, outsideRegion = 0;
            float worst = 0f; string worstMesh = "";

            for (int f = 0; f < _pendingFilters.Count; f++)
            {
                Mesh mesh = _pendingFilters[f].sharedMesh;
                if (mesh == null || !mesh.isReadable) continue;
                Transform tr = _pendingFilters[f].transform;
                Vector3[] verts = mesh.vertices;
                int[] idx = mesh.triangles;

                // Sample up to ~60 triangles per mesh — enough to expose a systematic hole without
                // doubling the bake time.
                int triCount = idx.Length / 3;
                int step = Mathf.Max(1, triCount / 60);
                for (int t = 0; t < triCount; t += step)
                {
                    Vector3 a = tr.TransformPoint(verts[idx[t * 3]]);
                    Vector3 b = tr.TransformPoint(verts[idx[t * 3 + 1]]);
                    Vector3 c = tr.TransformPoint(verts[idx[t * 3 + 2]]);

                    // A uniform random point on the triangle.
                    float u = (float)rnd.NextDouble(), w = (float)rnd.NextDouble();
                    if (u + w > 1f) { u = 1f - u; w = 1f - w; }
                    Vector3 p = a + (b - a) * u + (c - a) * w;

                    int vxi = (int)((p.x - min.x) * inv);
                    int vyi = (int)((p.y - min.y) * inv);
                    int vzi = (int)((p.z - min.z) * inv);
                    // Geometry outside the bake region is not in the field at all. Silently skipping it
                    // would let a bake that missed whole rocks report a clean bill of health, so count it.
                    if (vxi < 0 || vyi < 0 || vzi < 0 || vxi >= nx || vyi >= ny || vzi >= nz) { outsideRegion++; continue; }

                    float d = distance[vxi + nx * (vyi + ny * vzi)];
                    tested++;
                    if (d <= 0.001f) solidHit++;
                    else if (d <= voxel) withinVoxel++;
                    else
                    {
                        holes++;
                        if (d > worst) { worst = d; worstMesh = _pendingFilters[f].gameObject.name; }
                    }
                }
            }

            if (tested == 0) return "  coverage: no surface points inside the bake region — check the volume's Centre/Size.";

            float holePct = 100f * holes / tested;
            float outsidePct = 100f * outsideRegion / (tested + outsideRegion);

            string verdict = holes == 0
                ? "  coverage: PASS — every sampled surface point INSIDE the region reads as solid."
                : $"  coverage: FAIL — {holePct:F2}% of sampled surface points are HOLES (worst {worst:F2}m away, on '{worstMesh}').\n" +
                  "    Fish will swim through geometry there. Reduce Voxel Size, or look for oversized triangles.";

            // Reported separately from holes: this is not a hole in the field, it is geometry the field
            // was never asked about. Fish cannot reach outside the simulation bounds, so some is expected
            // and harmless — but if it is high, the region is probably not covering the reef.
            string outside = outsideRegion == 0
                ? "  region: all sampled geometry lies inside the baked region."
                : $"  region: {outsideRegion:N0} sampled surface points ({outsidePct:F1}%) lie OUTSIDE the baked region and are\n" +
                  "    therefore not solid at all. Expected for reef below the seabed or beyond the play area;\n" +
                  "    if fish can reach it, grow Size/Padding.";

            return $"  coverage: sampled {tested:N0} points ON the real surfaces — solid {solidHit:N0}, within a voxel {withinVoxel:N0}, holes {holes:N0}\n"
                   + verdict + "\n" + outside;
        }

        private static Texture3D WriteTexture(float[] distance, int nx, int ny, int nz)
        {
            Texture3D tex = new Texture3D(nx, ny, nz, TextureFormat.RHalf, false)
            {
                // Bilinear so the sampled distance — and therefore the gradient we derive the escape
                // direction from — is smooth across voxel boundaries rather than stepping.
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                name = "ReefSDF"
            };
            Color[] px = new Color[distance.Length];
            for (int i = 0; i < distance.Length; i++) px[i] = new Color(distance[i], 0f, 0f, 0f);
            tex.SetPixels(px);
            // Keep the CPU copy (makeNoLongerReadable: false). It is only 2.8 MB, it is what gets
            // serialised into the asset, and it lets the field be inspected/validated after baking —
            // discarding it makes the bake unverifiable and risks writing an asset with no pixel data.
            tex.Apply(false, false);
            return tex;
        }

        private static void SaveAsset(Texture3D tex)
        {
            string dir = System.IO.Path.GetDirectoryName(OutputPath);
            if (!AssetDatabase.IsValidFolder(dir)) System.IO.Directory.CreateDirectory(dir);
            Texture3D existing = AssetDatabase.LoadAssetAtPath<Texture3D>(OutputPath);
            if (existing != null) AssetDatabase.DeleteAsset(OutputPath);
            AssetDatabase.CreateAsset(tex, OutputPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }
    }
}
