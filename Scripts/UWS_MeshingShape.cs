using ClipperLib;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using UnityEngine;

using Poly2Tri;

using Path = System.Collections.Generic.List<ClipperLib.IntPoint>;
using Paths = System.Collections.Generic.List<System.Collections.Generic.List<ClipperLib.IntPoint>>;

public class UWS_MeshingShape
{
    private Path _originalGeometry;
    
    private Paths _meshes = new Paths();
    private Dictionary<Path, Path> _holes = new Dictionary<Path, Path>();

    private Path _clip;

    private Clipper _clipper = new Clipper();

    public void SetClip(Path clip)
    {
        _clip = clip;
    }

    public void AddGeometry(Path geometry)
    {
        _originalGeometry = geometry;

        _clipper.Clear();
        _clipper.AddPath(geometry, PolyType.ptSubject, true);
        _clipper.AddPath(_clip, PolyType.ptClip, true);
        _clipper.Execute(ClipType.ctIntersection, _meshes);
    }

    public void Merge(Path another)
    {
        Paths result = new Paths();

        bool check = !PolygonInPolygon(_originalGeometry, another);

        for (int i = 0; i < _meshes.Count; i++)
        {
            if (check)
            {
                Paths res = new Paths();

                /*_clipper.Clear();
                _clipper.AddPath(_meshes[i], PolyType.ptSubject, true);
                _clipper.AddPath(another, PolyType.ptClip, true);
                _clipper.Execute(ClipType.ctIntersection, res);*/

                _clipper.Clear();
                _clipper.AddPath(_meshes[i], PolyType.ptSubject, true);
                _clipper.AddPath(another, PolyType.ptClip, true);
                _clipper.Execute(ClipType.ctDifference, res);

                result.AddRange(res);
            } else
            {
                result.Add(_meshes[i]);
            }

            
        }

        _meshes = result;
    }

    public void Intersect(Path another)
    {
        Paths result = new Paths();

        for (int i = 0; i < _meshes.Count; i++)
        {
            Paths res = new Paths();

            _clipper.Clear();
            _clipper.AddPath(_meshes[i], PolyType.ptSubject, true);
            _clipper.AddPath(another, PolyType.ptClip, true);
            _clipper.Execute(ClipType.ctIntersection, res);

            result.AddRange(res);
        }

        _meshes = result;
    }

    public void IntersectWithCheck(Path another)
    {
        Paths result = new Paths();

        for (int i = 0; i < _meshes.Count; i++)
        {
            if (AnyPointOfPolygonInPolygon(_originalGeometry, another))
            {
                Paths res = new Paths();

                _clipper.Clear();
                _clipper.AddPath(_meshes[i], PolyType.ptSubject, true);
                _clipper.AddPath(another, PolyType.ptClip, true);
                _clipper.Execute(ClipType.ctIntersection, res);

                result.AddRange(res);
            } else
            {
                result.Add(_meshes[i]);
            }
        }

        _meshes = result;
    }

    public void AddHoles(Paths holes)
    {
        Paths mergedHoles = MergeHolesList(holes);

        foreach (Path hole in mergedHoles)
        {

            CutHole(hole);
        }
    }

    public void AddHolesConditioned(Paths holes)
    {
        Paths mergedHoles = MergeHolesList(holes);

        foreach (Path hole in mergedHoles)
        {

            CutHoleConditioned(hole);
        }
    }

    private void CutHoleConditioned(Path hole)
    {
        Paths result = new Paths();

        for (int i = 0; i < _meshes.Count; i++)
        {
            if (AnyPointOfPolygonInPolygon(_meshes[i], hole))
            {
                result.Add(_meshes[i]);
            }
            else
            {
                result.AddRange(CutHoleInPath(_meshes[i], hole));
            }

        }

        _meshes = result;
    }


    public void ReturnGeometry(out Paths meshes, out Dictionary<Path, Path> holes)
    {
        meshes = _meshes;
        holes = _holes;
    }

    private void CutHole(Path hole)
    {
        Paths result = new Paths();

        for (int i = 0; i < _meshes.Count; i++)
        {
            result.AddRange(CutHoleInPath(_meshes[i], hole));
        }

        _meshes = result;
    }

    private Paths CutHoleInPath(Path path, Path hole)
    {
        if (PolygonInPolygon(path, hole))
        {
            return new Paths();
        }

        Paths pathCutted = new Paths();

        _clipper.Clear();
        _clipper.AddPath(path, PolyType.ptSubject, true);
        _clipper.AddPath(hole, PolyType.ptClip, true);
        _clipper.Execute(ClipType.ctDifference, pathCutted);

        
        if (pathCutted.Count == 1)
        {
            return pathCutted;

        } else if (pathCutted.Count == 2)
        {
            if (PolygonInPolygon(pathCutted[0], pathCutted[1]) || PolygonInPolygon(pathCutted[1], pathCutted[0]))
            {
                Paths result = new Paths();
                result.Add(path);

                _holes.Add(path, hole);

                return result;
            }
            else
            {
                Paths result = new Paths();


                foreach (Path p in pathCutted)
                {
                    result.Add(p);
                }

                return result;
            }

        } else
        {
            Paths result = new Paths();


            foreach (Path p in pathCutted)
            {
                result.Add(p);
            }

            return result;
        }


    }

    private Paths MergeHolesList(Paths holes)
    {
        Paths oldHoles = MergeHolesListStep(holes);
        Paths newHoles;

        while (true)
        {
            newHoles = MergeHolesListStep(holes);

            if (newHoles.Count == oldHoles.Count)
            {
                break;
            }
            else
            {
                oldHoles = newHoles;
            }
        }

        return newHoles;
    }

    private Paths MergeHolesListStep(Paths holes)
    {
        Paths mergedHoles = new Paths();

        mergedHoles.Add(holes[0]);

        for (int i = 1; i < holes.Count; i++)
        {
            for (int j = 0; j < mergedHoles.Count; j++)
            {
                Path merged = MergeHoles(holes[i], mergedHoles[j]);

                if (merged == null)
                {
                    mergedHoles.Add(holes[i]);
                }
                else
                {
                    mergedHoles[j] = merged;
                }
            }
        }

        return mergedHoles;
    }

    private Path MergeHoles(Path a, Path b)
    {
        Paths result = new Paths();

        _clipper.Clear();
        _clipper.AddPath(a, PolyType.ptSubject, true);
        _clipper.AddPath(b, PolyType.ptClip, true);
        _clipper.Execute(ClipType.ctUnion, result);

        if (result.Count == 1)
        {
            return result[0];
        } else
        {
            return null;
        }
    }

    private bool PolygonInPolygon(Path a, Path b)
    {
        bool result = true;

        foreach (IntPoint p in a)
        {
            if (Clipper.PointInPolygon(p, b) == 0)
            {
                result = false;
            }
        }

        return result;
    }

    private bool AnyPointOfPolygonInPolygon(Path a, Path b)
    {
        bool result = false;

        foreach (IntPoint p in a)
        {
            if (Clipper.PointInPolygon(p, b) != 0)
            {
                result = true;
            }
        }

        return result;
    }
}
