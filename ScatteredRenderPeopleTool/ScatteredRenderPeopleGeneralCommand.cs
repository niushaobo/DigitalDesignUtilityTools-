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
    public class ScatteredRenderPeopleGeneralCommand : Command
    {
        public ScatteredRenderPeopleGeneralCommand()
        {
            // Rhino only creates one instance of each command class defined in a
            // plug-in, so it is safe to store a refence in a static property.
            Instance = this;
        }

        ///<summary>The only instance of this command.</summary>
        public static ScatteredRenderPeopleGeneralCommand Instance { get; private set; }

        ///<returns>The command name as it appears on the Rhino command line.</returns>
        public override string EnglishName => "ScatteredRenderPeopleGeneral";

        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            //starting messages
            RhinoApp.WriteLine("The {0} command will populate render contents for selected geometry right now.", EnglishName);


            //interactively get files to import instance definitions from
            /*
            var curImpDialog = new Rhino.UI.OpenFileDialog()
            {
                Filter = @"Rhino 3D Models (*.3dm)|*.3dm",
                DefaultExt = "3dm"
            };
            var curImpDiaRes = curImpDialog.ShowOpenDialog();
            if (curImpDiaRes != true)
            {
                return Result.Cancel;
            }
            */


            //please replace "curSelFilePath" string with your designated file path
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


            /////
            
            //this is the part to get different kinds of people models from a separate file and when populate render models, mix the models
            //from different files by a certain ratio; if only one file is needed, the code can be adjusted accordingly


            //get instances from the 2nd file
            //read the 2nd file doc
            //please replace the "curSelFilePath_2nd" string with your designated file path
            string curSelFilePath_2nd = @"\\***\***.3dm";

            RhinoApp.WriteLine($"{curSelFilePath_2nd}");

            Rhino.FileIO.File3dm curSel3dm_2nd = new Rhino.FileIO.File3dm();

            try
            {
                curSel3dm_2nd = Rhino.FileIO.File3dm.Read(curSelFilePath_2nd);
            }

            catch (Exception ex)
            {
                return Rhino.Commands.Result.Failure;
            }

            //get updated instance definition names from the current doc
            List<string> insDefNameL_curDoc_updated = new List<string>();
            foreach (InstanceDefinition curInsDef in doc.InstanceDefinitions) insDefNameL_curDoc_updated.Add(curInsDef.Name);

            List<int> curAddedInsIndL_2nd = new List<int>();

            foreach (InstanceDefinitionGeometry selInsDefGeo in curSel3dm_2nd.AllInstanceDefinitions)
            {
                RhinoApp.WriteLine($"processing {selInsDefGeo.Name}");

                //see if current instance definition's name already exists in current document
                if (insDefNameL_curDoc_updated.Contains(selInsDefGeo.Name) == true) curAddedInsIndL_2nd.Add(insDefNameL_curDoc_updated.IndexOf(selInsDefGeo.Name));


                else
                {
                    Guid[] selInsIdA = selInsDefGeo.GetObjectIds();

                    List<GeometryBase> selInsGeoL = new List<GeometryBase>();
                    List<Rhino.DocObjects.ObjectAttributes> selInsAttrL = new List<Rhino.DocObjects.ObjectAttributes>();

                    foreach (Guid selId in selInsIdA)
                    {
                        Rhino.FileIO.File3dmObject selInsObj = curSel3dm_2nd.Objects.FindId(selId);

                        selInsGeoL.Add(selInsObj.Geometry);
                        Rhino.DocObjects.ObjectAttributes selNewAttr = new Rhino.DocObjects.ObjectAttributes();
                        selNewAttr.Name = selInsObj.Attributes.Name;
                        selNewAttr.LayerIndex = curPplLayerInd;
                        selNewAttr.ColorSource = ObjectColorSource.ColorFromLayer;
                        selInsAttrL.Add(selNewAttr);

                    }

                    int curNewInsDefInd = doc.InstanceDefinitions.Add(selInsDefGeo.Name, string.Empty, Point3d.Origin, selInsGeoL, selInsAttrL);
                    curAddedInsIndL_2nd.Add(curNewInsDefInd);
                }

            }


            //when there is no instance definitions in the read 3dm file
            List<int> curAddedMinusIndL_2nd = curAddedInsIndL_2nd.FindAll(insInd => insInd == -1);

            if ((curAddedInsIndL_2nd.Count == 0) || (curAddedMinusIndL_2nd.Count == curAddedInsIndL_2nd.Count))
            {
                RhinoApp.WriteLine("No instance definition added from selected 2nd 3dm file, please try again");
                return Rhino.Commands.Result.Failure;
            }

            /////


            /////get geometries to populate from doc
            //object filter
            const Rhino.DocObjects.ObjectType curObjSelFilter = Rhino.DocObjects.ObjectType.Surface | Rhino.DocObjects.ObjectType.Brep | Rhino.DocObjects.ObjectType.Mesh;
            Rhino.DocObjects.ObjRef curObjRef;

            //command prompt for geometry to populate
            string curCommandPrompt = "Please select surface, polysurface or mesh to populate render people";

            Rhino.Commands.Result curCommandGR = Rhino.Input.RhinoGet.GetOneObject(curCommandPrompt, true, curObjSelFilter, out curObjRef);

            if ((curCommandGR != Rhino.Commands.Result.Success) || (curObjRef == null)) return curCommandGR;

            Surface curSrfGot = curObjRef.Surface();
            Brep curBrepGot = curObjRef.Brep();
            Mesh curMeshGot = curObjRef.Mesh();

            //command prompt for populate number
            bool pplCountDecisionStatus = false;

            int curDecPplCount = 0;

            while (pplCountDecisionStatus == false)
            {
                string curPplCountPrompt = "Please choose how many people are to be placed (1 - 100)";
                int curDecPplCount_Def = 0;
                Rhino.Commands.Result curPplCountGR = Rhino.Input.RhinoGet.GetInteger(curPplCountPrompt, true, ref curDecPplCount_Def);

                if (curPplCountGR != Rhino.Commands.Result.Success) return curPplCountGR;
                if ((curDecPplCount_Def >= 1) && (curDecPplCount_Def <= 100))
                {
                    curDecPplCount = curDecPplCount_Def;
                    pplCountDecisionStatus = true;
                }
                else
                {
                    RhinoApp.WriteLine("Input integer is not within range, please try again");
                }
            }


            /////when geometry input is surface
            if (curSrfGot != null)
            {
                //get brep from working surface
                Brep curWorkingSrfBr = curSrfGot.ToBrep();


                //get input surface area
                Rhino.Geometry.AreaMassProperties curSrfBrAMP = Rhino.Geometry.AreaMassProperties.Compute(curWorkingSrfBr);
                double curSrfAreaThr = 2.0 * 2.0 * Convert.ToDouble(curDecPplCount);
                //unit system
                if (curDocUnitSys == UnitSystem.Millimeters) curSrfAreaThr *= 1.0e6;

                if (curSrfBrAMP.Area < (curSrfAreaThr))
                {
                    RhinoApp.WriteLine("Geometry to be populated is too small, place the blocks yourself you lazy fucking bastard!");
                    return Rhino.Commands.Result.Failure;
                }


                //meshing parameter setup
                Rhino.Geometry.QuadRemeshParameters curSrfBrQuadParas = new Rhino.Geometry.QuadRemeshParameters();

                curSrfBrQuadParas.DetectHardEdges = false;
                curSrfBrQuadParas.PreserveMeshArrayEdgesMode = 0;
                curSrfBrQuadParas.TargetEdgeLength = 1.0;
                //unit system
                if (curDocUnitSys == UnitSystem.Millimeters) curSrfBrQuadParas.TargetEdgeLength = 1000.0;

                Mesh curSrfBrMeshRep = Mesh.QuadRemeshBrep(curBrepGot, curSrfBrQuadParas);



                //create surface point random generator
                Random curSrfRandGen = new Random();

                List<int> curCalcGrPplCountL = new List<int>();
                List<double> curCalcGrPplRadiusL = new List<double>();
                List<Point3d> selMeshVertPtL = new List<Point3d>();

                //unit systems
                if (curDocUnitSys == UnitSystem.Millimeters)
                {
                    selMeshVertPtL = ScatteredRenderPeopleUtilities.GroupPeoplePlacePtGen(curSrfRandGen, curSrfBrMeshRep, curDecPplCount, 2000.0, 1200.0, 500.0,
                        out curCalcGrPplCountL, out curCalcGrPplRadiusL);
                }

                else
                {
                    selMeshVertPtL = ScatteredRenderPeopleUtilities.GroupPeoplePlacePtGen(curSrfRandGen, curSrfBrMeshRep, curDecPplCount, 2.0, 1.2, 0.5,
                        out curCalcGrPplCountL, out curCalcGrPplRadiusL);
                }




                //place instance objects
                for (int i = 0; i < selMeshVertPtL.Count; ++i)
                {
                    Point3d selSrfPt = selMeshVertPtL[i];
                    int curGrPplCount = curCalcGrPplCountL[i];
                    double curPplRadius = curCalcGrPplRadiusL[i];

                    if (curGrPplCount == 1)
                    {
                        double curRandRotVal = (Math.PI) * 2.0 * (curSrfRandGen.NextDouble());

                        Plane curInstanceRefPl = new Plane(selSrfPt, Vector3d.XAxis, Vector3d.YAxis);
                        curInstanceRefPl.Rotate(curRandRotVal, Vector3d.ZAxis);
                        Transform curInstanceRefTransfm = Transform.PlaneToPlane(Plane.WorldXY, curInstanceRefPl);
                        //unit system
                        if (curDocUnitSys == UnitSystem.Millimeters)
                        {
                            Transform curUnitScalingTransfm = Transform.Scale(Point3d.Origin, 1000.0);
                            curInstanceRefTransfm = curInstanceRefTransfm * curUnitScalingTransfm;
                        }
                        int curNewInsIndInd = curSrfRandGen.Next(0, curAddedInsIndL.Count);
                        int curNewInsInd = curAddedInsIndL[curNewInsIndInd];

                        //instance object attributes
                        Rhino.DocObjects.ObjectAttributes curInstanceRefAttr = new Rhino.DocObjects.ObjectAttributes();
                        curInstanceRefAttr.LayerIndex = curPplLayerInd;
                        curInstanceRefAttr.ColorSource = ObjectColorSource.ColorFromLayer;
                        doc.Objects.AddInstanceObject(curNewInsInd, curInstanceRefTransfm, curInstanceRefAttr);
                    }

                    else
                    {
                        Circle curGrPplCircle = new Circle(selSrfPt, curPplRadius);
                        NurbsCurve curGrPplNurbCrv = curGrPplCircle.ToNurbsCurve();

                        //make sure the circle is oriented counter-clockwise
                        if (curGrPplNurbCrv.ClosedCurveOrientation(Vector3d.ZAxis) == CurveOrientation.Clockwise) curGrPplNurbCrv.Reverse();
                        curGrPplNurbCrv.Domain = new Interval(0.0, 1.0);


                        //populate people around current circle nurbs
                        double curPplCrvInterval = 1.0 / (Convert.ToDouble(curGrPplCount));
                        for (int j = 0; j < curGrPplCount; ++j)
                        {
                            double curDefCrvPara = (Convert.ToDouble(j)) * curPplCrvInterval;
                            double curCrvParaOffsetVal = curPplCrvInterval * 0.25 * (-1.0 + 2.0 * (curSrfRandGen.NextDouble()));
                            double curCrvPara = curDefCrvPara + curCrvParaOffsetVal;

                            Vector3d curPplCrvTgVt = curGrPplNurbCrv.DerivativeAt(curCrvPara, 1)[1];
                            Vector3d curInstanceRefXVt = curPplCrvTgVt;
                            curInstanceRefXVt.Unitize();
                            curInstanceRefXVt *= -1.0;

                            Vector3d curInstanceRefYVt = curInstanceRefXVt;
                            curInstanceRefYVt.Rotate((Math.PI / 2.0), Vector3d.ZAxis);

                            //get current people placing point
                            Point3d curDefPlacePt = curGrPplNurbCrv.PointAt(curCrvPara);

                            Vector3d curPplPlaceOffsetVt = selSrfPt - curDefPlacePt;
                            curPplPlaceOffsetVt *= 0.25;
                            curPplPlaceOffsetVt *= (-1.0 + 2.0 * (curSrfRandGen.NextDouble()));

                            Point3d curPlacePt = curDefPlacePt + curPplPlaceOffsetVt;
                            //project to current brep transformation
                            Point3d curProjPlacePt = curWorkingSrfBr.ClosestPoint(curPlacePt);

                            Plane curInstanceRefPl = new Plane(curProjPlacePt, curInstanceRefXVt, curInstanceRefYVt);
                            Transform curInstanceRefTransfm = Transform.PlaneToPlane(Plane.WorldXY, curInstanceRefPl);

                            //unit system
                            if (curDocUnitSys == UnitSystem.Millimeters)
                            {
                                Transform curUnitScalingTransfm = Transform.Scale(Point3d.Origin, 1000.0);
                                curInstanceRefTransfm = curInstanceRefTransfm * curUnitScalingTransfm;
                            }

                            int curNewInsIndInd = curSrfRandGen.Next(0, curAddedInsIndL_2nd.Count);
                            int curNewInsInd = curAddedInsIndL_2nd[curNewInsIndInd];

                            //instance object attributes
                            Rhino.DocObjects.ObjectAttributes curInstanceRefAttr = new Rhino.DocObjects.ObjectAttributes();
                            curInstanceRefAttr.LayerIndex = curPplLayerInd;
                            curInstanceRefAttr.ColorSource = ObjectColorSource.ColorFromLayer;

                            doc.Objects.AddInstanceObject(curNewInsInd, curInstanceRefTransfm, curInstanceRefAttr);

                        }

                    }
                }


                return Rhino.Commands.Result.Success;
            }

            /////

            /////when geometry input is brep
            else if (curBrepGot != null)
            {
                //get input brep area
                Rhino.Geometry.AreaMassProperties curBrAMP = Rhino.Geometry.AreaMassProperties.Compute(curBrepGot);
                double curBrAreaThr = 2.0 * 2.0 * Convert.ToDouble(curDecPplCount);

                //unit system
                if (curDocUnitSys == UnitSystem.Millimeters) curBrAreaThr *= 1.0e6;

                if (curBrAMP.Area < (curBrAreaThr))
                {
                    RhinoApp.WriteLine("Geometry to be populated is too small, place the blocks yourself you lazy fucking bastard!");
                    return Rhino.Commands.Result.Failure;
                }

                Rhino.Geometry.QuadRemeshParameters curBrQuadParas = new Rhino.Geometry.QuadRemeshParameters();

                curBrQuadParas.DetectHardEdges = false;
                curBrQuadParas.PreserveMeshArrayEdgesMode = 0;
                curBrQuadParas.TargetEdgeLength = 1.0;
                //unit system
                if (curDocUnitSys == UnitSystem.Millimeters) curBrQuadParas.TargetEdgeLength = 1000.0;

                Mesh curBrMeshRep = Mesh.QuadRemeshBrep(curBrepGot, curBrQuadParas);


                //generate mesh vert points to put instance references
                Random curBrRandGen = new Random();

                List<int> curCalcGrPplCountL = new List<int>();
                List<double> curCalcGrPplRadiusL = new List<double>();
                List<Point3d> selMeshVertPtL = new List<Point3d>();

                //unit system
                if (curDocUnitSys == UnitSystem.Millimeters)
                {
                    selMeshVertPtL = ScatteredRenderPeopleUtilities.GroupPeoplePlacePtGen(curBrRandGen, curBrMeshRep, curDecPplCount, 2000.0, 1200.0, 500.0,
                        out curCalcGrPplCountL, out curCalcGrPplRadiusL);
                }
                else
                {
                    selMeshVertPtL = ScatteredRenderPeopleUtilities.GroupPeoplePlacePtGen(curBrRandGen, curBrMeshRep, curDecPplCount, 2.0, 1.2, 0.5,
                        out curCalcGrPplCountL, out curCalcGrPplRadiusL);
                }



                //place instance objects
                for (int i = 0; i < selMeshVertPtL.Count; ++i)
                {
                    Point3d selVertPt = selMeshVertPtL[i];
                    int curGrPplCount = curCalcGrPplCountL[i];
                    double curPplRadius = curCalcGrPplRadiusL[i];

                    if (curGrPplCount == 1)
                    {
                        double curRandRotVal = (Math.PI) * 2.0 * (curBrRandGen.NextDouble());

                        Plane curInstanceRefPl = new Plane(selVertPt, Vector3d.XAxis, Vector3d.YAxis);
                        curInstanceRefPl.Rotate(curRandRotVal, Vector3d.ZAxis);
                        Transform curInstanceRefTransfm = Transform.PlaneToPlane(Plane.WorldXY, curInstanceRefPl);

                        //unit system
                        if (curDocUnitSys == UnitSystem.Millimeters)
                        {
                            Transform curUnitScalingTransfm = Transform.Scale(Point3d.Origin, 1000.0);
                            curInstanceRefTransfm = curInstanceRefTransfm * curUnitScalingTransfm;
                        }

                        int curNewInsIndInd = curBrRandGen.Next(0, curAddedInsIndL.Count);
                        int curNewInsInd = curAddedInsIndL[curNewInsIndInd];

                        Rhino.DocObjects.ObjectAttributes curInstanceRefAttr = new Rhino.DocObjects.ObjectAttributes();
                        curInstanceRefAttr.LayerIndex = curPplLayerInd;
                        curInstanceRefAttr.ColorSource = ObjectColorSource.ColorFromLayer;

                        doc.Objects.AddInstanceObject(curNewInsInd, curInstanceRefTransfm, curInstanceRefAttr);
                    }

                    else
                    {
                        Circle curGrPplCircle = new Circle(selVertPt, curPplRadius);
                        NurbsCurve curGrPplNurbCrv = curGrPplCircle.ToNurbsCurve();

                        //make sure the circle is oriented counter-clockwise
                        if (curGrPplNurbCrv.ClosedCurveOrientation(Vector3d.ZAxis) == CurveOrientation.Clockwise) curGrPplNurbCrv.Reverse();
                        curGrPplNurbCrv.Domain = new Interval(0.0, 1.0);


                        //populate people around current circle nurbs
                        double curPplCrvInterval = 1.0 / (Convert.ToDouble(curGrPplCount));
                        for (int j = 0; j < curGrPplCount; ++j)
                        {
                            double curDefCrvPara = (Convert.ToDouble(j)) * curPplCrvInterval;
                            double curCrvParaOffsetVal = curPplCrvInterval * 0.25 * (-1.0 + 2.0 * (curBrRandGen.NextDouble()));
                            double curCrvPara = curDefCrvPara + curCrvParaOffsetVal;

                            Vector3d curPplCrvTgVt = curGrPplNurbCrv.DerivativeAt(curCrvPara, 1)[1];
                            Vector3d curInstanceRefXVt = curPplCrvTgVt;
                            curInstanceRefXVt.Unitize();
                            curInstanceRefXVt *= -1.0;

                            Vector3d curInstanceRefYVt = curInstanceRefXVt;
                            curInstanceRefYVt.Rotate((Math.PI / 2.0), Vector3d.ZAxis);

                            //get current people placing point
                            Point3d curDefPlacePt = curGrPplNurbCrv.PointAt(curCrvPara);

                            Vector3d curPplPlaceOffsetVt = selVertPt - curDefPlacePt;
                            curPplPlaceOffsetVt *= 0.25;
                            curPplPlaceOffsetVt *= (-1.0 + 2.0 * (curBrRandGen.NextDouble()));

                            Point3d curPlacePt = curDefPlacePt + curPplPlaceOffsetVt;
                            //project to current brep transformation
                            Point3d curProjPlacePt = curBrepGot.ClosestPoint(curPlacePt);

                            Plane curInstanceRefPl = new Plane(curProjPlacePt, curInstanceRefXVt, curInstanceRefYVt);
                            Transform curInstanceRefTransfm = Transform.PlaneToPlane(Plane.WorldXY, curInstanceRefPl);

                            //unit system
                            if (curDocUnitSys == UnitSystem.Millimeters)
                            {
                                Transform curUnitScalingTransfm = Transform.Scale(Point3d.Origin, 1000.0);
                                curInstanceRefTransfm = curInstanceRefTransfm * curUnitScalingTransfm;
                            }

                            int curNewInsIndInd = curBrRandGen.Next(0, curAddedInsIndL_2nd.Count);
                            int curNewInsInd = curAddedInsIndL_2nd[curNewInsIndInd];

                            Rhino.DocObjects.ObjectAttributes curInstanceRefAttr = new Rhino.DocObjects.ObjectAttributes();
                            curInstanceRefAttr.LayerIndex = curPplLayerInd;
                            curInstanceRefAttr.ColorSource = ObjectColorSource.ColorFromLayer;

                            doc.Objects.AddInstanceObject(curNewInsInd, curInstanceRefTransfm, curInstanceRefAttr);

                        }

                    }
                }


                return Rhino.Commands.Result.Success;
            }
            /////

            /////when input geometry is mesh
            else if (curMeshGot != null)
            {

                //get input mesh area
                Rhino.Geometry.AreaMassProperties curMeshAMP = Rhino.Geometry.AreaMassProperties.Compute(curMeshGot);
                double curMeshAreaThr = 2.0 * 2.0 * Convert.ToDouble(curDecPplCount);


                //unit system
                if (curDocUnitSys == UnitSystem.Millimeters) curMeshAreaThr *= 1.0e6;

                if (curMeshAMP.Area < (curMeshAreaThr))
                {
                    RhinoApp.WriteLine("Geometry to be populated is too small, place the blocks yourself you lazy fucking bastard!");
                    return Rhino.Commands.Result.Failure;
                }

                Rhino.Geometry.QuadRemeshParameters curMeshQuadParas = new Rhino.Geometry.QuadRemeshParameters();

                curMeshQuadParas.DetectHardEdges = false;
                curMeshQuadParas.PreserveMeshArrayEdgesMode = 0;
                curMeshQuadParas.TargetEdgeLength = 1.0;
                //unit system
                if (curDocUnitSys == UnitSystem.Millimeters) curMeshQuadParas.TargetEdgeLength = 1000.0;
                Mesh curMeshQuadRep = curMeshGot.QuadRemesh(curMeshQuadParas);


                //generate mesh vert points to put instance references
                Random curMeshRandGen = new Random();

                List<int> curCalcGrPplCountL = new List<int>();
                List<double> curCalcGrPplRadiusL = new List<double>();
                List<Point3d> selMeshVertPtL = new List<Point3d>();

                //unit system
                if (curDocUnitSys == UnitSystem.Millimeters)
                {
                    selMeshVertPtL = ScatteredRenderPeopleUtilities.GroupPeoplePlacePtGen(curMeshRandGen, curMeshQuadRep, curDecPplCount, 2000.0, 1200.0, 500.0,
                        out curCalcGrPplCountL, out curCalcGrPplRadiusL);
                }

                else
                {
                    selMeshVertPtL = ScatteredRenderPeopleUtilities.GroupPeoplePlacePtGen(curMeshRandGen, curMeshQuadRep, curDecPplCount, 2.0, 1.2, 0.5,
                        out curCalcGrPplCountL, out curCalcGrPplRadiusL);
                }


                //place instance objects
                for (int i = 0; i < selMeshVertPtL.Count; ++i)
                {
                    Point3d selVertPt = selMeshVertPtL[i];
                    int curGrPplCount = curCalcGrPplCountL[i];
                    double curPplRadius = curCalcGrPplRadiusL[i];

                    if (curGrPplCount == 1)
                    {
                        double curRandRotVal = (Math.PI) * 2.0 * (curMeshRandGen.NextDouble());

                        Plane curInstanceRefPl = new Plane(selVertPt, Vector3d.XAxis, Vector3d.YAxis);
                        curInstanceRefPl.Rotate(curRandRotVal, Vector3d.ZAxis);
                        Transform curInstanceRefTransfm = Transform.PlaneToPlane(Plane.WorldXY, curInstanceRefPl);

                        //unit system
                        if (curDocUnitSys == UnitSystem.Millimeters)
                        {
                            Transform curUnitScalingTransfm = Transform.Scale(Point3d.Origin, 1000.0);
                            curInstanceRefTransfm = curInstanceRefTransfm * curUnitScalingTransfm;
                        }

                        int curNewInsIndInd = curMeshRandGen.Next(0, curAddedInsIndL.Count);
                        int curNewInsInd = curAddedInsIndL[curNewInsIndInd];

                        Rhino.DocObjects.ObjectAttributes curInstanceRefAttr = new Rhino.DocObjects.ObjectAttributes();
                        curInstanceRefAttr.LayerIndex = curPplLayerInd;
                        curInstanceRefAttr.ColorSource = ObjectColorSource.ColorFromLayer;

                        doc.Objects.AddInstanceObject(curNewInsInd, curInstanceRefTransfm, curInstanceRefAttr);
                    }

                    else
                    {
                        Circle curGrPplCircle = new Circle(selVertPt, curPplRadius);
                        NurbsCurve curGrPplNurbCrv = curGrPplCircle.ToNurbsCurve();

                        //make sure the circle is oriented counter-clockwise
                        if (curGrPplNurbCrv.ClosedCurveOrientation(Vector3d.ZAxis) == CurveOrientation.Clockwise) curGrPplNurbCrv.Reverse();
                        curGrPplNurbCrv.Domain = new Interval(0.0, 1.0);


                        //populate people around current circle nurbs
                        double curPplCrvInterval = 1.0 / (Convert.ToDouble(curGrPplCount));
                        for (int j = 0; j < curGrPplCount; ++j)
                        {
                            double curDefCrvPara = (Convert.ToDouble(j)) * curPplCrvInterval;
                            double curCrvParaOffsetVal = curPplCrvInterval * 0.25 * (-1.0 + 2.0 * (curMeshRandGen.NextDouble()));
                            double curCrvPara = curDefCrvPara + curCrvParaOffsetVal;

                            Vector3d curPplCrvTgVt = curGrPplNurbCrv.DerivativeAt(curCrvPara, 1)[1];
                            Vector3d curInstanceRefXVt = curPplCrvTgVt;
                            curInstanceRefXVt.Unitize();
                            curInstanceRefXVt *= -1.0;

                            Vector3d curInstanceRefYVt = curInstanceRefXVt;
                            curInstanceRefYVt.Rotate((Math.PI / 2.0), Vector3d.ZAxis);

                            //get current people placing point
                            Point3d curDefPlacePt = curGrPplNurbCrv.PointAt(curCrvPara);

                            Vector3d curPplPlaceOffsetVt = selVertPt - curDefPlacePt;
                            curPplPlaceOffsetVt *= 0.25;
                            curPplPlaceOffsetVt *= (-1.0 + 2.0 * (curMeshRandGen.NextDouble()));

                            Point3d curPlacePt = curDefPlacePt + curPplPlaceOffsetVt;
                            //project to current mesh transformation
                            Point3d curProjPlacePt = curMeshGot.ClosestPoint(curPlacePt);

                            Plane curInstanceRefPl = new Plane(curProjPlacePt, curInstanceRefXVt, curInstanceRefYVt);
                            Transform curInstanceRefTransfm = Transform.PlaneToPlane(Plane.WorldXY, curInstanceRefPl);

                            //unit system
                            if (curDocUnitSys == UnitSystem.Millimeters)
                            {
                                Transform curUnitScalingTransfm = Transform.Scale(Point3d.Origin, 1000.0);
                                curInstanceRefTransfm = curInstanceRefTransfm * curUnitScalingTransfm;
                            }

                            int curNewInsIndInd = curMeshRandGen.Next(0, curAddedInsIndL_2nd.Count);
                            int curNewInsInd = curAddedInsIndL_2nd[curNewInsIndInd];

                            Rhino.DocObjects.ObjectAttributes curInstanceRefAttr = new Rhino.DocObjects.ObjectAttributes();
                            curInstanceRefAttr.LayerIndex = curPplLayerInd;
                            curInstanceRefAttr.ColorSource = ObjectColorSource.ColorFromLayer;

                            doc.Objects.AddInstanceObject(curNewInsInd, curInstanceRefTransfm, curInstanceRefAttr);

                        }

                    }
                }



                return Rhino.Commands.Result.Success;
            }
            /////

            else
            {
                RhinoApp.WriteLine("Working geometries are not selected for this command, please try again");
                return Rhino.Commands.Result.Failure;
            }
        }
    }
}
