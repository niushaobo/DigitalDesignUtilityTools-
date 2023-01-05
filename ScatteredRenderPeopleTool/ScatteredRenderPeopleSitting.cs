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
    public class ScatteredRenderPeopleSitting : Command
    {
        public ScatteredRenderPeopleSitting()
        {
            Instance = this;
        }

        ///<summary>The only instance of the MyCommand command.</summary>
        public static ScatteredRenderPeopleSitting Instance { get; private set; }

        public override string EnglishName => "ScatteredRenderPeopleSitting";

        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {

            //starting messages
            RhinoApp.WriteLine("The {0} command will populate render contents for selected geometry right now.", EnglishName);

            //read external file
            //please replace the "curSelFilePath" with your designated file path
            string curSelFilePath = @"\\***\***.3dm";

            RhinoApp.WriteLine($"{curSelFilePath}");

            Rhino.FileIO.File3dm curSel3dm = new Rhino.FileIO.File3dm();

            try
            {
                curSel3dm = Rhino.FileIO.File3dm.Read(curSelFilePath);
            }

            catch (Exception ex)
            {
                return Rhino.Commands.Result.Failure;
            }

            /////get unit system of current document
            Rhino.UnitSystem curDocUnitSys = doc.ModelUnitSystem;

            //import instance definitions from file
            //get names of all the instance definitions in current document
            List<string> insDefNameL_curDoc = new List<string>();
            foreach (InstanceDefinition curInsDef in doc.InstanceDefinitions) insDefNameL_curDoc.Add(curInsDef.Name);

            //created designated layer in current doc
            List<string> layerNameL_curDoc = new List<string>();
            foreach (Rhino.DocObjects.Layer curDocLayer in doc.Layers) layerNameL_curDoc.Add(curDocLayer.Name);
            int curPplLayerInd = 0;
            if (layerNameL_curDoc.Contains("Render_People") == false) curPplLayerInd = doc.Layers.Add("Render_People", System.Drawing.Color.Red);
            else curPplLayerInd = layerNameL_curDoc.IndexOf("Render_People");

            //record added instance definition indices in current document
            List<int> curAddedInsIndL = new List<int>();

            foreach (InstanceDefinitionGeometry selInsDefGeo in curSel3dm.AllInstanceDefinitions)
            {
                RhinoApp.WriteLine($"processing {selInsDefGeo.Name}");

                //see if current instance definition's name already exists in current document
                if (insDefNameL_curDoc.Contains(selInsDefGeo.Name) == true) curAddedInsIndL.Add(insDefNameL_curDoc.IndexOf(selInsDefGeo.Name));


                else
                {
                    Guid[] selInsIdA = selInsDefGeo.GetObjectIds();

                    List<GeometryBase> selInsGeoL = new List<GeometryBase>();
                    List<Rhino.DocObjects.ObjectAttributes> selInsAttrL = new List<Rhino.DocObjects.ObjectAttributes>();

                    foreach (Guid selId in selInsIdA)
                    {
                        Rhino.FileIO.File3dmObject selInsObj = curSel3dm.Objects.FindId(selId);


                        selInsGeoL.Add(selInsObj.Geometry);
                        Rhino.DocObjects.ObjectAttributes selNewAttr = new Rhino.DocObjects.ObjectAttributes();
                        selNewAttr.Name = selInsObj.Attributes.Name;
                        selNewAttr.LayerIndex = curPplLayerInd;
                        selNewAttr.ColorSource = ObjectColorSource.ColorFromLayer;
                        selInsAttrL.Add(selNewAttr);

                    }

                    int curNewInsDefInd = doc.InstanceDefinitions.Add(selInsDefGeo.Name, string.Empty, Point3d.Origin, selInsGeoL, selInsAttrL);
                    curAddedInsIndL.Add(curNewInsDefInd);
                }

            }

            //when there is no instance definitions in the read 3dm file
            List<int> curAddedMinusIndL = curAddedInsIndL.FindAll(insInd => insInd == -1);

            if ((curAddedInsIndL.Count == 0) || (curAddedMinusIndL.Count == curAddedInsIndL.Count))
            {
                RhinoApp.WriteLine("No instance definition added from selected 3dm file, please try again");
                return Rhino.Commands.Result.Failure;
            }

            /////get geometries to populate from doc
            //object filter
            const Rhino.DocObjects.ObjectType curObjSelFilter = Rhino.DocObjects.ObjectType.Curve;
            Rhino.DocObjects.ObjRef[] curObjRefA;

            //command prompt for geometry to populate
            string curCommandPrompt = "Please select curves to populate render people";

            Rhino.Commands.Result curCommandGR = Rhino.Input.RhinoGet.GetMultipleObjects(curCommandPrompt, true, curObjSelFilter, out curObjRefA);

            if ((curCommandGR != Rhino.Commands.Result.Success) || (curObjRefA == null)) return curCommandGR;

            /////get people density value from input
            bool pplDensityDecisionStatus = false;

            double curPplDensityVal = 0.0;

            while (pplDensityDecisionStatus == false)
            {
                string curPplDensityPrompt = "Please choose how much dense the curves are to be populated (0.0 - 100.0)";
                double curPplDensityVal_Def = 0.0;
                Rhino.Commands.Result curPplDensityGR = Rhino.Input.RhinoGet.GetNumber(curPplDensityPrompt, true, ref curPplDensityVal_Def);

                if (curPplDensityGR != Rhino.Commands.Result.Success) return curPplDensityGR;
                if ((curPplDensityVal_Def >= 0.0) && (curPplDensityVal_Def <= 100.0))
                {
                    curPplDensityVal = curPplDensityVal_Def;
                    pplDensityDecisionStatus = true;
                }
                else
                {
                    RhinoApp.WriteLine("Input number is not within range, please try again");
                }
            }

            /////get people facing point
            Point3d curPplOriRefPt = Point3d.Origin;
            string curRefPtPrompt = "Please pick a point the people will be oriented towards";
            Rhino.Commands.Result curRefPtGr = Rhino.Input.RhinoGet.GetPoint(curRefPtPrompt, true, out curPplOriRefPt);

            if ((curRefPtGr != Rhino.Commands.Result.Success) || (curPplOriRefPt == null)) return curRefPtGr;


            //get curve points to place instance references
            List<Curve> curCrvGotL = new List<Curve>();
            foreach (Rhino.DocObjects.ObjRef curObjRef in curObjRefA) curCrvGotL.Add(curObjRef.Curve());

            //record total placed instance reference count
            int totalPlacedRefCount = 0;

            /////curve random generator seed generator
            Random randSeedGen = new Random();


            for (int i = 0; i < curCrvGotL.Count; ++i)
            {
                var curCrvGot = curCrvGotL[i];
                if (curCrvGot != null)
                {
                    int curCrvRandSeed = randSeedGen.Next(0, 10000);
                    Random curCrvRandGen = new Random(curCrvRandSeed);

                    Curve curWorkingCrv = curCrvGot.DuplicateCurve();

                    //evaluate curve orientation according to the input reference point
                    Curve curWorkingCrv_Proj = Curve.ProjectToPlane(curWorkingCrv, Plane.WorldXY);
                    curWorkingCrv_Proj.Domain = new Interval(0.0, 1.0);
                    Point3d curOriPt_Proj = curPplOriRefPt;
                    curOriPt_Proj.Z = 0.0;

                    //when the curve is closed
                    if (curWorkingCrv_Proj.IsClosed == true)
                    {
                        if (curWorkingCrv_Proj.Contains(curOriPt_Proj, Plane.WorldXY, Rhino.RhinoMath.SqrtEpsilon) == PointContainment.Inside)
                        {
                            if (curWorkingCrv_Proj.ClosedCurveOrientation(Vector3d.ZAxis) == CurveOrientation.CounterClockwise) curWorkingCrv.Reverse();
                        }
                        if (curWorkingCrv_Proj.Contains(curOriPt_Proj, Plane.WorldXY, Rhino.RhinoMath.SqrtEpsilon) == PointContainment.Outside)
                        {
                            if (curWorkingCrv_Proj.ClosedCurveOrientation(Vector3d.ZAxis) == CurveOrientation.Clockwise) curWorkingCrv.Reverse();
                        }
                    }
                    //when the curve is open
                    else
                    {
                        double curOriPtPara = 0.0;
                        curWorkingCrv_Proj.ClosestPoint(curOriPt_Proj, out curOriPtPara);

                        Vector3d curOriPtVt = curOriPt_Proj - curWorkingCrv_Proj.PointAt(curOriPtPara);
                        double curWorkingCrv_Proj_len = curWorkingCrv_Proj.GetLength();

                        //when the orientation ref point is relatively close to the curve
                        if (curOriPtVt.Length < (2.0 * curWorkingCrv_Proj_len))
                        {
                            curOriPtVt.Unitize();

                            Vector3d curCrvOriVt = curWorkingCrv_Proj.DerivativeAt(curOriPtPara, 1)[1];
                            curCrvOriVt.Rotate(Math.PI / 2.0, Vector3d.ZAxis);
                            curCrvOriVt.Unitize();
                            curCrvOriVt *= -1.0;

                            double curCrvOriAngleDif = Math.Acos(curCrvOriVt * curOriPtVt);
                            if (curCrvOriAngleDif > (Math.PI / 2.0)) curWorkingCrv.Reverse();
                        }
                        //when the ref point is far away from the curve
                        else
                        {
                            Line curWorkingLn_Proj = new Line(curWorkingCrv_Proj.PointAtStart, curWorkingCrv_Proj.PointAtEnd);
                            Vector3d curWorkingGeneralVt_Proj = curWorkingCrv_Proj.PointAtEnd - curWorkingCrv_Proj.PointAtStart;
                            curWorkingGeneralVt_Proj.Unitize();
                            curWorkingGeneralVt_Proj.Rotate((Math.PI / 2.0), Vector3d.ZAxis);
                            curWorkingGeneralVt_Proj *= -1.0;

                            Vector3d curOriPtVt_Ln = curOriPt_Proj - curWorkingLn_Proj.ClosestPoint(curOriPt_Proj, false);
                            curOriPtVt_Ln.Unitize();

                            double curCrvOriAngleDif = Math.Acos(curWorkingGeneralVt_Proj * curOriPtVt_Ln);
                            if (curCrvOriAngleDif > (Math.PI / 2.0)) curWorkingCrv.Reverse();
                        }

                    }



                    curWorkingCrv.Domain = new Interval(0.0, 1.0);

                    //generate instance reference place pts
                    List<Point3d> selCrvPtL = new List<Point3d>();
                    Curve curWorkingRebCrv = curWorkingCrv.DuplicateCurve();
                    List<double> selCrvParaL = new List<double>();
                    if (curDocUnitSys == UnitSystem.Millimeters)
                    {
                        selCrvPtL = ScatteredRenderPeopleUtilities.SinglePersonPlacePtGen(curCrvRandGen, curWorkingCrv, curPplDensityVal,
                            1200.0, 600.0, out curWorkingRebCrv, out selCrvParaL);
                    }
                    else
                    {
                        selCrvPtL = ScatteredRenderPeopleUtilities.SinglePersonPlacePtGen(curCrvRandGen, curWorkingCrv, curPplDensityVal,
                            1.2, 0.6, out curWorkingRebCrv, out selCrvParaL);
                    }

                    if (selCrvPtL.Count == 0)
                    {
                        RhinoApp.WriteLine("Input density is too high for selected curve {0}, please adjust curve input or density value", Convert.ToDouble(i));
                        totalPlacedRefCount += 0;
                    }

                    else
                    {
                        int prevSelInsIndInd = curCrvRandGen.Next(0, curAddedInsIndL.Count);
                        int selInsIndInd0 = 0;

                        for (int j = 0; j < selCrvPtL.Count; ++j)
                        {
                            Point3d curSelCrvPt = selCrvPtL[j];
                            double curSelCrvPara = selCrvParaL[j];

                            //calculate current instance reference place plane
                            Vector3d curSelCrvTgVt = curWorkingRebCrv.DerivativeAt(curSelCrvPara, 1)[1];
                            Vector3d curInstanceRefXVt = curSelCrvTgVt;
                            curInstanceRefXVt.Z = 0.0;
                            curInstanceRefXVt.Unitize();

                            Vector3d curInstanceRefYVt = curInstanceRefXVt;
                            curInstanceRefYVt.Rotate((Math.PI / 2.0), Vector3d.ZAxis);

                            Plane curInstanceRefPl = new Plane(curSelCrvPt, curInstanceRefXVt, curInstanceRefYVt);



                            Transform curInstanceRefTransfm = Transform.PlaneToPlane(Plane.WorldXY, curInstanceRefPl);
                            //unit system
                            if (curDocUnitSys == UnitSystem.Millimeters)
                            {
                                Transform curUnitScalingTransfm = Transform.Scale(Point3d.Origin, 1000.0);
                                curInstanceRefTransfm = curInstanceRefTransfm * curUnitScalingTransfm;
                            }

                            //shift the references vertically a bit
                            if (curDocUnitSys == UnitSystem.Millimeters)
                            {
                                Transform curVerticalShiftTransfm = Transform.Translation(new Vector3d(0.0, 0.0, -450.0));
                                curInstanceRefTransfm = curVerticalShiftTransfm * curInstanceRefTransfm;
                            }

                            else
                            {
                                Transform curVerticalShiftTransfm = Transform.Translation(new Vector3d(0.0, 0.0, -0.45));
                                curInstanceRefTransfm = curVerticalShiftTransfm * curInstanceRefTransfm;
                            }

                            //generate current instance reference index to be added
                            bool consecutiveIndStatus = true;
                            int curNewInsIndInd = 0;
                            int curConsecLoopCounter = 0;
                            while (consecutiveIndStatus == true)
                            {
                                int curTestInd = curCrvRandGen.Next(0, curAddedInsIndL.Count);
                                if (curConsecLoopCounter < 1.0e5)
                                {
                                    if (curTestInd != prevSelInsIndInd)
                                    {

                                        if (j != (selCrvPtL.Count - 1))
                                        {
                                            if (j == 0) selInsIndInd0 = curTestInd;

                                            curNewInsIndInd = curTestInd;
                                            prevSelInsIndInd = curTestInd;

                                            consecutiveIndStatus = false;
                                        }

                                        else
                                        {
                                            if (curTestInd != selInsIndInd0)
                                            {
                                                curNewInsIndInd = curTestInd;
                                                prevSelInsIndInd = curTestInd;

                                                consecutiveIndStatus = false;
                                            }
                                        }

                                        curConsecLoopCounter++;
                                    }
                                    else
                                    {
                                        curConsecLoopCounter++;
                                        continue;
                                    }
                                }

                                else
                                {
                                    if (j != (selCrvPtL.Count - 1))
                                    {
                                        if (j == 0) selInsIndInd0 = curTestInd;
                                        curNewInsIndInd = curTestInd;
                                        prevSelInsIndInd = curTestInd;

                                        curConsecLoopCounter++;
                                        consecutiveIndStatus = false;
                                    }

                                    else
                                    {
                                        if (curTestInd != selInsIndInd0)
                                        {
                                            curNewInsIndInd = curTestInd;
                                            prevSelInsIndInd = curTestInd;

                                            consecutiveIndStatus = false;
                                        }

                                        curConsecLoopCounter++;
                                    }

                                }
                            }

                            int curNewInsInd = curAddedInsIndL[curNewInsIndInd];

                            //instance object attributes
                            Rhino.DocObjects.ObjectAttributes curInstanceRefAttr = new Rhino.DocObjects.ObjectAttributes();
                            curInstanceRefAttr.LayerIndex = curPplLayerInd;
                            curInstanceRefAttr.ColorSource = ObjectColorSource.ColorFromLayer;
                            doc.Objects.AddInstanceObject(curNewInsInd, curInstanceRefTransfm, curInstanceRefAttr);

                        }


                        totalPlacedRefCount += selCrvPtL.Count;
                    }

                }
            }


            if (totalPlacedRefCount == 0)
            {
                RhinoApp.WriteLine("Geometry to be populated is too small, place the blocks yourself you lazy fucking bastard!");
                return Result.Failure;
            }





            return Result.Success;
        }
    }
}