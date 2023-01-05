using Eto.Forms;
using Rhino;
using Rhino.Commands;
using Rhino.DocObjects;
using Rhino.Geometry;
using Rhino.Geometry.Intersect;
using Rhino.Input;
using Rhino.Input.Custom;
using Rhino.UI.Runtime;
using System;
using System.Collections.Generic;
using System.IO;
using Command = Rhino.Commands.Command;

namespace ScatteredRenderPeopleTool
{
    public class ScatteredRenderPeopleUtilities
    {
        //empty constructor
        public ScatteredRenderPeopleUtilities() { }

        //people placement point generation for single people
        //overload for surface
        public static List<Point3d> SinglePersonPlacePtGen(Random curRandGen, Surface curPlaceSrf, int pplCount, double pplDist)
        {
            List<Point3d> genSrfPtL = new List<Point3d>();

            while (genSrfPtL.Count < pplCount)
            {
                double curRandSrfUVal = curRandGen.NextDouble();
                double curRandSrfVVal = curRandGen.NextDouble();

                Point3d curRandSrfPt = curPlaceSrf.PointAt(curRandSrfUVal, curRandSrfVVal);

                if (genSrfPtL.Count == 0) genSrfPtL.Add(curRandSrfPt);
                else
                {
                    bool existingPtProxyStatus = false;
                    foreach (Point3d genSrfPt in genSrfPtL)
                    {
                        if (curRandSrfPt.DistanceTo(genSrfPt) < pplDist)
                        {
                            existingPtProxyStatus = true;
                            break;
                        }
                    }

                    if (existingPtProxyStatus == false) genSrfPtL.Add(curRandSrfPt);
                }
            }

            return genSrfPtL;
        }


        //people placement point generation for single people
        //overload for curve
        public static List<Point3d> SinglePersonPlacePtGen(Random curRandGen, Curve curPlaceCrv, double pplDensity, double pplIntervalDist, double pplDist,
            out Curve curRebCrv, out List<double> placeParaL)
        {
            List<Point3d> genCrvPtL = new List<Point3d>();
            List<double> genCrvParaL = new List<double>();

            double[] curCrvDivParaA = curPlaceCrv.DivideByLength(pplIntervalDist, true);

            if (curCrvDivParaA == null)
            {
                curRebCrv = curPlaceCrv.DuplicateCurve();
                placeParaL = genCrvParaL;
                return genCrvPtL;
            }

            int curPplPlaceCount = Convert.ToInt32(Math.Round((pplDensity / 100.0) * Convert.ToDouble(curCrvDivParaA.Length)));

            if (curPplPlaceCount == 0)
            {
                curRebCrv = curPlaceCrv.DuplicateCurve();
                placeParaL = genCrvParaL;
                return genCrvPtL;
            }

            //rebuild curve according to pplDist
            int curCrvRebCount = curPlaceCrv.DivideByLength(pplDist, true).Length;
            NurbsCurve curWorkingPlaceCrv = curPlaceCrv.Rebuild(curCrvRebCount, 3, true);
            curWorkingPlaceCrv.Domain = new Interval(0.0, 1.0);

            int curLoopCounter = 0;

            while (genCrvPtL.Count < curPplPlaceCount)
            {
                double curRandCrvPara = curRandGen.NextDouble();

                Point3d curRandCrvPt = curWorkingPlaceCrv.PointAt(curRandCrvPara);

                if (curLoopCounter < 1.0e5)
                {
                    if (genCrvPtL.Count == 0)
                    {
                        genCrvPtL.Add(curRandCrvPt);
                        genCrvParaL.Add(curRandCrvPara);
                        curLoopCounter++;
                    }
                    else
                    {
                        bool existingPtProxyStatus = false;
                        foreach (Point3d genCrvPt in genCrvPtL)
                        {
                            if (curRandCrvPt.DistanceTo(genCrvPt) < pplDist)
                            {
                                existingPtProxyStatus = true;
                                break;
                            }
                        }

                        if (existingPtProxyStatus == false)
                        {
                            genCrvPtL.Add(curRandCrvPt);
                            genCrvParaL.Add(curRandCrvPara);
                        }

                        curLoopCounter++;
                    }
                }

                else
                {
                    genCrvPtL.Add(curRandCrvPt);
                    genCrvParaL.Add(curRandCrvPara);
                    curLoopCounter++;
                }
            }

            //sort output collections
            genCrvPtL.Sort((Point3d genCrvPt0, Point3d genCrvPt1) =>
            {
                double genPtPara0 = 0.0;
                double genPtPara1 = 0.0;

                curWorkingPlaceCrv.ClosestPoint(genCrvPt0, out genPtPara0);
                curWorkingPlaceCrv.ClosestPoint(genCrvPt1, out genPtPara1);

                return genPtPara0.CompareTo(genPtPara1);
            });
            genCrvParaL.Sort();

            //output
            curRebCrv = curWorkingPlaceCrv;
            placeParaL = genCrvParaL;
            return genCrvPtL;

        }








        //people placement point generation for people in groups
        //overload for surface
        public static List<Point3d> GroupPeoplePlacePtGen(Random curRandGen, Surface curPlaceSrf, int pplCount, double grDist, double pplDist, double bdDist,
            out List<int> calcGrPplCountL, out List<double> calcGrPplRadiusL)
        {

            List<int> curGrPplCountL = new List<int>();

            int curTotalPplNum = 0;

            //keep track of group people count
            int curGrPpl1Count = 0;
            int curGrPpl2Count = 0;
            int curGrPpl3Count = 0;



            int curGrPpl2Target = Convert.ToInt32(Math.Truncate((Convert.ToDouble(pplCount)) * 0.25 / 2.0));
            int curGrPpl3Target = Convert.ToInt32(Math.Truncate((Convert.ToDouble(pplCount)) * 0.25 / 3.0));
            int curGrPpl1Target = pplCount - (2 * curGrPpl2Target + 3 * curGrPpl3Target);

            while (curTotalPplNum < pplCount)
            {
                int curGrPplCount = curRandGen.Next(1, 4);

                if (curGrPplCount == 1)
                {
                    if (curGrPpl1Count < curGrPpl1Target)
                    {
                        curGrPplCountL.Add(curGrPplCount);
                        curTotalPplNum += curGrPplCount;
                        curGrPpl1Count++;
                    }
                }

                if (curGrPplCount == 2)
                {
                    if (curGrPpl2Count < curGrPpl2Target)
                    {
                        curGrPplCountL.Add(curGrPplCount);
                        curTotalPplNum += curGrPplCount;
                        curGrPpl2Count++;
                    }
                }

                if (curGrPplCount == 3)
                {
                    if (curGrPpl3Count < curGrPpl3Target)
                    {
                        curGrPplCountL.Add(curGrPplCount);
                        curTotalPplNum += curGrPplCount;
                        curGrPpl3Count++;
                    }
                }
            }

            //generate surface points to place group people
            //also generate circle radii for place people in groups
            List<Point3d> genSrfPtL = new List<Point3d>();
            List<double> grPplRadiusL = new List<double>();

            //generate surface boundary curve
            Brep curPlaceSrfBr = curPlaceSrf.ToBrep();
            Curve[] curPlaceSrfBrBdA = new Curve[] { };
            Curve[] curPlaceSrfBrBdSegA = curPlaceSrfBr.DuplicateNakedEdgeCurves(true, true);

            if (curPlaceSrfBrBdSegA.Length != 0) curPlaceSrfBrBdA = Curve.JoinCurves(curPlaceSrfBrBdSegA);

            //loop counter
            int curLoopCounter = 0;


            while (genSrfPtL.Count < curGrPplCountL.Count)
            {
                double curRandSrfUVal = curRandGen.NextDouble();
                double curRandSrfVVal = curRandGen.NextDouble();

                Point3d curRandSrfPt = curPlaceSrf.PointAt(curRandSrfUVal, curRandSrfVVal);

                //get current itered group people count
                int curGrPplCount = curGrPplCountL[genSrfPtL.Count];

                if (curLoopCounter < 1.0e5)
                {
                    if (genSrfPtL.Count == 0)
                    {

                        if (curGrPplCount == 1)
                        {
                            //check distance to surface boundary
                            bool bdClosenessStatus = false;
                            if (curPlaceSrfBrBdA.Length != 0)
                            {
                                foreach (Curve bdCrv in curPlaceSrfBrBdA)
                                {
                                    double curSrfBdPara = 0.0;
                                    bdCrv.ClosestPoint(curRandSrfPt, out curSrfBdPara);
                                    if (curRandSrfPt.DistanceTo(bdCrv.PointAt(curSrfBdPara)) < bdDist)
                                    {
                                        bdClosenessStatus = true;
                                        break;
                                    }
                                }
                            }

                            //check normal vector angle at the point
                            Vector3d curRandSrfNormal = curPlaceSrf.NormalAt(curRandSrfUVal, curRandSrfVVal);
                            Vector3d curRandSrfNormal_Proj = curRandSrfNormal;
                            curRandSrfNormal_Proj.Z = 0.0;

                            curRandSrfNormal.Unitize();
                            curRandSrfNormal_Proj.Unitize();
                            double curRandSrfNormalAngle = Math.Acos(curRandSrfNormal * curRandSrfNormal_Proj);

                            if ((bdClosenessStatus == false) && (curRandSrfNormalAngle > (Math.PI / 4.0)) && (curRandSrfNormal.Z > 0))
                            {
                                genSrfPtL.Add(curRandSrfPt);
                                grPplRadiusL.Add(0.0);
                            }

                            curLoopCounter++;
                        }

                        else
                        {
                            double curPplRadius = (pplDist * 0.5) / (Math.Sin(Math.PI / Convert.ToDouble(curGrPplCount)));

                            //check distance to surface boundary
                            bool bdClosenessStatus = false;
                            if (curPlaceSrfBrBdA.Length != 0)
                            {
                                foreach (Curve bdCrv in curPlaceSrfBrBdA)
                                {
                                    double curSrfBdPara = 0.0;
                                    bdCrv.ClosestPoint(curRandSrfPt, out curSrfBdPara);
                                    if (curRandSrfPt.DistanceTo(bdCrv.PointAt(curSrfBdPara)) < (bdDist + curPplRadius))
                                    {
                                        bdClosenessStatus = true;
                                        break;
                                    }
                                }
                            }

                            //check normal vector angle at the point
                            Vector3d curRandSrfNormal = curPlaceSrf.NormalAt(curRandSrfUVal, curRandSrfVVal);
                            Vector3d curRandSrfNormal_Proj = curRandSrfNormal;
                            curRandSrfNormal_Proj.Z = 0.0;

                            curRandSrfNormal.Unitize();
                            curRandSrfNormal_Proj.Unitize();
                            double curRandSrfNormalAngle = Math.Acos(curRandSrfNormal * curRandSrfNormal_Proj);

                            if ((bdClosenessStatus == false) && (curRandSrfNormalAngle > (Math.PI / 4.0)) && (curRandSrfNormal.Z > 0))
                            {
                                genSrfPtL.Add(curRandSrfPt);
                                grPplRadiusL.Add(curPplRadius);
                            }

                            curLoopCounter++;
                        }
                    }
                    else
                    {
                        if (curGrPplCount == 1)
                        {
                            bool existingPtProxyStatus = false;
                            foreach (Point3d selSrfPt in genSrfPtL)
                            {
                                if (curRandSrfPt.DistanceTo(selSrfPt) < grDist)
                                {
                                    existingPtProxyStatus = true;
                                    break;
                                }
                            }

                            //check distance to surface boundary
                            bool bdClosenessStatus = false;
                            if (curPlaceSrfBrBdA.Length != 0)
                            {
                                foreach (Curve bdCrv in curPlaceSrfBrBdA)
                                {
                                    double curSrfBdPara = 0.0;
                                    bdCrv.ClosestPoint(curRandSrfPt, out curSrfBdPara);
                                    if (curRandSrfPt.DistanceTo(bdCrv.PointAt(curSrfBdPara)) < bdDist)
                                    {
                                        bdClosenessStatus = true;
                                        break;
                                    }
                                }
                            }

                            //check normal vector angle at the point
                            Vector3d curRandSrfNormal = curPlaceSrf.NormalAt(curRandSrfUVal, curRandSrfVVal);
                            Vector3d curRandSrfNormal_Proj = curRandSrfNormal;
                            curRandSrfNormal_Proj.Z = 0.0;

                            curRandSrfNormal.Unitize();
                            curRandSrfNormal_Proj.Unitize();
                            double curRandSrfNormalAngle = Math.Acos(curRandSrfNormal * curRandSrfNormal_Proj);

                            if ((existingPtProxyStatus == false) && (bdClosenessStatus == false) && (curRandSrfNormalAngle > (Math.PI / 4.0)) && (curRandSrfNormal.Z > 0))
                            {
                                genSrfPtL.Add(curRandSrfPt);
                                grPplRadiusL.Add(0.0);
                            }

                            curLoopCounter++;
                        }
                        else
                        {
                            //calculate current group people radius
                            double curPplRadius = (pplDist * 0.5) / (Math.Sin(Math.PI / Convert.ToDouble(curGrPplCount)));


                            bool existingPtProxyStatus = false;
                            foreach (Point3d selSrfPt in genSrfPtL)
                            {
                                if (curRandSrfPt.DistanceTo(selSrfPt) < (grDist + curPplRadius))
                                {
                                    existingPtProxyStatus = true;
                                    break;
                                }
                            }

                            //check distance to surface boundary
                            bool bdClosenessStatus = false;
                            if (curPlaceSrfBrBdA.Length != 0)
                            {
                                foreach (Curve bdCrv in curPlaceSrfBrBdA)
                                {
                                    double curSrfBdPara = 0.0;
                                    bdCrv.ClosestPoint(curRandSrfPt, out curSrfBdPara);
                                    if (curRandSrfPt.DistanceTo(bdCrv.PointAt(curSrfBdPara)) < (bdDist + curPplRadius))
                                    {
                                        bdClosenessStatus = true;
                                        break;
                                    }
                                }
                            }

                            //check normal vector angle at the point
                            Vector3d curRandSrfNormal = curPlaceSrf.NormalAt(curRandSrfUVal, curRandSrfVVal);
                            Vector3d curRandSrfNormal_Proj = curRandSrfNormal;
                            curRandSrfNormal_Proj.Z = 0.0;

                            curRandSrfNormal.Unitize();
                            curRandSrfNormal_Proj.Unitize();
                            double curRandSrfNormalAngle = Math.Acos(curRandSrfNormal * curRandSrfNormal_Proj);

                            if ((existingPtProxyStatus == false) && (bdClosenessStatus == false) && (curRandSrfNormalAngle > (Math.PI / 4.0)) && (curRandSrfNormal.Z > 0))
                            {
                                genSrfPtL.Add(curRandSrfPt);
                                grPplRadiusL.Add(curPplRadius);
                            }

                            curLoopCounter++;
                        }
                    }
                }

                else
                {
                    if (curGrPplCount == 1)
                    {
                        genSrfPtL.Add(curRandSrfPt);
                        grPplRadiusL.Add(0.0);
                        curLoopCounter++;
                    }

                    else
                    {
                        //calculate current group people radius
                        double curPplRadius = (pplDist * 0.5) / (Math.Sin(Math.PI / Convert.ToDouble(curGrPplCount)));

                        genSrfPtL.Add(curRandSrfPt);
                        grPplRadiusL.Add(curPplRadius);
                        curLoopCounter++;
                    }
                }



            }


            //output

            calcGrPplCountL = curGrPplCountL;
            calcGrPplRadiusL = grPplRadiusL;


            return genSrfPtL;
        }


        //people placement point generation for people in groups
        //overload for mesh
        public static List<Point3d> GroupPeoplePlacePtGen(Random curRandGen, Mesh curPlaceMesh, int pplCount, double grDist, double pplDist, double bdDist,
            out List<int> calcGrPplCountL, out List<double> calcGrPplRadiusL)
        {
            List<int> curGrPplCountL = new List<int>();

            int curTotalPplNum = 0;

            //keep track of group people count
            int curGrPpl1Count = 0;
            int curGrPpl2Count = 0;
            int curGrPpl3Count = 0;



            int curGrPpl2Target = Convert.ToInt32(Math.Truncate((Convert.ToDouble(pplCount)) * 0.25 / 2.0));
            int curGrPpl3Target = Convert.ToInt32(Math.Truncate((Convert.ToDouble(pplCount)) * 0.25 / 3.0));
            int curGrPpl1Target = pplCount - (2 * curGrPpl2Target + 3 * curGrPpl3Target);

            while (curTotalPplNum < pplCount)
            {
                int curGrPplCount = curRandGen.Next(1, 4);

                if (curGrPplCount == 1)
                {
                    if (curGrPpl1Count < curGrPpl1Target)
                    {
                        curGrPplCountL.Add(curGrPplCount);
                        curTotalPplNum += curGrPplCount;
                        curGrPpl1Count++;
                    }
                }

                if (curGrPplCount == 2)
                {
                    if (curGrPpl2Count < curGrPpl2Target)
                    {
                        curGrPplCountL.Add(curGrPplCount);
                        curTotalPplNum += curGrPplCount;
                        curGrPpl2Count++;
                    }
                }

                if (curGrPplCount == 3)
                {
                    if (curGrPpl3Count < curGrPpl3Target)
                    {
                        curGrPplCountL.Add(curGrPplCount);
                        curTotalPplNum += curGrPplCount;
                        curGrPpl3Count++;
                    }
                }
            }

            //generate mesh vertex points to place group people
            //also generate circle radii for place people in groups
            List<Point3d> genMeshVertPtL = new List<Point3d>();
            List<double> grPplRadiusL = new List<double>();

            //generate place mesh boundary curves
            Polyline[] curMeshNakedBdL = curPlaceMesh.GetNakedEdges();
            //compute and get mesh normals
            curPlaceMesh.Normals.ComputeNormals();
            Rhino.Geometry.Collections.MeshVertexNormalList curPlaceVertNormalL = curPlaceMesh.Normals;

            int curBrMeshVertCount = curPlaceMesh.Vertices.Count;

            //debug
            int curBrMeshNormalCount = curPlaceVertNormalL.Count;

            int curLoopCounter = 0;

            while (genMeshVertPtL.Count < curGrPplCountL.Count)
            {
                int curRandBrMeshVertInd = curRandGen.Next(0, curBrMeshVertCount);

                Point3d curRandBrMeshPt = curPlaceMesh.Vertices[curRandBrMeshVertInd];

                //get current itered group people count
                int curGrPplCount = curGrPplCountL[genMeshVertPtL.Count];

                if (curLoopCounter < (1.0e5))
                {
                    if (genMeshVertPtL.Count == 0)
                    {

                        if (curGrPplCount == 1)
                        {
                            //check distance to surface boundary
                            bool bdClosenessStatus = false;
                            if (curMeshNakedBdL != null)
                            {
                                foreach (Polyline bdPolyLn in curMeshNakedBdL)
                                {
                                    if (curRandBrMeshPt.DistanceTo(bdPolyLn.ClosestPoint(curRandBrMeshPt)) < bdDist)
                                    {
                                        bdClosenessStatus = true;
                                        break;
                                    }
                                }
                            }

                            //check normal vector angle at the point                            
                            Vector3d curRandMeshNormal = curPlaceVertNormalL[curRandBrMeshVertInd];
                            Vector3d curRandMeshNormal_Proj = curRandMeshNormal;
                            curRandMeshNormal_Proj.Z = 0.0;

                            curRandMeshNormal.Unitize();
                            curRandMeshNormal_Proj.Unitize();
                            double curRandMeshNormalAngle = Math.Acos(curRandMeshNormal * curRandMeshNormal_Proj);

                            if ((bdClosenessStatus == false) && (curRandMeshNormalAngle > (Math.PI / 4.0)) && (curRandMeshNormal.Z > 0))
                            {
                                genMeshVertPtL.Add(curRandBrMeshPt);
                                grPplRadiusL.Add(0.0);
                            }

                            curLoopCounter++;
                        }

                        else
                        {
                            double curPplRadius = (pplDist * 0.5) / (Math.Sin(Math.PI / Convert.ToDouble(curGrPplCount)));

                            //check distance to surface boundary
                            bool bdClosenessStatus = false;
                            if (curMeshNakedBdL != null)
                            {
                                foreach (Polyline bdPolyLn in curMeshNakedBdL)
                                {
                                    if (curRandBrMeshPt.DistanceTo(bdPolyLn.ClosestPoint(curRandBrMeshPt)) < (bdDist + curPplRadius))
                                    {
                                        bdClosenessStatus = true;
                                        break;
                                    }
                                }
                            }

                            //check normal vector angle at the point
                            Vector3d curRandMeshNormal = curPlaceVertNormalL[curRandBrMeshVertInd];
                            Vector3d curRandMeshNormal_Proj = curRandMeshNormal;
                            curRandMeshNormal_Proj.Z = 0.0;

                            curRandMeshNormal.Unitize();
                            curRandMeshNormal_Proj.Unitize();
                            double curRandMeshNormalAngle = Math.Acos(curRandMeshNormal * curRandMeshNormal_Proj);

                            if ((bdClosenessStatus == false) && (curRandMeshNormalAngle > (Math.PI / 4.0)) && (curRandMeshNormal.Z > 0))
                            {
                                genMeshVertPtL.Add(curRandBrMeshPt);
                                grPplRadiusL.Add(curPplRadius);
                            }

                            curLoopCounter++;
                        }
                    }
                    else
                    {
                        if (curGrPplCount == 1)
                        {
                            bool existingPtProxyStatus = false;
                            foreach (Point3d selVertPt in genMeshVertPtL)
                            {
                                if (curRandBrMeshPt.DistanceTo(selVertPt) < grDist)
                                {
                                    existingPtProxyStatus = true;
                                    break;
                                }
                            }

                            //check distance to surface boundary
                            bool bdClosenessStatus = false;
                            if (curMeshNakedBdL != null)
                            {
                                foreach (Polyline bdPolyLn in curMeshNakedBdL)
                                {
                                    if (curRandBrMeshPt.DistanceTo(bdPolyLn.ClosestPoint(curRandBrMeshPt)) < bdDist)
                                    {
                                        bdClosenessStatus = true;
                                        break;
                                    }
                                }
                            }

                            //check normal vector angle at the point
                            Vector3d curRandMeshNormal = curPlaceVertNormalL[curRandBrMeshVertInd];
                            Vector3d curRandMeshNormal_Proj = curRandMeshNormal;
                            curRandMeshNormal_Proj.Z = 0.0;

                            curRandMeshNormal.Unitize();
                            curRandMeshNormal_Proj.Unitize();
                            double curRandMeshNormalAngle = Math.Acos(curRandMeshNormal * curRandMeshNormal_Proj);

                            if ((existingPtProxyStatus == false) && (bdClosenessStatus == false) && (curRandMeshNormalAngle > (Math.PI / 4.0)) && (curRandMeshNormal.Z > 0))
                            {
                                genMeshVertPtL.Add(curRandBrMeshPt);
                                grPplRadiusL.Add(0.0);
                            }

                            curLoopCounter++;
                        }
                        else
                        {
                            //calculate current group people radius
                            double curPplRadius = (pplDist * 0.5) / (Math.Sin(Math.PI / Convert.ToDouble(curGrPplCount)));


                            bool existingPtProxyStatus = false;
                            foreach (Point3d selVertPt in genMeshVertPtL)
                            {
                                if (curRandBrMeshPt.DistanceTo(selVertPt) < (grDist + curPplRadius))
                                {
                                    existingPtProxyStatus = true;
                                    break;
                                }
                            }

                            //check distance to surface boundary
                            bool bdClosenessStatus = false;
                            if (curMeshNakedBdL != null)
                            {
                                foreach (Polyline bdPolyLn in curMeshNakedBdL)
                                {
                                    if (curRandBrMeshPt.DistanceTo(bdPolyLn.ClosestPoint(curRandBrMeshPt)) < (bdDist + curPplRadius))
                                    {
                                        bdClosenessStatus = true;
                                        break;
                                    }
                                }
                            }

                            //check normal vector angle at the point
                            Vector3d curRandMeshNormal = curPlaceVertNormalL[curRandBrMeshVertInd];
                            Vector3d curRandMeshNormal_Proj = curRandMeshNormal;
                            curRandMeshNormal_Proj.Z = 0.0;

                            curRandMeshNormal.Unitize();
                            curRandMeshNormal_Proj.Unitize();
                            double curRandMeshNormalAngle = Math.Acos(curRandMeshNormal * curRandMeshNormal_Proj);

                            if ((existingPtProxyStatus == false) && (bdClosenessStatus == false) && (curRandMeshNormalAngle > (Math.PI / 4.0)) && (curRandMeshNormal.Z > 0))
                            {
                                genMeshVertPtL.Add(curRandBrMeshPt);
                                grPplRadiusL.Add(curPplRadius);
                            }

                            curLoopCounter++;
                        }
                    }
                }

                else
                {
                    if (curGrPplCount == 1)
                    {
                        genMeshVertPtL.Add(curRandBrMeshPt);
                        grPplRadiusL.Add(0.0);

                        curLoopCounter++;
                    }

                    else
                    {
                        //calculate current group people radius
                        double curPplRadius = (pplDist * 0.5) / (Math.Sin(Math.PI / Convert.ToDouble(curGrPplCount)));

                        genMeshVertPtL.Add(curRandBrMeshPt);
                        grPplRadiusL.Add(curPplRadius);

                        curLoopCounter++;
                    }
                }

            }


            //output
            calcGrPplCountL = curGrPplCountL;
            calcGrPplRadiusL = grPplRadiusL;

            return genMeshVertPtL;
        }




        //people placement point generation for people in groups
        //overload for mesh and for the walking command
        public static List<Point3d> GroupPeoplePlacePtGen_Walking(Random curRandGen, Mesh curPlaceMesh, int pplCount, double grDist, double pplDist, double bdDist,
            out List<int> calcGrPplCountL, out List<double> calcGrPplRadiusL)
        {
            List<int> curGrPplCountL = new List<int>();

            int curTotalPplNum = 0;

            //keep track of group people count
            int curGrPpl1Count = 0;
            int curGrPpl2Count = 0;



            int curGrPpl2Target = Convert.ToInt32(Math.Truncate((Convert.ToDouble(pplCount)) * 0.2 / 2.0));
            int curGrPpl1Target = pplCount - (2 * curGrPpl2Target);

            while (curTotalPplNum < pplCount)
            {
                int curGrPplCount = curRandGen.Next(1, 3);

                if (curGrPplCount == 1)
                {
                    if (curGrPpl1Count < curGrPpl1Target)
                    {
                        curGrPplCountL.Add(curGrPplCount);
                        curTotalPplNum += curGrPplCount;
                        curGrPpl1Count++;
                    }
                }

                if (curGrPplCount == 2)
                {
                    if (curGrPpl2Count < curGrPpl2Target)
                    {
                        curGrPplCountL.Add(curGrPplCount);
                        curTotalPplNum += curGrPplCount;
                        curGrPpl2Count++;
                    }
                }
            }

            //generate mesh vertex points to place group people
            //also generate circle radii for place people in groups
            List<Point3d> genMeshVertPtL = new List<Point3d>();
            List<double> grPplRadiusL = new List<double>();

            //generate place mesh boundary curves
            Polyline[] curMeshNakedBdL = curPlaceMesh.GetNakedEdges();
            //compute and get mesh normals
            curPlaceMesh.Normals.ComputeNormals();
            Rhino.Geometry.Collections.MeshVertexNormalList curPlaceVertNormalL = curPlaceMesh.Normals;

            int curBrMeshVertCount = curPlaceMesh.Vertices.Count;

            //debug
            int curBrMeshNormalCount = curPlaceVertNormalL.Count;

            int curLoopCounter = 0;

            while (genMeshVertPtL.Count < curGrPplCountL.Count)
            {
                int curRandBrMeshVertInd = curRandGen.Next(0, curBrMeshVertCount);

                Point3d curRandBrMeshPt = curPlaceMesh.Vertices[curRandBrMeshVertInd];

                //get current itered group people count
                int curGrPplCount = curGrPplCountL[genMeshVertPtL.Count];

                if (curLoopCounter < (1.0e5))
                {
                    if (genMeshVertPtL.Count == 0)
                    {

                        if (curGrPplCount == 1)
                        {
                            //check distance to surface boundary
                            bool bdClosenessStatus = false;
                            if (curMeshNakedBdL != null)
                            {
                                foreach (Polyline bdPolyLn in curMeshNakedBdL)
                                {
                                    if (curRandBrMeshPt.DistanceTo(bdPolyLn.ClosestPoint(curRandBrMeshPt)) < bdDist)
                                    {
                                        bdClosenessStatus = true;
                                        break;
                                    }
                                }
                            }

                            //check normal vector angle at the point                            
                            Vector3d curRandMeshNormal = curPlaceVertNormalL[curRandBrMeshVertInd];
                            Vector3d curRandMeshNormal_Proj = curRandMeshNormal;
                            curRandMeshNormal_Proj.Z = 0.0;

                            curRandMeshNormal.Unitize();
                            curRandMeshNormal_Proj.Unitize();
                            double curRandMeshNormalAngle = Math.Acos(curRandMeshNormal * curRandMeshNormal_Proj);

                            if ((bdClosenessStatus == false) && (curRandMeshNormalAngle > (Math.PI / 4.0)) && (curRandMeshNormal.Z > 0))
                            {
                                genMeshVertPtL.Add(curRandBrMeshPt);
                                grPplRadiusL.Add(0.0);
                            }

                            curLoopCounter++;
                        }

                        else
                        {
                            double curPplRadius = (pplDist * 0.5) / (Math.Sin(Math.PI / Convert.ToDouble(curGrPplCount)));

                            //check distance to surface boundary
                            bool bdClosenessStatus = false;
                            if (curMeshNakedBdL != null)
                            {
                                foreach (Polyline bdPolyLn in curMeshNakedBdL)
                                {
                                    if (curRandBrMeshPt.DistanceTo(bdPolyLn.ClosestPoint(curRandBrMeshPt)) < (bdDist + curPplRadius))
                                    {
                                        bdClosenessStatus = true;
                                        break;
                                    }
                                }
                            }

                            //check normal vector angle at the point
                            Vector3d curRandMeshNormal = curPlaceVertNormalL[curRandBrMeshVertInd];
                            Vector3d curRandMeshNormal_Proj = curRandMeshNormal;
                            curRandMeshNormal_Proj.Z = 0.0;

                            curRandMeshNormal.Unitize();
                            curRandMeshNormal_Proj.Unitize();
                            double curRandMeshNormalAngle = Math.Acos(curRandMeshNormal * curRandMeshNormal_Proj);

                            if ((bdClosenessStatus == false) && (curRandMeshNormalAngle > (Math.PI / 4.0)) && (curRandMeshNormal.Z > 0))
                            {
                                genMeshVertPtL.Add(curRandBrMeshPt);
                                grPplRadiusL.Add(curPplRadius);
                            }

                            curLoopCounter++;
                        }
                    }
                    else
                    {
                        if (curGrPplCount == 1)
                        {
                            bool existingPtProxyStatus = false;
                            foreach (Point3d selVertPt in genMeshVertPtL)
                            {
                                if (curRandBrMeshPt.DistanceTo(selVertPt) < grDist)
                                {
                                    existingPtProxyStatus = true;
                                    break;
                                }
                            }

                            //check distance to surface boundary
                            bool bdClosenessStatus = false;
                            if (curMeshNakedBdL != null)
                            {
                                foreach (Polyline bdPolyLn in curMeshNakedBdL)
                                {
                                    if (curRandBrMeshPt.DistanceTo(bdPolyLn.ClosestPoint(curRandBrMeshPt)) < bdDist)
                                    {
                                        bdClosenessStatus = true;
                                        break;
                                    }
                                }
                            }

                            //check normal vector angle at the point
                            Vector3d curRandMeshNormal = curPlaceVertNormalL[curRandBrMeshVertInd];
                            Vector3d curRandMeshNormal_Proj = curRandMeshNormal;
                            curRandMeshNormal_Proj.Z = 0.0;

                            curRandMeshNormal.Unitize();
                            curRandMeshNormal_Proj.Unitize();
                            double curRandMeshNormalAngle = Math.Acos(curRandMeshNormal * curRandMeshNormal_Proj);

                            if ((existingPtProxyStatus == false) && (bdClosenessStatus == false) && (curRandMeshNormalAngle > (Math.PI / 4.0)) && (curRandMeshNormal.Z > 0))
                            {
                                genMeshVertPtL.Add(curRandBrMeshPt);
                                grPplRadiusL.Add(0.0);
                            }

                            curLoopCounter++;
                        }
                        else
                        {
                            //calculate current group people radius
                            double curPplRadius = (pplDist * 0.5) / (Math.Sin(Math.PI / Convert.ToDouble(curGrPplCount)));


                            bool existingPtProxyStatus = false;
                            foreach (Point3d selVertPt in genMeshVertPtL)
                            {
                                if (curRandBrMeshPt.DistanceTo(selVertPt) < (grDist + curPplRadius))
                                {
                                    existingPtProxyStatus = true;
                                    break;
                                }
                            }

                            //check distance to surface boundary
                            bool bdClosenessStatus = false;
                            if (curMeshNakedBdL != null)
                            {
                                foreach (Polyline bdPolyLn in curMeshNakedBdL)
                                {
                                    if (curRandBrMeshPt.DistanceTo(bdPolyLn.ClosestPoint(curRandBrMeshPt)) < (bdDist + curPplRadius))
                                    {
                                        bdClosenessStatus = true;
                                        break;
                                    }
                                }
                            }

                            //check normal vector angle at the point
                            Vector3d curRandMeshNormal = curPlaceVertNormalL[curRandBrMeshVertInd];
                            Vector3d curRandMeshNormal_Proj = curRandMeshNormal;
                            curRandMeshNormal_Proj.Z = 0.0;

                            curRandMeshNormal.Unitize();
                            curRandMeshNormal_Proj.Unitize();
                            double curRandMeshNormalAngle = Math.Acos(curRandMeshNormal * curRandMeshNormal_Proj);

                            if ((existingPtProxyStatus == false) && (bdClosenessStatus == false) && (curRandMeshNormalAngle > (Math.PI / 4.0)) && (curRandMeshNormal.Z > 0))
                            {
                                genMeshVertPtL.Add(curRandBrMeshPt);
                                grPplRadiusL.Add(curPplRadius);
                            }

                            curLoopCounter++;
                        }
                    }
                }

                else
                {
                    if (curGrPplCount == 1)
                    {
                        genMeshVertPtL.Add(curRandBrMeshPt);
                        grPplRadiusL.Add(0.0);

                        curLoopCounter++;
                    }

                    else
                    {
                        //calculate current group people radius
                        double curPplRadius = (pplDist * 0.5) / (Math.Sin(Math.PI / Convert.ToDouble(curGrPplCount)));

                        genMeshVertPtL.Add(curRandBrMeshPt);
                        grPplRadiusL.Add(curPplRadius);

                        curLoopCounter++;
                    }
                }

            }


            //output
            calcGrPplCountL = curGrPplCountL;
            calcGrPplRadiusL = grPplRadiusL;

            return genMeshVertPtL;
        }
    }
}